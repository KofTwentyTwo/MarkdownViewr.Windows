/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Exercises independent native document windows and private history through real controls and owned file fixtures.</summary>
public partial class AppTests
{
   /// <summary>Literal startup paths open separate readers; zoom and Ctrl+W leave the sibling's actual glyphs and watcher intact.</summary>
   [Fact]
   public void Windows_StartupFilesAndClose_PreservesSiblingReader()
   {
      using var session = new AppSession(document: "# First independent reader", documentFileName: "one.mdown",
         additionalDocuments: new Dictionary<string, string>(StringComparer.Ordinal) { ["two.mkd"] = "# Second independent reader" });
      Window first = WaitForDocumentWindow(session, "one.mdown");
      Window second = WaitForDocumentWindow(session, "two.mkd");
      AutomationElement firstHeading = WaitForWindowHeading(session, first, "First independent reader");
      AutomationElement secondHeading = WaitForWindowHeading(session, second, "Second independent reader");
      double firstHeight = firstHeading.BoundingRectangle.Height;
      double secondHeight = secondHeading.BoundingRectangle.Height;
      Assert.Equal(2, DocumentWindows(session).Length);
      first.SetForeground();
      WaitForWindowElement(session, first, "ZoomInButton").Patterns.Invoke.Pattern.Invoke();
      session.WaitFor(() => firstHeading.BoundingRectangle.Height > firstHeight && WaitForWindowElement(session, first, "ZoomResetButton").IsEnabled ? firstHeading : null, "the first window's actual larger glyph");
      Assert.Equal(secondHeight, secondHeading.BoundingRectangle.Height);
      Assert.Equal("Actual size (100%)", WaitForWindowElement(session, second, "ZoomResetButton").Name);
      first.SetForeground();
      WaitForWindowElement(session, first, "ZoomResetButton").Focus();
      Wait.UntilInputIsProcessed();
      SendReadingKey(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_W);
      session.WaitFor(() => DocumentWindows(session).Length == 1 ? second : null, "Ctrl+W closing only its own document window");
      Assert.False(session.App.HasExited);
      File.WriteAllText(Path.Combine(session.DataDirectory, "two.mkd"), "# Sibling reload after close");
      AutomationElement reloaded = WaitForWindowHeading(session, second, "Sibling reload after close");
      Assert.Equal(secondHeight, reloaded.BoundingRectangle.Height);
      RetainDocumentWindow(session, second, "surviving-window");
   }



