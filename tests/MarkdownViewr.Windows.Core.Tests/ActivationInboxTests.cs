/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text;
using MarkdownViewr.Windows.Core.Activation;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>
/// Real current-user pipes between a real inbox and real senders. Deadlines run on a manual
/// clock, so no test waits for one and none can be reached early by a slow machine.
/// </summary>
public sealed class ActivationInboxTests
{
   private static readonly TimeSpan s_safetyNet = TimeSpan.FromSeconds(30);



   /// <summary>A request sent while the reader is starting is held until it attaches; afterwards each request gets the reader's own answer.</summary>
   [Fact]
   public async Task Send_QueuedThenAttached_DeliversOnceAndHonorsAcknowledgement()
   {
      string key = NewKey();
      var received = new ConcurrentQueue<ActivationRequest>();
      var errors = new ConcurrentQueue<Exception>();
      using var inbox = new ActivationInbox(key, errors.Enqueue);
      var request = ActivationRequest.Create(["sender & café.md"], Path.GetTempPath());
      using var safety = new CancellationTokenSource(s_safetyNet);
      Assert.True(await ActivationInbox.SendAsync(key, request, safety.Token));
      Assert.Empty(received);
      inbox.Attach(value =>
      {
         received.Enqueue(value);
         return true;
      });
      Assert.Equal(request.Paths, Assert.Single(received).Paths);
      Assert.True(await ActivationInbox.SendAsync(key, request, safety.Token));
      Assert.Equal(2, received.Count);
      inbox.Attach(_ => false);
      Assert.False(await ActivationInbox.SendAsync(key, request, safety.Token));
      Assert.Equal(2, received.Count);
      Assert.Empty(errors);
   }



   /// <summary>While the reader is starting it holds a bounded number of requests and refuses the next, which the sender is told.</summary>
   [Fact]
   public async Task Send_MoreThanTheBoundBeforeTheReaderAttaches_RefusesTheExtraRequest()
   {
      string key = NewKey();
      var received = new ConcurrentQueue<ActivationRequest>();
      using var inbox = new ActivationInbox(key, _ => { });
      using var safety = new CancellationTokenSource(s_safetyNet);
      for(int index = 0; index < ActivationInbox.MaximumPendingRequests; index++)
      {
         Assert.True(await ActivationInbox.SendAsync(key, ActivationRequest.Create([$"held{index}.md"], Path.GetTempPath()), safety.Token));
      }
      Assert.False(await ActivationInbox.SendAsync(key, ActivationRequest.Create(["one too many.md"], Path.GetTempPath()), safety.Token));
      inbox.Attach(value =>
      {
         received.Enqueue(value);
         return true;
      });
      Assert.Equal(Enumerable.Range(0, ActivationInbox.MaximumPendingRequests).Select(index => $"held{index}.md"), received.Select(request => Path.GetFileName(Assert.Single(request.Paths))), StringComparer.Ordinal);
      Assert.Equal(16, ActivationInbox.MaximumPendingRequests);
   }



   /// <summary>A held request the reader then cannot take is reported, not silently dropped.</summary>
   [Fact]
   public async Task Attach_HeldRequestTheReaderRefuses_IsReported()
   {
      string key = NewKey();
      var errors = new ConcurrentQueue<Exception>();
      using var inbox = new ActivationInbox(key, errors.Enqueue);
      using var safety = new CancellationTokenSource(s_safetyNet);
      Assert.True(await ActivationInbox.SendAsync(key, new ActivationRequest([]), safety.Token));
      inbox.Attach(_ => false);
      Assert.IsType<IOException>(Assert.Single(errors));
      Assert.Throws<ArgumentNullException>(() => inbox.Attach(null!));
      Assert.Throws<ArgumentNullException>(() => new ActivationInbox(NewKey(), null!));
   }



   /// <summary>
   /// The pipe name belongs to one inbox at a time. A second one fails to start, so its process
   /// can run without an inbox, and the name is free again once the first is disposed.
   /// </summary>
   [Fact]
   public async Task Constructor_NameAlreadyTaken_ThrowsUntilTheOwnerIsDisposed()
   {
      string key = NewKey();
      using var safety = new CancellationTokenSource(s_safetyNet);
      Task stopped;
      using(var owner = new ActivationInbox(key, _ => { }))
      {
         Assert.ThrowsAny<IOException>(() => new ActivationInbox(key, _ => { }));
         stopped = owner.Completion;
      }
      await stopped.WaitAsync(safety.Token);
      using var successor = new ActivationInbox(key, _ => { });
      successor.Attach(_ => true);
      Assert.True(await ActivationInbox.SendAsync(key, new ActivationRequest([]), safety.Token));
   }



