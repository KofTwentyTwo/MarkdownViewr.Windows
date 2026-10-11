/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text;
using FlaUI.Core.AutomationElements;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Observes real loaded timestamps and stale state while a document cannot be reread.</summary>
public partial class AppTests
{
   /// <summary>A locked save retains the displayed snapshot and stale action; a successful reload commits new metadata and clears it.</summary>
   [Fact]
   public void Footer_LockedSaveAndReload_RetainsSnapshotUntilSuccessfulRead()
   {
      using var session = new AppSession(document: "# Footer original");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Footer original")), "the footer fixture", TimeSpan.FromSeconds(30));
      AutomationElement timestamp = session.WaitForElement("FileModifiedText");
      Assert.StartsWith("Modified ", timestamp.Name, StringComparison.Ordinal);
      Assert.True(session.WaitForElement("OpenDocumentFolderButton").IsEnabled);
      string original = timestamp.Name;
      string path = Path.Combine(session.DataDirectory, "document.md");
      using(var locked = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None))
      {
         byte[] updated = Encoding.UTF8.GetBytes("# Footer changed while locked");
         locked.Write(updated);
         locked.SetLength(updated.Length);
         locked.Flush(flushToDisk: true);
         session.WaitForElement("FileStaleButton");
         session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("StatusText")) is { } status && status.Name.Contains("previous version", StringComparison.Ordinal) ? status : null, "the failed locked reload retaining its old snapshot");
         Assert.Equal(original, session.WaitForElement("FileModifiedText").Name);
         Assert.NotNull(session.FindInApp(cf => cf.ByName("Footer original")));
      }
      session.InvokeMenuItem("File", "ReloadMenuItem");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Footer changed while locked")), "the successful footer reload");
      session.WaitForElementGone("FileStaleButton");
      Assert.True(session.WaitForElement("OpenDocumentFolderButton").IsEnabled);
      RetainDocumentWindow(session, session.MainWindow, "footer-after-locked-reload");
   }
}