   /// <summary>New windows stay blank; recent activation focuses existing readers, failed opens preserve them and clearing cannot be undone by stale preferences.</summary>
   [Fact]
   public void Windows_NewRecentAndClear_PreservesDocumentsAndHistory()
   {
      using var session = new AppSession(document: "# Recent first", documentFileName: "one.md",
         additionalDocuments: new Dictionary<string, string>(StringComparer.Ordinal) { ["two.markdown"] = "# Recent second" });
      Window first = WaitForDocumentWindow(session, "one.md");
      Window second = WaitForDocumentWindow(session, "two.markdown");
      WaitForWindowHeading(session, first, "Recent first");
      WaitForWindowHeading(session, second, "Recent second");
      string[] history = ReadRecentDocuments(session);
      Assert.Equal(2, history.Length);
      first.SetForeground();
      WaitForWindowElement(session, first, "ZoomResetButton").Focus();
      Wait.UntilInputIsProcessed();
      SendReadingKey(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_N);
      Window blank = session.WaitFor(() => DocumentWindows(session).FirstOrDefault(window => string.Equals(window.Name, AppIdentity.Info.DisplayName, StringComparison.Ordinal)), "a genuinely blank native Ctrl+N window").AsWindow();
      Assert.Null(blank.FindFirstDescendant(cf => cf.ByAutomationId("mv-heading-0")));
      Assert.Equal(3, DocumentWindows(session).Length);
      InvokeDocumentMenu(session, blank, "RecentDocument:" + Array.IndexOf(history, Path.Combine(session.DataDirectory, "one.md")), recent: true);
      Assert.Equal(3, DocumentWindows(session).Length);
      Assert.Equal(history, ReadRecentDocuments(session));
      InvokeDocumentMenu(session, blank, "CloseWindowMenuItem");
      InvokeDocumentMenu(session, second, "CloseWindowMenuItem");
      session.WaitFor(() => DocumentWindows(session).Length == 1 ? first : null, "the first reader remaining after both other windows close");
      File.Delete(Path.Combine(session.DataDirectory, "two.markdown"));
      InvokeDocumentMenu(session, first, "RecentDocument:" + Array.IndexOf(history, Path.Combine(session.DataDirectory, "two.markdown")), recent: true);
      Window missing = session.WaitFor(() => DocumentWindows(session).FirstOrDefault(window => string.Equals(window.Name, AppIdentity.Info.DisplayName, StringComparison.Ordinal)), "the separate missing-file window").AsWindow();
      session.WaitFor(() => missing.FindFirstDescendant(cf => cf.ByAutomationId("StatusText")) is { } status && status.Name.StartsWith("Could not open this document:", StringComparison.Ordinal) ? status : null, "the actual missing recent-file error");
      Assert.NotNull(first.FindFirstDescendant(cf => cf.ByName("Recent first")));
      Assert.Equal(history, ReadRecentDocuments(session));
      InvokeDocumentMenu(session, missing, "ClearRecentDocuments", recent: true);
      Assert.Empty(ReadRecentDocuments(session));
      InvokeDocumentMenu(session, first, "SourceToggle", menuName: "View");
      session.WaitFor(() => first.FindFirstDescendant(cf => cf.ByAutomationId("SourceText")), "the first window saving its source preference");
      Assert.Empty(ReadRecentDocuments(session));
      Assert.True(File.Exists(Path.Combine(session.DataDirectory, "one.md")));
      RetainDocumentWindow(session, first, "cleared-history-reader");
      InvokeDocumentMenu(session, first, "ExitMenuItem");
      session.WaitFor(() => session.App.HasExited ? session.Automation.GetDesktop() : null, "File/Exit closing all document windows");
   }