   /// <summary>A frame with a zero, negative or oversized length, an unknown field, a relative path or a null body is rejected; the next valid sender is still served.</summary>
   [Fact]
   public async Task Listen_MalformedPeers_RejectsAndRecovers()
   {
      string key = NewKey();
      var received = new ConcurrentQueue<ActivationRequest>();
      var errors = new ConcurrentQueue<Exception>();
      using var inbox = new ActivationInbox(key, errors.Enqueue);
      inbox.Attach(value =>
      {
         received.Enqueue(value);
         return true;
      });
      using var safety = new CancellationTokenSource(s_safetyNet);
      await SendFrame(key, ActivationRequest.MaximumPayloadBytes + 1, [], safety.Token);
      await SendFrame(key, 0, [], safety.Token);
      await SendFrame(key, -1, [], safety.Token);
      foreach(string body in new[] { "{\"Paths\":[],\"Command\":\"execute\"}", "{\"Paths\":[\"relative.md\"]}", "null" })
      {
         byte[] payload = Encoding.UTF8.GetBytes(body);
         await SendFrame(key, payload.Length, payload, safety.Token);
      }
      var request = ActivationRequest.Create(["valid.md"], Path.GetTempPath());
      Assert.True(await ActivationInbox.SendAsync(key, request, safety.Token));
      Assert.Equal(request.Paths, Assert.Single(received).Paths);
      // Each frame is refused for its own reason: the three lengths before any payload is read, then the schema, the path rule and the empty body.
      Type[] expected = [typeof(InvalidDataException), typeof(InvalidDataException), typeof(InvalidDataException), typeof(System.Text.Json.JsonException), typeof(InvalidDataException), typeof(InvalidDataException)];
      Assert.Equal(expected, errors.Select(error => error.GetType()));
      Assert.Equal(1024 * 1024, ActivationRequest.MaximumPayloadBytes);
   }



   /// <summary>A frame of exactly the largest size is read and accepted.</summary>
   [Fact]
   public async Task Listen_FrameOfExactlyTheLargestSize_IsAccepted()
   {
      string key = NewKey();
      var received = new ConcurrentQueue<ActivationRequest>();
      var errors = new ConcurrentQueue<Exception>();
      using var inbox = new ActivationInbox(key, errors.Enqueue);
      inbox.Attach(value =>
      {
         received.Enqueue(value);
         return true;
      });
      using var safety = new CancellationTokenSource(s_safetyNet);
      byte[] payload = new byte[ActivationRequest.MaximumPayloadBytes];
      Array.Fill(payload, (byte)' ');
      "{\"Paths\":[\"C:\\\\largest.md\"]}"u8.CopyTo(payload);
      Assert.Equal(1, await SendFrame(key, payload.Length, payload, safety.Token));
      Assert.Equal([@"C:\largest.md"], Assert.Single(received).Paths);
      Assert.Empty(errors);
   }



   /// <summary>A sender never puts an invalid or oversized request on the pipe.</summary>
   [Fact]
   public async Task Send_InvalidOrOversizedRequest_IsRefusedBeforeConnecting()
   {
      string key = NewKey();
      using var safety = new CancellationTokenSource(s_safetyNet);
      await Assert.ThrowsAsync<InvalidDataException>(() => ActivationInbox.SendAsync(key, new ActivationRequest(["relative.md"]), safety.Token));
      await Assert.ThrowsAsync<ArgumentNullException>(() => ActivationInbox.SendAsync(key, null!, safety.Token));
      string longPath = @"C:\" + new string('a', 9000);
      var oversized = new ActivationRequest(Enumerable.Range(0, ActivationRequest.MaximumPaths).Select(index => longPath + index.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray());
      oversized.Validate();
      await Assert.ThrowsAsync<ArgumentException>(() => ActivationInbox.SendAsync(key, oversized, safety.Token));
   }



   /// <summary>With no reader listening, a sender gives up at its connect deadline instead of waiting forever.</summary>
   [Fact]
   public async Task Send_NoReaderListening_GivesUpAtTheConnectDeadline()
   {
      var clock = new ManualTimeProvider();
      using var safety = new CancellationTokenSource(s_safetyNet);
      Task<bool> sending = ActivationInbox.SendAsync(NewKey(), new ActivationRequest([]), clock, safety.Token);
      await clock.NextTimerAsync(safety.Token);
      Assert.False(sending.IsCompleted);
      clock.Elapse();
      await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sending);
      Assert.False(safety.IsCancellationRequested);
      Assert.Equal(TimeSpan.FromSeconds(2), ActivationInbox.ConnectDeadline);
   }



   /// <summary>A peer that connects and sends nothing is dropped at the exchange deadline, and the next sender is served.</summary>
   [Fact]
   public async Task Listen_PeerThatSendsNothing_IsDroppedAtTheDeadline()
   {
      string key = NewKey();
      var clock = new ManualTimeProvider();
      var reported = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
      using var inbox = new ActivationInbox(key, exception => reported.TrySetResult(exception), clock);
      inbox.Attach(_ => true);
      using var safety = new CancellationTokenSource(s_safetyNet);
      await using(var silent = new NamedPipeClientStream(".", key, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly))
      {
         await silent.ConnectAsync(safety.Token);
         await clock.NextTimerAsync(safety.Token);
         Assert.False(reported.Task.IsCompleted);
         clock.Elapse();
         Assert.IsAssignableFrom<OperationCanceledException>(await reported.Task.WaitAsync(safety.Token));
      }
      Assert.True(await ActivationInbox.SendAsync(key, new ActivationRequest([]), safety.Token));
      Assert.Equal(TimeSpan.FromSeconds(10), ActivationInbox.ExchangeDeadline);
   }



