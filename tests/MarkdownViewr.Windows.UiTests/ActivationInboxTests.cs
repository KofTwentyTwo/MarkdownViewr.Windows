/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Real current-user pipes prove bounded native delivery without taking desktop input or launching an app.</summary>
public sealed class ActivationInboxTests
{
   /// <summary>A startup request survives until a reader dispatcher attaches; rejected dispatch returns a negative acknowledgement.</summary>
   [Fact]
   public async Task Send_QueuedThenAttached_DeliversOnceAndHonorsAcknowledgement()
   {
      string key = "k22-mv-tests-" + Guid.NewGuid().ToString("N");
      var received = new ConcurrentQueue<ActivationRequest>();
      var errors = new ConcurrentQueue<Exception>();
      using var inbox = new ActivationInbox(key, errors.Enqueue);
      var request = ActivationRequest.Create(["sender & café.md"], Path.GetTempPath());
      using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
      Assert.True(await ActivationInbox.SendAsync(key, request, deadline.Token));
      Assert.Empty(received);
      inbox.Attach(value => { received.Enqueue(value); return true; });
      Assert.Equal(request.Paths, Assert.Single(received).Paths);
      Assert.True(await ActivationInbox.SendAsync(key, request, deadline.Token));
      Assert.Equal(2, received.Count);
      inbox.Attach(_ => false);
      Assert.False(await ActivationInbox.SendAsync(key, request, deadline.Token));
      Assert.Equal(2, received.Count);
      Assert.Empty(errors);
   }



   /// <summary>Oversized frames, unknown JSON fields and relative pipe paths are rejected, then a real valid peer still connects.</summary>
   [Fact]
   public async Task Listen_MalformedPeers_RejectsAndRecovers()
   {
      string key = "k22-mv-tests-" + Guid.NewGuid().ToString("N");
      var received = new ConcurrentQueue<ActivationRequest>();
      var errors = new ConcurrentQueue<Exception>();
      using var inbox = new ActivationInbox(key, errors.Enqueue);
      inbox.Attach(value => { received.Enqueue(value); return true; });
      using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
      await SendMalformed(key, ActivationRequest.MaximumPayloadBytes + 1, [], deadline.Token);
      byte[] unknown = Encoding.UTF8.GetBytes("{\"Paths\":[],\"Command\":\"execute\"}");
      await SendMalformed(key, unknown.Length, unknown, deadline.Token);
      byte[] relative = Encoding.UTF8.GetBytes("{\"Paths\":[\"relative.md\"]}");
      await SendMalformed(key, relative.Length, relative, deadline.Token);
      var request = ActivationRequest.Create(["valid.md"], Path.GetTempPath());
      Assert.True(await ActivationInbox.SendAsync(key, request, deadline.Token));
      Assert.Equal(request.Paths, Assert.Single(received).Paths);
      Assert.Equal(3, errors.Count);
   }



   /// <summary>A disconnected sender never blocks the next activation and shutdown releases the pipe without leaking an owned connection.</summary>
   [Fact]
   public async Task Listen_DisconnectedPeer_RecoversAndReleasesPipe()
   {
      string key = "k22-mv-tests-" + Guid.NewGuid().ToString("N");
      var errors = new ConcurrentQueue<Exception>();
      using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
      Task completed;
      using(var inbox = new ActivationInbox(key, errors.Enqueue))
      {
         inbox.Attach(_ => true);
         await using(var client = new NamedPipeClientStream(".", key, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly))
         {
            await client.ConnectAsync(deadline.Token);
         }
         Assert.True(await ActivationInbox.SendAsync(key, new ActivationRequest([]), deadline.Token));
         Assert.Single(errors);
         completed = inbox.Completion;
         inbox.Dispose();
         inbox.Dispose();
      }
      await completed.WaitAsync(deadline.Token);
      using var replacement = new ActivationInbox(key, _ => { });
      replacement.Attach(_ => true);
      Assert.True(await ActivationInbox.SendAsync(key, new ActivationRequest([]), deadline.Token));
   }



   /// <summary>An already-closed peer must release its native connection before another real activation.</summary>
   [Fact]
   public async Task Listen_AlreadyClosedPeer_RecoversAfterReportedDisconnect()
   {
      string key = "k22-mv-tests-" + Guid.NewGuid().ToString("N");
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
      inbox.Attach(value => { received.Enqueue(value); return true; });
      using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
      await using(var client = new NamedPipeClientStream(".", key, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly))
      {
         await client.ConnectAsync(deadline.Token);
      }
      (SendOrPostCallback callback, object? state) = await connectionContext.Continuation.Task.WaitAsync(deadline.Token);
      // Resume the real server after the real peer closes, exercising the synchronous broken-read path.
      var resume = Task.Run(() => callback(state), deadline.Token);
      Assert.IsAssignableFrom<IOException>(await reported.Task.WaitAsync(deadline.Token));
      var request = ActivationRequest.Create(["after closed peer.md"], Path.GetTempPath());
      Assert.True(await ActivationInbox.SendAsync(key, request, deadline.Token));
      Assert.Equal(request.Paths, Assert.Single(received).Paths);
      await resume.WaitAsync(deadline.Token);
   }



   /// <summary>Defers only the first real server continuation, making the already-closed native peer schedule reproducible.</summary>
   private sealed class DeferredConnectionContext : SynchronizationContext
   {
      internal TaskCompletionSource<(SendOrPostCallback Callback, object? State)> Continuation { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);



      /// <summary>Captures the actual asynchronous continuation without changing pipe bytes, handles or acknowledgements.</summary>
      public override void Post(SendOrPostCallback callback, object? state) => Continuation.TrySetResult((callback, state));
   }



   /// <summary>Sends an intentionally invalid frame and waits for the actual server to close it before starting the next peer.</summary>
   private static async Task SendMalformed(string key, int declaredLength, byte[] payload, CancellationToken cancellationToken)
   {
      await using var client = new NamedPipeClientStream(".", key, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
      await client.ConnectAsync(cancellationToken);
      byte[] length = new byte[4];
      BinaryPrimitives.WriteInt32LittleEndian(length, declaredLength);
      await client.WriteAsync(length, cancellationToken);
      await client.WriteAsync(payload, cancellationToken);
      await client.FlushAsync(cancellationToken);
      byte[] response = new byte[1];
      try
      {
         Assert.Equal(0, await client.ReadAsync(response, cancellationToken));
      }
      catch(IOException)
      {
         // Windows may report a broken pipe rather than EOF after the server rejects the malformed frame.
      }
   }
}
