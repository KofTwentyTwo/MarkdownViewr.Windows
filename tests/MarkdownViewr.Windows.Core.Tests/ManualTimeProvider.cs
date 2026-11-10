/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>
/// A clock whose timers fire only when a test says so, so a deadline can be reached without
/// waiting for it and can never be reached by a slow machine.
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
   private readonly Lock _gate = new();
   private readonly List<ManualTimer> _timers = [];
   private TaskCompletionSource _created = new(TaskCreationOptions.RunContinuationsAsynchronously);



   /// <summary>Records the timer instead of scheduling it.</summary>
   public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
   {
      var timer = new ManualTimer(callback, state);
      lock(_gate)
      {
         _timers.Add(timer);
         _created.TrySetResult();
      }
      return timer;
   }



   /// <summary>Completes once code under test has created a timer since the last call.</summary>
   public Task NextTimerAsync(CancellationToken cancellationToken)
   {
      Task created;
      lock(_gate)
      {
         created = _created.Task;
         if(created.IsCompleted)
         {
            _created = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
         }
      }
      return created.WaitAsync(cancellationToken);
   }



   /// <summary>Fires every timer created so far that has not been disposed, as if each deadline had passed.</summary>
   public void Elapse()
   {
      ManualTimer[] pending;
      lock(_gate)
      {
         pending = _timers.ToArray();
         _timers.Clear();
      }
      foreach(ManualTimer timer in pending)
      {
         timer.Fire();
      }
   }



   /// <summary>A timer that runs its callback once, on request, unless it was disposed first.</summary>
   private sealed class ManualTimer(TimerCallback callback, object? state) : ITimer
   {
      private int _disposed;



      /// <summary>Runs the callback unless the owner has already let go of the timer.</summary>
      public void Fire()
      {
         if(Volatile.Read(ref _disposed) == 0)
         {
            callback(state);
         }
      }



      /// <summary>Rescheduling is accepted and ignored: only <see cref="Elapse"/> fires a timer.</summary>
      public bool Change(TimeSpan dueTime, TimeSpan period) => true;



      /// <summary>Marks the timer dead so a later <see cref="Elapse"/> skips it.</summary>
      public void Dispose() => Interlocked.Exchange(ref _disposed, 1);



      /// <summary>Asynchronous disposal has nothing to wait for.</summary>
      public ValueTask DisposeAsync()
      {
         Dispose();
         return ValueTask.CompletedTask;
      }
   }
}