   /// <summary>A sender that disconnects without a request is reported and does not block the next one; disposing twice is harmless and frees the name.</summary>
   [Fact]
   public async Task Listen_DisconnectedPeer_RecoversAndReleasesPipe()
   {
      string key = NewKey();
      var errors = new ConcurrentQueue<Exception>();
      using var safety = new CancellationTokenSource(s_safetyNet);
      Task completed;
      using(var inbox = new ActivationInbox(key, errors.Enqueue))
      {
         inbox.Attach(_ => true);
         await using(var client = new NamedPipeClientStream(".", key, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly))
         {
            await client.ConnectAsync(safety.Token);
         }
         Assert.True(await ActivationInbox.SendAsync(key, new ActivationRequest([]), safety.Token));
         Assert.Single(errors);
         completed = inbox.Completion;
         inbox.Dispose();
         inbox.Dispose();
      }
      await completed.WaitAsync(safety.Token);
      Assert.Single(errors);
      using var replacement = new ActivationInbox(key, _ => { });
      replacement.Attach(_ => true);
      Assert.True(await ActivationInbox.SendAsync(key, new ActivationRequest([]), safety.Token));
   }



   /// <summary>A peer that has already closed when the reader turns to it is released before the next sender connects.</summary>
   [Fact]
   public async Task Listen_AlreadyClosedPeer_RecoversAfterReportedDisconnect()
   {
      string key = NewKey();
      var reported = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
      var received = new ConcurrentQueue<ActivationRequest>();
      var connectionContext = new DeferredConnectionContext();
      SynchronizationContext? previous = SynchronizationContext.Current;
      ActivationInbox inbox;
      try
      {
         SynchronizationContext.SetSynchronizationContext(connectionContext);
         inbox = new ActivationInbox(key, exception => reported.TrySetResult(exception));
      }
      finally
      {
         SynchronizationContext.SetSynchronizationContext(previous);
      }
      using ActivationInbox ownedInbox = inbox;
      inbox.Attach(value =>
      {
         received.Enqueue(value);
         return true;
      });
      using var safety = new CancellationTokenSource(s_safetyNet);
      await using(var client = new NamedPipeClientStream(".", key, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly))
      {
         await client.ConnectAsync(safety.Token);
      }
      (SendOrPostCallback callback, object? state) = await connectionContext.Continuation.Task.WaitAsync(safety.Token);
      // Resume the listener only after the peer has closed, so its first read finds a broken pipe.
      var resume = Task.Run(() => callback(state), safety.Token);
      Assert.IsAssignableFrom<IOException>(await reported.Task.WaitAsync(safety.Token));
      var request = ActivationRequest.Create(["after closed peer.md"], Path.GetTempPath());
      Assert.True(await ActivationInbox.SendAsync(key, request, safety.Token));
      Assert.Equal(request.Paths, Assert.Single(received).Paths);
      await resume.WaitAsync(safety.Token);
   }



   /// <summary>A pipe name no other test or process uses.</summary>
   private static string NewKey() => "k22-mv-tests-" + Guid.NewGuid().ToString("N");



   /// <summary>Sends one raw frame and returns the reader's answer byte, or -1 when the reader closed the connection without answering.</summary>
   private static async Task<int> SendFrame(string key, int declaredLength, byte[] payload, CancellationToken cancellationToken)
   {
      await using var client = new NamedPipeClientStream(".", key, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
      await client.ConnectAsync(cancellationToken);
      byte[] length = new byte[4];
      BinaryPrimitives.WriteInt32LittleEndian(length, declaredLength);
      byte[] response = new byte[1];
      try
      {
         await client.WriteAsync(length, cancellationToken);
         await client.WriteAsync(payload, cancellationToken);
         await client.FlushAsync(cancellationToken);
         return await client.ReadAsync(response, cancellationToken) == 1 ? response[0] : -1;
      }
      catch(IOException)
      {
         // Windows may report a broken pipe instead of end-of-stream once the reader has rejected the frame.
         return -1;
      }
   }



   /// <summary>Captures the listener's first continuation so a test decides when it resumes.</summary>
   private sealed class DeferredConnectionContext : SynchronizationContext
   {
      /// <summary>The captured continuation and its state.</summary>
      internal TaskCompletionSource<(SendOrPostCallback Callback, object? State)> Continuation { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);



      /// <summary>Holds the first posted continuation instead of running it.</summary>
      public override void Post(SendOrPostCallback callback, object? state) => Continuation.TrySetResult((callback, state));
   }
}
