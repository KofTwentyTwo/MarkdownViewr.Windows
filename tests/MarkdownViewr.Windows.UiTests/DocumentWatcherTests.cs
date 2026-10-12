/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Threading.Channels;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Checks the real directory subscription without reserving or launching a desktop window.</summary>
public sealed class DocumentWatcherTests : IDisposable
{
   private readonly string _root = Path.Combine(Path.GetTempPath(), "markdownviewr-watcher-" + Guid.NewGuid().ToString("N"));
   private readonly Channel<byte> _events = Channel.CreateUnbounded<byte>();



   /// <summary>Creates a unique filesystem fixture for each watcher scenario.</summary>
   public DocumentWatcherTests()
   {
      Directory.CreateDirectory(_root);
      File.WriteAllText(DocumentPath, "# Before");
   }



   /// <summary>The selected path whose replacement must remain observable.</summary>
   private string DocumentPath => Path.Combine(_root, "document.md");



   /// <summary>Regular save notifications reach the selected document subscription.</summary>
   [Fact]
   public async Task Watch_OrdinarySave_Notifies()
   {
      using DocumentWatcher watcher = CreateWatcher();
      File.WriteAllText(DocumentPath, "# After");
      await NextEventAsync();
      Assert.Equal("# After", File.ReadAllText(DocumentPath));
   }



   /// <summary>Write-then-rename saves replace the file without losing the subscription.</summary>
   [Fact]
   public async Task Watch_AtomicReplacement_ContinuesNotifying()
   {
      using DocumentWatcher watcher = CreateWatcher();
      string replacement = Path.Combine(_root, "replacement.tmp");
      File.WriteAllText(replacement, "# Replaced");
      File.Move(replacement, DocumentPath, overwrite: true);
      await NextEventAsync();
      Assert.Equal("# Replaced", File.ReadAllText(DocumentPath));
      File.WriteAllText(DocumentPath, "# Next save");
      await NextEventAsync();
      Assert.Equal("# Next save", File.ReadAllText(DocumentPath));
   }



   /// <summary>Watching the directory permits recovery when the selected file is deleted and recreated.</summary>
   [Fact]
   public async Task Watch_DeletionAndRecreation_NotifiesBoth()
   {
      using DocumentWatcher watcher = CreateWatcher();
      File.Delete(DocumentPath);
      await NextEventAsync();
      Assert.False(File.Exists(DocumentPath));
      File.WriteAllText(DocumentPath, "# Restored");
      await NextEventAsync();
      Assert.Equal("# Restored", File.ReadAllText(DocumentPath));
   }



   /// <summary>Moving the original file away and renaming another file into its place are both observed.</summary>
   [Fact]
   public async Task Watch_RenameAwayAndBack_NotifiesBoth()
   {
      using DocumentWatcher watcher = CreateWatcher();
      string backup = Path.Combine(_root, "backup.md");
      File.Move(DocumentPath, backup);
      await NextEventAsync();
      File.Move(backup, DocumentPath);
      await NextEventAsync();
      Assert.True(File.Exists(DocumentPath));
   }



   /// <summary>Unrelated files do not make the selected document reload.</summary>
   [Fact]
   public async Task Watch_UnrelatedFile_DoesNotNotify()
   {
      using DocumentWatcher watcher = CreateWatcher();
      File.WriteAllText(Path.Combine(_root, "other.md"), "# Other");
      await Task.Delay(750);
      Assert.False(_events.Reader.TryRead(out _));
      File.WriteAllText(DocumentPath, "# Selected");
      await NextEventAsync();
   }



   /// <summary>Disposal cancels pending debounce work and further directory events.</summary>
   [Fact]
   public async Task Dispose_PendingSave_DoesNotNotify()
   {
      DocumentWatcher watcher = CreateWatcher();
      File.WriteAllText(DocumentPath, "# Changed");
      watcher.Dispose();
      await Task.Delay(750);
      Assert.False(_events.Reader.TryRead(out _));
   }



   /// <summary>Callbacks use a private event channel and never touch GUI input.</summary>
   private DocumentWatcher CreateWatcher()
   {
      return new DocumentWatcher(DocumentPath, () => _events.Writer.TryWrite(0), () => _events.Writer.TryComplete(new IOException("Watcher error.")));
   }



   /// <summary>Waits for the authoritative event, with a bounded failure deadline.</summary>
   private async Task NextEventAsync()
   {
      await _events.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
   }



   /// <summary>Deletes only this fixture's unique explicitly created temporary root.</summary>
   public void Dispose() => Directory.Delete(_root, recursive: true);
}
