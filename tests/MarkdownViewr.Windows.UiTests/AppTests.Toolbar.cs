/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Drawing;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Drives actual reference toolbar commands and verifies their shared reader/menu state and literal editor boundary.</summary>
public partial class AppTests
{
   /// <summary>TOC/depth/source/theme/zoom toolbar actions change actual native/browser surfaces and agree with View.</summary>
   [Fact]
   public void Toolbar_ReadingCommandsAndMenus_RemainSynchronized()
   {
      using var session = new AppSession(document: "# Toolbar fixture\n\n## Child\n\n### Deep heading");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Toolbar fixture")), "the actual toolbar fixture", TimeSpan.FromSeconds(30));
      Assert.Null(session.FindInApp(cf => cf.ByAutomationId("ToolbarDepthButton")));
      Assert.False(session.WaitForElement("ToolbarPrintButton").IsOffscreen);
      Assert.True(session.WaitForElement("ToolbarPrintButton").IsEnabled);
      AssertCompactToolbarLayout(session);
      ClickToolbarCommand(session, "ToolbarOutlineButton");
      Assert.NotNull(session.WaitForElement("OutlineDepth"));
      session.WaitForElement("OutlineDepth").AsComboBox().Select(1);
      Assert.NotNull(session.WaitForElement("OutlineHeading:1"));
      Assert.Null(session.FindInApp(cf => cf.ByAutomationId("OutlineHeading:2")));
      using(var settings = JsonDocument.Parse(session.ReadSettings()))
      {
         Assert.Equal(2, settings.RootElement.GetProperty("OutlineDepth").GetInt32());
      }
      ClickToolbarCommand(session, "ToolbarSourceButton");
      AssertEqualSourcePanes(session);
      session.InvokeMenuItem("View", "SourceToggle");
      session.WaitForElementGone("SourceText");
      Assert.Equal(ToggleState.Off, session.WaitForElement("ToolbarSourceButton").Patterns.Toggle.Pattern.ToggleState.Value);
      ClickToolbarCommand(session, "ToolbarThemeButton");
      session.ClickElement("ToolbarTheme:GitHub Light");
      WaitForDocumentColor(session, Color.White);
      Assert.Equal("Document theme: GitHub Light", FindToolbarCommand(session, "ToolbarThemeButton").Name);
      double height = session.WaitForElement("mv-heading-0").BoundingRectangle.Height;
      SendReadingKey(VirtualKeyShort.ESCAPE);
      ClickToolbarCommand(session, "ZoomInButton");
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-0")) is { } glyph && glyph.BoundingRectangle.Height > height ? glyph : null, "actual glyph growth from toolbar zoom");
      ClickToolbarCommand(session, "ZoomResetButton");
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("ZoomResetButton").And(cf.ByName("Actual size (100%)"))), "the actual toolbar zoom percentage reset");
      session.InvokeMenuItem("View", "OutlineToggle");
      session.WaitForElementGone("OutlineDepth");
      Assert.Equal(ToggleState.Off, session.WaitForElement("ToolbarOutlineButton").Patterns.Toggle.Pattern.ToggleState.Value);
      Assert.Null(session.FindInApp(cf => cf.ByAutomationId("ToolbarDepthButton")));
      RetainDocumentWindow(session, session.MainWindow, "reference-toolbar-reading-actions");
   }



   /// <summary>Multiple editors offer real file/folder targets; one editor launches directly and overflow Print uses the owned draft.</summary>
   [Fact]
   public void Toolbar_EditorAndPrint_UsesLiteralTargetsAndOwnedDialogs()
   {
      using var session = new AppSession(document: "# Toolbar editor fixture", editorProbe: true, documentFileName: "notes & café $HOME.md");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Toolbar editor fixture")), "the actual toolbar editor fixture", TimeSpan.FromSeconds(30));
      AssertDisabledEditorExplanation(session);
      (string fileId, string folderId) = ConfigureToolbarEditors(session);
      ClickToolbarCommand(session, "ToolbarEditorButton");
      session.ClickElement("ToolbarEditor:" + folderId);
      using(JsonDocument result = ReadToolbarEditorResult(session))
      {
         Assert.Equal(session.DataDirectory, Assert.Single(result.RootElement.GetProperty("Arguments").EnumerateArray()).GetString());
      }
      File.Delete(Path.Combine(session.DataDirectory, "editor-result.json"));
      OpenSettingsAction(session, "ConfigureEditorMenuItem", "SettingsEditorsTab");
      session.WaitForElement("EditorChoice:" + folderId).Patterns.SelectionItem.Pattern.Select();
      session.ClickElement("EditorRemove");
      session.ClickButton("Save");
      session.WaitForElementGone("EditorPath");
      CloseSettings(session);
      using(var settings = JsonDocument.Parse(session.ReadSettings()))
      {
         Assert.Equal(fileId, Assert.Single(settings.RootElement.GetProperty("ConfiguredEditors").EnumerateArray()).GetProperty("Id").GetString());
      }
      ClickToolbarCommand(session, "ToolbarEditorButton");
      using(JsonDocument result = ReadToolbarEditorResult(session))
      {
         Assert.Equal(Path.Combine(session.DataDirectory, "notes & café $HOME.md"), Assert.Single(result.RootElement.GetProperty("Arguments").EnumerateArray()).GetString());
      }
      string saved = session.ReadSettings();
      ClickToolbarCommand(session, "ToolbarPrintButton");
      session.WaitForElement("PrintOptionsDialog");
      session.ClickButton("Cancel");
      session.WaitForElementGone("PrintOptionsDialog");
      Assert.Equal(saved, session.ReadSettings());
      Assert.DoesNotContain("notes & café", session.ReadLogs(), StringComparison.Ordinal);
      Assert.DoesNotContain("Unhandled UI exception", session.ReadLogs(), StringComparison.Ordinal);
      RetainDocumentWindow(session, session.MainWindow, "reference-toolbar-editor-print");
   }



   /// <summary>Physical hover exposes the actual disabled-command reason and mouse exit closes it without a UI exception.</summary>
   private static void AssertDisabledEditorExplanation(AppSession session)
   {
      AutomationElement disabledEditor = FindToolbarCommand(session, "ToolbarEditorButton");
      Assert.False(disabledEditor.IsEnabled);
      const string Explanation = "No editor is configured. Choose one in File → Settings → Editors.";
      Assert.Equal(Explanation, disabledEditor.Properties.HelpText.Value);
      session.MainWindow.SetForeground();
      Wait.UntilInputIsProcessed();
      Rectangle target = disabledEditor.BoundingRectangle;
      NativeMouse.MoveTo(new Point(target.Left + target.Width / 2, target.Top + target.Height / 2));
      AutomationElement tooltip = session.WaitForElement("EditorUnavailableTip");
      Assert.False(tooltip.IsOffscreen);
      Assert.True(string.Equals(tooltip.Name, Explanation, StringComparison.Ordinal) || tooltip.FindFirstDescendant(cf => cf.ByName(Explanation)) is not null, "The actual disabled-command tooltip must show the Settings route.");
      Assert.DoesNotContain("Unhandled UI exception", session.ReadLogs(), StringComparison.Ordinal);
      RetainDocumentWindow(session, session.MainWindow, "disabled-editor-native-tooltip");
      Rectangle document = session.WaitForElement("DocumentView").BoundingRectangle;
      NativeMouse.MoveTo(new Point(document.Left + 24, document.Top + 24));
      SendReadingKey(VirtualKeyShort.ESCAPE);
      session.WaitForElementGone("EditorUnavailableTip");
   }



   /// <summary>A committed editor change refreshes actual sibling commands and its later source save cannot restore the old list.</summary>
   [Fact]
   public void Toolbar_EditorBroadcast_PreservesSiblingMenuAndLaterWindowSave()
   {
      using var session = new AppSession(document: "# First toolbar sibling", editorProbe: true, documentFileName: "first.md",
         additionalDocuments: new Dictionary<string, string>(StringComparer.Ordinal) { ["second.md"] = "# Second toolbar sibling" });
      Window first = WaitForDocumentWindow(session, "first.md");
      Window second = WaitForDocumentWindow(session, "second.md");
      WaitForWindowHeading(session, second, "Second toolbar sibling");
      InvokeDocumentMenu(session, first, "SettingsMenuItem");
      session.WaitForElement("SettingsEditorsTab").Patterns.SelectionItem.Pattern.Select();
      session.ClickElement("ConfigureEditorMenuItem");
      session.EnterText("EditorName", "Shared editor");
      session.EnterText("EditorPath", ResolveEditorProbe());
      session.ClickButton("Save");
      session.WaitForElementGone("EditorPath");
      CloseSettings(session);
      AutomationElement command = session.WaitFor(() => second.FindFirstDescendant(cf => cf.ByAutomationId("ToolbarEditorButton").And(cf.ByName("Open in Shared editor"))), "the sibling's actual updated toolbar command");
      Assert.True(command.IsEnabled);
      second.SetForeground();
      command.Patterns.Invoke.Pattern.Invoke();
      using(JsonDocument result = ReadToolbarEditorResult(session))
      {
         Assert.Equal(Path.Combine(session.DataDirectory, "second.md"), Assert.Single(result.RootElement.GetProperty("Arguments").EnumerateArray()).GetString());
      }
      InvokeDocumentMenu(session, second, "SourceToggle", menuName: "View");
      session.WaitFor(() => second.FindFirstDescendant(cf => cf.ByAutomationId("SourceText")), "the sibling's later local source preference save");
      using(var settings = JsonDocument.Parse(session.ReadSettings()))
      {
         Assert.Equal("Shared editor", Assert.Single(settings.RootElement.GetProperty("ConfiguredEditors").EnumerateArray()).GetProperty("Name").GetString());
      }
      Assert.NotNull(WaitForWindowHeading(session, first, "First toolbar sibling"));
      Assert.Equal(2, DocumentWindows(session).Length);
      RetainDocumentWindow(session, second, "reference-toolbar-shared-editor");
   }



   /// <summary>Narrow-window overflow preserves actual source, theme, zoom and Print actions without clipping fixed TOC controls.</summary>
   [Fact]
   public void Toolbar_NarrowWindow_OverflowRetainsReadingAndPrintCommands()
   {
      using var session = new AppSession(document: "# Narrow toolbar fixture");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Narrow toolbar fixture")), "the narrow toolbar fixture", TimeSpan.FromSeconds(30));
      double width = session.MainWindow.BoundingRectangle.Width;
      session.MainWindow.Patterns.Transform.Pattern.Resize(650, session.MainWindow.BoundingRectangle.Height);
      session.WaitFor(() => session.MainWindow.BoundingRectangle.Width < width - 100 ? session.MainWindow : null, "the actual narrower native window");
      Assert.False(session.WaitForElement("ToolbarPrintButton").IsOffscreen);
      ClickToolbarCommand(session, "ToolbarSourceButton");
      AssertEqualSourcePanes(session);
      ClickToolbarCommand(session, "ToolbarThemeButton");
      session.ClickElement("ToolbarTheme:GitHub Light");
      WaitForDocumentColor(session, Color.White);
      ClickToolbarCommand(session, "ZoomInButton");
      AutomationElement reset = FindToolbarCommand(session, "ZoomResetButton");
      session.WaitFor(() => string.Equals(reset.Name, "Actual size (110%)", StringComparison.Ordinal) ? reset : null, "the actual narrow-toolbar zoom percentage");
      SendReadingKey(VirtualKeyShort.ESCAPE);
      string saved = session.ReadSettings();
      ClickToolbarCommand(session, "ToolbarPrintButton");
      session.WaitForElement("PrintOptionsDialog");
      session.ClickButton("Cancel");
      session.WaitForElementGone("PrintOptionsDialog");
      Assert.Equal(saved, session.ReadSettings());
      Assert.Equal("# Narrow toolbar fixture", session.WaitForElement("SourceText").Patterns.Value.Pattern.Value.Value);
      RetainDocumentWindow(session, session.MainWindow, "reference-toolbar-narrow-overflow");
   }



   /// <summary>Measures the native row at the current DPI, including aligned hit targets and the gap between command groups.</summary>
   private static void AssertCompactToolbarLayout(AppSession session)
   {
      AutomationElement fixedPrint = session.WaitForElement("ToolbarPrintButton");
      double scale = fixedPrint.BoundingRectangle.Height / 48;
      foreach(string identity in new[] { "ToolbarOutlineButton", "ToolbarSourceButton", "ZoomResetButton", "ToolbarThemeButton" })
      {
         AutomationElement command = session.WaitForElement(identity);
         double printCenter = fixedPrint.BoundingRectangle.Top + fixedPrint.BoundingRectangle.Height / 2;
         double commandCenter = command.BoundingRectangle.Top + command.BoundingRectangle.Height / 2;
         Assert.InRange(Math.Abs(printCenter - commandCenter), 0, 2 * scale);
      }
      Assert.InRange(session.WaitForElement("ReaderToolbar").BoundingRectangle.Height, 40 * scale, 52 * scale);
      Assert.InRange(session.WaitForElement("ToolbarSourceButton").BoundingRectangle.Left - fixedPrint.BoundingRectangle.Right, 0, 36 * scale);
   }



   /// <summary>Only owned controls are invoked; responsive overflow follows the same visible command path a user takes.</summary>
   private static void ClickToolbarCommand(AppSession session, string identity)
   {
      _ = FindToolbarCommand(session, identity);
      session.ClickElement(identity);
   }



   /// <summary>Reads native commands in the bar or its already-open popup, opening More only when needed.</summary>
   private static AutomationElement FindToolbarCommand(AppSession session, string identity)
   {
      AutomationElement? command = session.FindInApp(cf => cf.ByAutomationId(identity));
      if(command is null || command.IsOffscreen)
      {
         AutomationElement toolbar = session.WaitForElement("ReaderToolbar");
         AutomationElement more = session.WaitFor(() => toolbar.FindFirstDescendant(cf => cf.ByAutomationId("MoreButton")), "the owned toolbar overflow button");
         more.Patterns.Invoke.Pattern.Invoke();
      }
      return session.WaitForElement(identity);
   }



   /// <summary>Configures the real native editor list with the existing test-only process fixture.</summary>
   private static (string FileId, string FolderId) ConfigureToolbarEditors(AppSession session)
   {
      string executable = ResolveEditorProbe();
      OpenSettingsAction(session, "ConfigureEditorMenuItem", "SettingsEditorsTab");
      session.EnterText("EditorName", "File editor");
      session.EnterText("EditorPath", executable);
      session.ClickElement("EditorAdd");
      session.EnterText("EditorName", "Folder editor");
      session.EnterText("EditorPath", executable);
      session.WaitForElement("EditorFolder").Patterns.Toggle.Pattern.Toggle();
      session.ClickButton("Save");
      session.WaitForElementGone("EditorPath");
      CloseSettings(session);
      using var settings = JsonDocument.Parse(session.ReadSettings());
      JsonElement editors = settings.RootElement.GetProperty("ConfiguredEditors");
      return (editors[0].GetProperty("Id").GetString()!, editors[1].GetProperty("Id").GetString()!);
   }



   /// <summary>Returns real probe output after the launched process writes its literal arguments.</summary>
   private static JsonDocument ReadToolbarEditorResult(AppSession session)
   {
      string path = Path.Combine(session.DataDirectory, "editor-result.json");
      session.WaitFor(() => File.Exists(path) ? session.MainWindow : null, "the actual toolbar-launched editor fixture output");
      return JsonDocument.Parse(File.ReadAllText(path));
   }
}
