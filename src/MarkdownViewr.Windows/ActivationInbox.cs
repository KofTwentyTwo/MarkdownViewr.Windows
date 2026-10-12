/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text.Json;


namespace MarkdownViewr.Windows;

/// <summary>Bounded current-user IPC delivers sender-canonicalized document paths to the SDK's chosen primary instance.</summary>
internal sealed class ActivationInbox : IDisposable
{
   private readonly Lock _gate = new();
   private readonly Queue<ActivationRequest> _pending = new();
   private readonly CancellationTokenSource _closed = new();
   private readonly NamedPipeServerStream _pipe;
   private readonly Action<Exception> _report;
   private readonly Task _listener;
   private Func<ActivationRequest, bool>? _dispatch;
   private int _disposed;



   /// <summary>The primary owns the pipe before XAML starts; only the same Windows user can connect.</summary>
   internal ActivationInbox(string key, Action<Exception> report)
   {
      _report = report;
      _pipe = new NamedPipeServerStream(key, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
      _listener = ListenAsync();
   }



   /// <summary>The owned listener completes on disposal; tests and shutdown diagnostics can observe failures instead of losing a task.</summary>
   internal Task Completion => _listener;



   /// <summary>Attaches the native UI dispatcher after launch, delivering bounded requests received while XAML initialized.</summary>
   internal void Attach(Func<ActivationRequest, bool> dispatch)
   {
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



   /// <summary>Sends one fixed request with a bounded connection/acknowledgement wait and preserves the real server acknowledgement.</summary>
   internal static async Task<bool> SendAsync(string key, ActivationRequest request, CancellationToken cancellationToken)
   {
      request.Validate();
      byte[] payload = JsonSerializer.SerializeToUtf8Bytes(request, ActivationJsonContext.Default.ActivationRequest);
      if(payload.Length > ActivationRequest.MaximumPayloadBytes)
      {
         throw new ArgumentException("The activation payload is too large.", nameof(request));
      }
      using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      timeout.CancelAfter(TimeSpan.FromSeconds(10));
      await using var client = new NamedPipeClientStream(".", key, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
      await client.ConnectAsync(timeout.Token);
      byte[] length = new byte[4];
      BinaryPrimitives.WriteInt32LittleEndian(length, payload.Length);
      await client.WriteAsync(length, timeout.Token);
      await client.WriteAsync(payload, timeout.Token);
      await client.FlushAsync(timeout.Token);
      byte[] response = new byte[1];
      await client.ReadExactlyAsync(response, timeout.Token);
      return response[0] == 1;
   }



   /// <summary>A disconnected or malformed peer cannot stop later activations; each request has a bounded read deadline.</summary>
   private async Task ListenAsync()
   {
      while(!_closed.IsCancellationRequested)
      {
         bool connected = false;
         try
         {
            await _pipe.WaitForConnectionAsync(_closed.Token);
            connected = true;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_closed.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            byte[] length = new byte[4];
            await _pipe.ReadExactlyAsync(length, timeout.Token);
            int size = BinaryPrimitives.ReadInt32LittleEndian(length);
            if(size <= 0 || size > ActivationRequest.MaximumPayloadBytes)
            {
               throw new InvalidDataException("Invalid activation length.");
            }
            byte[] payload = new byte[size];
            await _pipe.ReadExactlyAsync(payload, timeout.Token);
            ActivationRequest request = JsonSerializer.Deserialize(payload, ActivationJsonContext.Default.ActivationRequest) ?? throw new InvalidDataException("Missing activation request.");
            request.Validate();
            bool accepted;
            lock(_gate)
            {
               accepted = _dispatch is null ? QueueBeforeLaunch(request) : _dispatch(request);
            }
            await _pipe.WriteAsync(new byte[] { accepted ? (byte)1 : (byte)0 }, timeout.Token);
            await _pipe.FlushAsync(timeout.Token);
         }
         catch(Exception exception) when(exception is IOException or InvalidDataException or JsonException or ArgumentException or OperationCanceledException or ObjectDisposedException)
         {
            if(!_closed.IsCancellationRequested)
            {
               _report(exception);
            }
         }
         finally
         {
            if(connected && !_closed.IsCancellationRequested)
            {
               // A broken read clears IsConnected, but the owned native connection still requires Disconnect.
               try
               {
                  _pipe.Disconnect();
               }
               catch(ObjectDisposedException) when(_closed.IsCancellationRequested)
               {
                  // Shutdown may release the handle after the cancellation check.
               }
            }
         }
      }
   }



   /// <summary>A startup burst can queue at most16 requests; the peer is told to retry if the native reader is busy.</summary>
   private bool QueueBeforeLaunch(ActivationRequest request)
   {
      if(_pending.Count >= 16)
      {
         return false;
      }
      _pending.Enqueue(request);
      return true;
   }



   /// <summary>Application shutdown cancels pending connections and releases the owned current-user pipe.</summary>
   public void Dispose()
   {
      if(Interlocked.Exchange(ref _disposed, 1) != 0)
      {
         return;
      }
      _closed.Cancel();
      _pipe.Dispose();
      _closed.Dispose();
   }
}
