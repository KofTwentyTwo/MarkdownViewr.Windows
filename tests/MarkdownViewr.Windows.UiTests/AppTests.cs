/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Net;
using System.Net.Sockets;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Patterns;
using Xunit.Abstractions;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>
/// End-to-end tests of the real app: the AppKit shell pieces and the document surface.
/// UI tests share one desktop, so they never run in parallel.
/// </summary>
[Collection("Desktop")]
public class AppTests
{
   private readonly ITestOutputHelper _output;



   /// <summary>Records input observations in both passing and failing test results.</summary>
   public AppTests(ITestOutputHelper output) => _output = output;



   /// <summary>Splash: shows, then dismisses itself.</summary>
   [Fact]
   public void Splash_ShowsThenDismissesItself()
   {
      using var session = new AppSession(showSplash: true);
      session.WaitForElement("AppKitSplash");
      session.WaitForElementGone("AppKitSplash", TimeSpan.FromSeconds(15));
   }



   /// <summary>About: shows the build version.</summary>
   [Fact]
   public void About_ShowsTheBuildVersion()
   {
      using var session = new AppSession();
      session.InvokeMenuItem("Help", "AboutMenuItem");
      AutomationElement version = session.WaitForElement("AboutVersionText");
      Assert.StartsWith("Version ", version.Name, StringComparison.Ordinal);
      session.ClickButton("Close");
      session.WaitForElementGone("AboutVersionText");
   }



   /// <summary>Activity log: opens a window showing the log.</summary>
   [Fact]
   public void ActivityLog_OpensAWindowShowingTheLog()
   {
      using var session = new AppSession();
      session.InvokeMenuItem("Help", "ActivityLogMenuItem");
      string title = AppIdentity.Info.DisplayName + " — Activity log";
      session.WaitFor(
         () => session.TopLevelWindows().FirstOrDefault(window => string.Equals(window.Name, title, StringComparison.Ordinal)),
         "the activity log window");
   }



   /// <summary>Theme: the choice is saved to settings.json.</summary>
   [Fact]
   public void ThemeChoice_IsSaved()
   {
      using var session = new AppSession();
      session.InvokeMenuItem("View", "ThemeDark");
      _output.WriteLine(session.LastInvocation);
      _output.WriteLine(session.LastMouseInput);
      try
      {
         session.WaitFor(
            () => session.ReadSettings().Contains("\"Theme\": \"Dark\"", StringComparison.Ordinal) ? session.MainWindow : null,
            "settings.json to record the dark theme");
      }
      catch(TimeoutException exception)
      {
         throw new TimeoutException($"{exception.Message} {session.LastInvocation} {session.LastMouseInput} Settings: {session.ReadSettings()}. App logs: {session.ReadLogs()}", exception);
      }
   }



   /// <summary>Updates: a build that was not installed by Velopack says updates need an install.</summary>
   [Fact]
   public void CheckForUpdates_InALooseBuild_SaysUpdatesNeedAnInstall()
   {
      using var session = new AppSession();
      session.InvokeMenuItem("Help", "CheckForUpdatesMenuItem");
      session.WaitFor(
         () => session.FindInApp(cf => cf.ByName("Updates are only available in installed builds.")),
         "the not-installed message");
      session.ClickButton("OK");
   }



   /// <summary>The launched document appears in the actual browser accessibility tree.</summary>
   [Fact]
   public void OpenDocument_GfmDocument_ShowsRenderedContent()
   {
      using var session = new AppSession(document: "# Reader acceptance\n\n| Column | Value |\n| --- | --- |\n| One | Two |\n\n- [x] Finished\n\n<script>document.body.textContent='UNSAFE EXECUTION'</script>");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Reader acceptance")), "the rendered document heading", TimeSpan.FromSeconds(30));
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Two")), "the rendered table cell");
      Assert.Null(session.FindInApp(cf => cf.ByName("UNSAFE EXECUTION")));
      Assert.Contains("Opened a Markdown document.", session.ReadLogs(), StringComparison.Ordinal);
   }



