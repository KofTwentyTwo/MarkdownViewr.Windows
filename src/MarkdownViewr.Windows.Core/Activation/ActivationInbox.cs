/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text.Json;


namespace MarkdownViewr.Windows.Core.Activation;

/// <summary>
/// The running reader's inbox for documents a later launch asks it to open. One named pipe,
/// open only to the current Windows user, carries one length-prefixed <see cref="ActivationRequest"/>
/// per connection and answers with one byte: accepted or not.
/// </summary>
public sealed class ActivationInbox : IDisposable
{
   /// <summary>Requests held while the reader is still starting; a further one is refused so the sender can report it.</summary>
   public const int MaximumPendingRequests = 16;

   /// <summary>How long a sender waits for the reader's pipe to exist and be free.</summary>
   public static readonly TimeSpan ConnectDeadline = TimeSpan.FromSeconds(2);

   /// <summary>How long either side waits for the other to finish one request.</summary>
   public static readonly TimeSpan ExchangeDeadline = TimeSpan.FromSeconds(10);

   private readonly Lock _gate = new();
   private readonly Queue<ActivationRequest> _pending = new();
   private readonly CancellationTokenSource _closed = new();
   private readonly NamedPipeServerStream _pipe;
   private readonly Action<Exception> _report;
   private readonly TimeProvider _time;
   private readonly Task _listener;
   private Func<ActivationRequest, bool>? _dispatch;
   private int _disposed;



   /// <summary>
   /// Creates the pipe and starts listening. Throws when the pipe name is already taken, by a
   /// reader that has not finished closing or by another program; the caller then runs without
   /// an inbox. Deadlines run on <paramref name="time"/>, the system clock unless a test supplies its own.
   /// </summary>
   public ActivationInbox(string key, Action<Exception> report, TimeProvider? time = null)
   {
      ArgumentNullException.ThrowIfNull(report);
      _report = report;
      _time = time ?? TimeProvider.System;
      _pipe = new NamedPipeServerStream(key, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
      _listener = ListenAsync();
   }



   /// <summary>Completes when the listener has stopped after <see cref="Dispose"/>.</summary>
   public Task Completion => _listener;



   /// <summary>
   /// Connects the inbox to the reader once its windows can open documents, delivering first the
   /// requests that arrived while it was starting. The dispatcher returns false when it cannot
   /// take a request.
   /// </summary>
   public void Attach(Func<ActivationRequest, bool> dispatch)
   {
      ArgumentNullException.ThrowIfNull(dispatch);
      lock(_gate)
      {
         _dispatch = dispatch;
         while(_pending.TryDequeue(out ActivationRequest? request))
         {
            if(!dispatch(request))
            {
               _report(new IOException("The reader could not accept activation."));
            }
         }
      }
   }



   /// <summary>Sends one request to the running reader and returns whether it accepted it.</summary>
   public static Task<bool> SendAsync(string key, ActivationRequest request, CancellationToken cancellationToken)
      => SendAsync(key, request, TimeProvider.System, cancellationToken);



   /// <summary>Sends one request with its deadlines on the given clock. Throws when no reader answers in time or the request is too large to send.</summary>
   internal static async Task<bool> SendAsync(string key, ActivationRequest request, TimeProvider time, CancellationToken cancellationToken)
   {
      ArgumentNullException.ThrowIfNull(request);
      request.Validate();
      byte[] payload = JsonSerializer.SerializeToUtf8Bytes(request, ActivationJsonContext.Default.ActivationRequest);
      if(payload.Length > ActivationRequest.MaximumPayloadBytes)
      {
         throw new ArgumentException("The activation payload is too large.", nameof(request));
      }
      await using var client = new NamedPipeClientStream(".", key, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
      using(var deadline = new CancellationTokenSource(ConnectDeadline, time))
      using(var connecting = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token))
      {
         await client.ConnectAsync(connecting.Token);
      }
      using var exchangeDeadline = new CancellationTokenSource(ExchangeDeadline, time);
      using var exchanging = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, exchangeDeadline.Token);
      byte[] length = new byte[4];
      BinaryPrimitives.WriteInt32LittleEndian(length, payload.Length);
      await client.WriteAsync(length, exchanging.Token);
      await client.WriteAsync(payload, exchanging.Token);
      await client.FlushAsync(exchanging.Token);
      byte[] response = new byte[1];
      await client.ReadExactlyAsync(response, exchanging.Token);
      return response[0] == 1;
   }



   /// <summary>Serves one connection at a time until disposed; a peer that disconnects, stalls or sends an invalid request is reported and does not stop the next one.</summary>
   private async Task ListenAsync()
   {
      while(!_closed.IsCancellationRequested)
      {
         bool connected = false;
         try
         {
            await _pipe.WaitForConnectionAsync(_closed.Token);
            connected = true;
            using var deadline = new CancellationTokenSource(ExchangeDeadline, _time);
            using var exchanging = CancellationTokenSource.CreateLinkedTokenSource(_closed.Token, deadline.Token);
            bool accepted = Accept(await ReadRequestAsync(exchanging.Token));
            await _pipe.WriteAsync(new byte[] { accepted ? (byte)1 : (byte)0 }, exchanging.Token);
            await _pipe.FlushAsync(exchanging.Token);
         }
         catch(Exception exception) when(exception is IOException or InvalidDataException or JsonException or OperationCanceledException or ObjectDisposedException)
         {
            if(!_closed.IsCancellationRequested)
            {
               _report(exception);
            }
         }
         finally
         {
            // Under the lock Dispose takes, so the pipe cannot be disposed between the check and the call.
            lock(_gate)
            {
               if(connected && !_closed.IsCancellationRequested)
               {
                  // A failed read clears IsConnected, but the connection still has to be released before the next peer.
                  _pipe.Disconnect();
               }
            }
         }
      }
   }



   /// <summary>Reads one bounded, length-prefixed request and checks it against the schema and the path rules.</summary>
   private async Task<ActivationRequest> ReadRequestAsync(CancellationToken cancellationToken)
   {
      byte[] length = new byte[4];
      await _pipe.ReadExactlyAsync(length, cancellationToken);
      int size = BinaryPrimitives.ReadInt32LittleEndian(length);
      if(size <= 0 || size > ActivationRequest.MaximumPayloadBytes)
      {
         throw new InvalidDataException("Invalid activation length.");
      }
      byte[] payload = new byte[size];
      await _pipe.ReadExactlyAsync(payload, cancellationToken);
      ActivationRequest request = JsonSerializer.Deserialize(payload, ActivationJsonContext.Default.ActivationRequest) ?? throw new InvalidDataException("Missing activation request.");
      request.Validate();
      return request;
   }



   /// <summary>Hands the request to the reader, or holds it until the reader attaches; false when neither is possible.</summary>
   private bool Accept(ActivationRequest request)
   {
      lock(_gate)
      {
         if(_dispatch is not null)
         {
            return _dispatch(request);
         }
         if(_pending.Count >= MaximumPendingRequests)
         {
            return false;
         }
         _pending.Enqueue(request);
         return true;
      }
   }



   /// <summary>Stops listening and releases the pipe name.</summary>
   public void Dispose()
   {
      if(Interlocked.Exchange(ref _disposed, 1) != 0)
      {
         return;
      }
      lock(_gate)
      {
         _closed.Cancel();
         _pipe.Dispose();
      }
      _closed.Dispose();
   }
}
