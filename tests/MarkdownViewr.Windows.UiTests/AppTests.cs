/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using FlaUI.Core.AutomationElements;
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

}



/// <summary>UI tests share one desktop, so the collection never runs in parallel.</summary>
[CollectionDefinition("Desktop", DisableParallelization = true)]
public class DesktopCollectionDefinition
{
}
