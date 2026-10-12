/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Covers picker/save races without filesystem timing or a desktop.</summary>
public sealed class DocumentLoadSchedulerTests
{
   /// <summary>An idle document request starts immediately and leaves no queued work.</summary>
   [Fact]
   public void TryBegin_Idle_StartsImmediately()
   {
      var queue = new DocumentLoadScheduler();
      Assert.True(queue.TryBegin("first.md", reload: false));
      Assert.Null(queue.Complete("first.md"));
   }



   /// <summary>A file selected while a save reload is active is preserved for the next load.</summary>
   [Fact]
   public void TryBegin_PickerDuringReload_QueuesTheSelection()
   {
      var queue = new DocumentLoadScheduler();
      Assert.True(queue.TryBegin("old.md", reload: true));
      Assert.False(queue.TryBegin("new.md", reload: false));
      Assert.Equal(("new.md", false), queue.Complete("old.md"));
   }



   /// <summary>Repeated save events consume one pending refresh rather than an unbounded queue.</summary>
   [Fact]
   public void TryBegin_RepeatedReloads_Coalesces()
   {
      var queue = new DocumentLoadScheduler();
      Assert.True(queue.TryBegin("document.md", reload: true));
      Assert.False(queue.TryBegin("document.md", reload: true));
      Assert.False(queue.TryBegin("document.md", reload: true));
      Assert.Equal(("document.md", true), queue.Complete("document.md"));
      Assert.True(queue.TryBegin("document.md", reload: true));
      Assert.Null(queue.Complete("document.md"));
   }



   /// <summary>An explicit picker selection takes priority over notifications from the previous document.</summary>
   [Fact]
   public void Complete_OpenAndReloadPending_PrioritizesOpen()
   {
      var queue = new DocumentLoadScheduler();
      Assert.True(queue.TryBegin("old.md", reload: true));
      Assert.False(queue.TryBegin("new.md", reload: false));
      Assert.False(queue.TryBegin("old.md", reload: true));
      Assert.Equal(("new.md", false), queue.Complete("old.md"));
      Assert.True(queue.TryBegin("new.md", reload: false));
      Assert.Null(queue.Complete("new.md"));
   }



   /// <summary>The user's most recent pending explicit selection wins.</summary>
   [Fact]
   public void Complete_MultipleSelections_UsesTheLatest()
   {
      var queue = new DocumentLoadScheduler();
      Assert.True(queue.TryBegin("first.md", reload: false));
      Assert.False(queue.TryBegin("second.md", reload: false));
      Assert.False(queue.TryBegin("third.md", reload: false));
      Assert.Equal(("third.md", false), queue.Complete("first.md"));
   }



   /// <summary>Reloading needs an established current document, even if the first open failed.</summary>
   [Fact]
   public void Complete_NoCurrentDocument_DiscardsPendingReload()
   {
      var queue = new DocumentLoadScheduler();
      Assert.True(queue.TryBegin("missing.md", reload: false));
      Assert.False(queue.TryBegin("missing.md", reload: true));
      Assert.Null(queue.Complete(null));
   }



   /// <summary>If a selected file fails to open, a pending save of the retained document is still processed.</summary>
   [Fact]
   public void Complete_FailedSelection_RetainsThePreviousDocumentRefresh()
   {
      var queue = new DocumentLoadScheduler();
      Assert.True(queue.TryBegin("old.md", reload: true));
      Assert.False(queue.TryBegin("missing.md", reload: false));
      Assert.False(queue.TryBegin("old.md", reload: true));
      Assert.Equal(("missing.md", false), queue.Complete("old.md"));
      Assert.True(queue.TryBegin("missing.md", reload: false));
      Assert.Equal(("old.md", true), queue.Complete("old.md"));
   }



   /// <summary>The loader returns to idle after a failed or completed request.</summary>
   [Fact]
   public void Complete_AfterFailure_AllowsAnotherOpen()
   {
      var queue = new DocumentLoadScheduler();
      Assert.True(queue.TryBegin("missing.md", reload: false));
      Assert.Null(queue.Complete(null));
      Assert.True(queue.TryBegin("existing.md", reload: false));
   }
}