   /// <summary>Reload uses the actual source file again instead of preserving an obsolete rendered snapshot.</summary>
   [Fact]
   public void Reload_ChangedDocument_ShowsNewContent()
   {
      using var session = new AppSession(document: "# Before reload");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Before reload")), "the initial heading", TimeSpan.FromSeconds(30));
      File.WriteAllText(Path.Combine(session.DataDirectory, "document.md"), "# After reload");
      session.InvokeMenuItem("File", "ReloadMenuItem");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("After reload")), "the reloaded heading");
      Assert.Null(session.FindInApp(cf => cf.ByName("Before reload")));
   }

   /// <summary>Saving through a replacement file updates the real browser without invoking Reload.</summary>
   [Fact]
   public void LiveReload_AtomicReplacement_ShowsNewContent()
   {
      using var session = new AppSession(document: "# Before automatic reload");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Before automatic reload")), "the initial heading", TimeSpan.FromSeconds(30));
      string replacement = Path.Combine(session.DataDirectory, "replacement.tmp");
      File.WriteAllText(replacement, "# After automatic reload");
      File.Move(replacement, Path.Combine(session.DataDirectory, "document.md"), overwrite: true);
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("After automatic reload")), "the automatically reloaded heading");
      Assert.Contains("Reloaded a Markdown document.", session.ReadLogs(), StringComparison.Ordinal);
   }



   /// <summary>A missing file preserves the displayed snapshot and recovers when the path reappears.</summary>
   [Fact]
   public void LiveReload_DeletedThenRecreated_PreservesAndRecovers()
   {
      using var session = new AppSession(document: "# Last readable version");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Last readable version")), "the initial heading", TimeSpan.FromSeconds(30));
      string path = Path.Combine(session.DataDirectory, "document.md");
      File.Delete(path);
      session.WaitFor(
         () => session.FindInApp(cf => cf.ByName("The document is missing. Showing the last version; it will reload when restored.")),
         "the missing-file status");
      Assert.NotNull(session.FindInApp(cf => cf.ByName("Last readable version")));
      File.WriteAllText(path, "# Recovered document");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Recovered document")), "the recreated document heading");
   }



   /// <summary>The application retries a brief exclusive editor lock instead of dropping its pending save.</summary>
   [Fact]
   public void LiveReload_EditorTemporarilyLocksFile_Retries()
   {
      using var session = new AppSession(document: "# Before editor lock");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Before editor lock")), "the initial heading", TimeSpan.FromSeconds(30));
      using(var file = new FileStream(Path.Combine(session.DataDirectory, "document.md"), FileMode.Create, FileAccess.Write, FileShare.None))
      {
         using var writer = new StreamWriter(file);
         writer.Write("# After editor lock");
         writer.Flush();
         Thread.Sleep(800);
      }
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("After editor lock")), "the unlocked save");
   }



   /// <summary>Reload retains relative position when a save changes the document's total height.</summary>
   [Fact]
   public void LiveReload_HeightChanges_RetainsRelativeScroll()
   {
      string paragraphs = string.Join("\n\n", Enumerable.Range(0, 200).Select(index => $"Paragraph {index}: a document long enough to scroll."));
      using var session = new AppSession(document: "# Scroll before\n\n" + paragraphs);
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Scroll before")), "the initial heading", TimeSpan.FromSeconds(30));
      AutomationElement scroller = FindDocumentScroller(session);
      scroller.Patterns.Scroll.Pattern.SetScrollPercent(-1, 80);
      session.WaitFor(
         () => scroller.Patterns.Scroll.Pattern.VerticalScrollPercent.Value > 75 ? scroller : null,
         "the document to scroll");
      File.WriteAllText(Path.Combine(session.DataDirectory, "document.md"), "# Scroll after\n\n" + paragraphs + "\n\n" + paragraphs);
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Scroll after")), "the reloaded heading");
      session.WaitFor(
         () =>
         {
            AutomationElement updated = FindDocumentScroller(session);
            return updated.Patterns.Scroll.Pattern.VerticalScrollPercent.Value > 75 ? updated : null;
         },
         "relative scroll restoration");
   }



   /// <summary>Remote image markup cannot connect to a listening server even when the parser retains its URL.</summary>
   [Fact]
   public void OpenDocument_RemoteImage_DoesNotConnect()
   {
      using var listener = new TcpListener(IPAddress.Loopback, 0);
      listener.Start();
      int port = ((IPEndPoint)listener.LocalEndpoint).Port;
      using var session = new AppSession(document: $"# Network boundary\n\n![remote image](http://127.0.0.1:{port}/remote.png)");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Network boundary")), "the rendered document heading", TimeSpan.FromSeconds(30));
      Thread.Sleep(1500);
      Assert.False(listener.Pending(), "The document attempted to fetch its remote image.");
   }



   /// <summary>Finds the browser's actual scroll provider, which is separate from the native window.</summary>
   private static AutomationElement FindDocumentScroller(AppSession session)
   {
      return session.WaitFor(
         () => session.MainWindow.FindAllDescendants().FirstOrDefault(element =>
            element.Patterns.Scroll.TryGetPattern(out IScrollPattern? scroll) && scroll.VerticallyScrollable.Value),
         "the browser scroll provider");
   }


}



/// <summary>UI tests share one desktop, so the collection never runs in parallel.</summary>
[CollectionDefinition("Desktop", DisableParallelization = true)]
public class DesktopCollectionDefinition
{
}