   /// <summary>The real Windows picker opens multiple literal paths in separate readers and cancellation preserves them.</summary>
   [Fact]
   public void Windows_FilePickerMultipleAndCancel_PreservesExistingReader()
   {
      using var session = new AppSession(document: "# Original picker reader", documentFileName: "original.txt",
         documentAssets: new Dictionary<string, byte[]>(StringComparer.Ordinal)
         {
            ["picked & caf\u00e9.mdown"] = Encoding.UTF8.GetBytes("# First picked reader"),
            ["picked two.mkd"] = Encoding.UTF8.GetBytes("# Second picked reader"),
         });
      Window original = WaitForDocumentWindow(session, "original.txt");
      WaitForWindowHeading(session, original, "Original picker reader");
      InvokeDocumentMenu(session, original, "OpenMenuItem");
      AutomationElement filename = session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("1001").And(cf.ByControlType(ControlType.Edit))), "the real Windows multi-file picker filename field");
      string paths = "\"" + Path.Combine(session.DataDirectory, "picked & caf\u00e9.mdown") + "\" \"" + Path.Combine(session.DataDirectory, "picked two.mkd") + "\"";
      EnterPickerFilename(session, filename, paths);
      session.ClickButton("Open");
      Window first = WaitForDocumentWindow(session, "picked & caf\u00e9.mdown");
      Window second = WaitForDocumentWindow(session, "picked two.mkd");
      WaitForWindowHeading(session, first, "First picked reader");
      WaitForWindowHeading(session, second, "Second picked reader");
      Assert.Equal(3, DocumentWindows(session).Length);
      Assert.NotNull(original.FindFirstDescendant(cf => cf.ByName("Original picker reader")));
      string[] history = ReadRecentDocuments(session);
      Assert.Equal(3, history.Length);
      InvokeDocumentMenu(session, original, "OpenMenuItem");
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("1001").And(cf.ByControlType(ControlType.Edit))), "the reopened native file picker");
      session.ClickButton("Cancel");
      session.WaitForElementGone("1001");
      Assert.Equal(3, DocumentWindows(session).Length);
      Assert.Equal(history, ReadRecentDocuments(session));
      Assert.NotNull(original.FindFirstDescendant(cf => cf.ByName("Original picker reader")));
      RetainDocumentWindow(session, first, "multiple-picker-reader");
   }



   /// <summary>Counts only real app document windows, excluding native menu, log and picker surfaces.</summary>
   private static Window[] DocumentWindows(AppSession session)
      => session.TopLevelWindows().Where(window => window.FindFirstDescendant(cf => cf.ByAutomationId("DocumentView")) is not null).Select(window => window.AsWindow()).ToArray();



   /// <summary>Matches the native document window's basename rather than an arbitrary WebView child title.</summary>
   private static Window WaitForDocumentWindow(AppSession session, string filename)
      => session.WaitFor(() => DocumentWindows(session).FirstOrDefault(window => window.Name.StartsWith(filename + " \u2014 ", StringComparison.Ordinal)), "the separate " + filename + " reader", TimeSpan.FromSeconds(30)).AsWindow();



   /// <summary>Requires the actual browser heading within the specified window, including asynchronous reload completion.</summary>
   private static AutomationElement WaitForWindowHeading(AppSession session, Window window, string title)
      => session.WaitFor(() => window.FindFirstDescendant(cf => cf.ByAutomationId("mv-heading-0").And(cf.ByName(title))), "the actual " + title + " glyph in its own reader", TimeSpan.FromSeconds(30));



   /// <summary>Looks up native controls only in their owning document window.</summary>
   private static AutomationElement WaitForWindowElement(AppSession session, Window window, string id)
      => session.WaitFor(() => window.FindFirstDescendant(cf => cf.ByAutomationId(id)), "the native " + id + " control in its own window");



   /// <summary>Reads the persisted private history of owned fixture paths after the native commands.</summary>
   private static string[] ReadRecentDocuments(AppSession session)
   {
      using var settings = JsonDocument.Parse(session.ReadSettings());
      return settings.RootElement.GetProperty("RecentFiles").EnumerateArray().Select(value => value.GetString()!).ToArray();
   }



   /// <summary>Opens the requested window's native menu and invokes its actual item, including the recent submenu.</summary>
   private static void InvokeDocumentMenu(AppSession session, Window window, string id, bool recent = false, string menuName = "File")
   {
      window.SetForeground();
      AutomationElement menu = session.WaitFor(() => window.FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuItem).And(cf.ByName(menuName))), "the window's native " + menuName + " menu");
      menu.Patterns.ExpandCollapse.Pattern.Expand();
      if(recent)
      {
         AutomationElement submenu = session.WaitFor(() => session.TopLevelWindows().Select(root => root.FindFirstDescendant(cf => cf.ByAutomationId("RecentDocumentsMenu"))).FirstOrDefault(element => element is not null), "the private recent-file submenu");
         submenu.Patterns.ExpandCollapse.Pattern.Expand();
      }
      AutomationElement item = session.WaitFor(() => session.TopLevelWindows().Select(root => root.FindFirstDescendant(cf => cf.ByAutomationId(id))).FirstOrDefault(element => element is not null), "the requested native " + id + " command");
      item.Patterns.Invoke.Pattern.Invoke();
      Wait.UntilInputIsProcessed();
   }



   /// <summary>Retains only the owned fixture window; hosted installed evidence collects these actual pixels.</summary>
   private static void RetainDocumentWindow(AppSession session, Window window, string name)
   {
      string directory = Path.Combine(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? session.DataDirectory, "artifacts", "installed-smoke-ui");
      Directory.CreateDirectory(directory);
      window.SetForeground();
      window.CaptureToFile(Path.Combine(directory, name + "-" + Guid.NewGuid().ToString("N") + ".png"));
   }
}
