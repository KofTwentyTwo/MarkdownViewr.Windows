/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Drives actual Settings navigation and checks that parent cancellation retains the saved reader state.</summary>
public partial class AppTests
{
   /// <summary>Explicit startup panel defaults apply to later windows, while the current reader keeps its local visibility choices.</summary>
   [Fact]
   public void Settings_NewWindowPanelDefaults_LeaveExistingReaderUnchanged()
   {
      using var session = new AppSession(document: "# Default visibility fixture");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Default visibility fixture")), "the default visibility fixture", TimeSpan.FromSeconds(30));
      Assert.Null(session.FindInApp(cf => cf.ByAutomationId("OutlineDepth")));
      Assert.Null(session.FindInApp(cf => cf.ByAutomationId("SourceText")));
      session.InvokeMenuItem("File", "SettingsMenuItem");
      session.WaitForElement("SettingsAppearanceTab").Patterns.SelectionItem.Pattern.Select();
      AutomationElement contents = session.WaitForElement("SettingsDefaultContentsToggle");
      AutomationElement source = session.WaitForElement("SettingsDefaultSourceToggle");
      Assert.Equal(ToggleState.Off, contents.Patterns.Toggle.Pattern.ToggleState.Value);
      Assert.Equal(ToggleState.Off, source.Patterns.Toggle.Pattern.ToggleState.Value);
      contents.Patterns.Toggle.Pattern.Toggle();
      source.Patterns.Toggle.Pattern.Toggle();
      using(var saved = JsonDocument.Parse(session.ReadSettings()))
      {
         Assert.True(saved.RootElement.GetProperty("ShowOutlineByDefault").GetBoolean());
         Assert.True(saved.RootElement.GetProperty("ShowSourceByDefault").GetBoolean());
      }
      CloseSettings(session);
      Window original = session.MainWindow;
      Assert.Null(original.FindFirstDescendant(cf => cf.ByAutomationId("SourceText")));
      original.SetForeground();
      session.WaitForElement("ZoomResetButton").Focus();
      Wait.UntilInputIsProcessed();
      SendReadingKey(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_N);
      session.WaitFor(() => DocumentWindows(session).Length == 2 ? session.MainWindow : null, "a reader opened with the selected panel defaults");
      Window second = DocumentWindows(session).Single(window => !window.Equals(original));
      Assert.NotNull(WaitForWindowElement(session, second, "SourceText"));
      AutomationElement depth = WaitForWindowElement(session, second, "OutlineDepth");
      Assert.False(depth.IsOffscreen);
      Assert.True(depth.BoundingRectangle.Width > 0);
      Assert.Null(original.FindFirstDescendant(cf => cf.ByAutomationId("SourceText")));
      Assert.Null(original.FindFirstDescendant(cf => cf.ByAutomationId("OutlineDepth")));
   }



   /// <summary>Native long titles wrap without truncation, depth markers update sibling windows and unrelated saves retain both choices.</summary>
   [Fact]
   public void Settings_ContentsPresentation_UpdatesLiveSiblingsAndPersists()
   {
      const string Title = "A deliberately long heading that must wrap across several lines in a narrow Contents panel without losing its full title";
      const string Document = "# Contents display fixture\n\n## " + Title + "\nBody";
      using var session = new AppSession(document: Document, documentFileName: "contents.md",
         additionalDocuments: new Dictionary<string, string>(StringComparer.Ordinal) { ["sibling.md"] = Document });
      Window primary = WaitForDocumentWindow(session, "contents.md");
      Window sibling = WaitForDocumentWindow(session, "sibling.md");
      WaitForWindowHeading(session, sibling, "Contents display fixture");
      session.WaitForElement("mv-heading-0");
      InvokeDocumentMenu(session, primary, "OutlineToggle", menuName: "View");
      InvokeDocumentMenu(session, sibling, "OutlineToggle", menuName: "View");
      AutomationElement title = WaitForWindowElement(session, primary, "OutlineTitle:1");
      double oneLineHeight = title.BoundingRectangle.Height;
      Assert.True(oneLineHeight > 0);
      Assert.Equal(Title, WaitForWindowElement(session, primary, "OutlineHeading:1").Name);
      Assert.Null(session.FindInApp(cf => cf.ByAutomationId("OutlineDepthMarker:1")));
      InvokeDocumentMenu(session, primary, "SettingsMenuItem");
      AutomationElement wrap = session.WaitForElement("SettingsContentsWrapToggle");
      AutomationElement markers = session.WaitForElement("SettingsContentsDepthMarkersToggle");
      Assert.Equal(ToggleState.Off, wrap.Patterns.Toggle.Pattern.ToggleState.Value);
      Assert.Equal(ToggleState.Off, markers.Patterns.Toggle.Pattern.ToggleState.Value);
      wrap.Patterns.Toggle.Pattern.Toggle();
      markers.Patterns.Toggle.Pattern.Toggle();
      RetainDocumentWindow(session, session.TopLevelWindows().Single(element => string.Equals(element.Name, session.DisplayName + " — Settings", StringComparison.Ordinal)).AsWindow(), "general-contents-settings");
      CloseSettings(session);
      session.WaitFor(() => primary.FindFirstDescendant(cf => cf.ByAutomationId("OutlineTitle:1")) is { } label && label.BoundingRectangle.Height > oneLineHeight * 1.8 ? label : null, "the fully wrapped Contents title");
      Assert.Equal("Heading level 2", WaitForWindowElement(session, primary, "OutlineDepthMarker:1").Name);
      Assert.Equal("Heading level 2", WaitForWindowElement(session, sibling, "OutlineDepthMarker:1").Name);
      Assert.True(WaitForWindowElement(session, sibling, "OutlineTitle:1").BoundingRectangle.Height > oneLineHeight * 1.8);
      InvokeDocumentMenu(session, sibling, "SourceToggle", menuName: "View");
      AssertSavedContentsPreferences(session, enabled: true);
      InvokeDocumentMenu(session, primary, "SettingsMenuItem");
      Assert.Equal(ToggleState.On, session.WaitForElement("SettingsContentsWrapToggle").Patterns.Toggle.Pattern.ToggleState.Value);
      Assert.Equal(ToggleState.On, session.WaitForElement("SettingsContentsDepthMarkersToggle").Patterns.Toggle.Pattern.ToggleState.Value);
      session.WaitForElement("SettingsContentsWrapToggle").Patterns.Toggle.Pattern.Toggle();
      session.WaitForElement("SettingsContentsDepthMarkersToggle").Patterns.Toggle.Pattern.Toggle();
      CloseSettings(session);
      Assert.Null(session.FindInApp(cf => cf.ByAutomationId("OutlineDepthMarker:1")));
      session.WaitFor(() => primary.FindFirstDescendant(cf => cf.ByAutomationId("OutlineTitle:1")) is { } label && label.BoundingRectangle.Height <= oneLineHeight * 1.2 ? label : null, "the restored single-line Contents title");
      Assert.Equal(Title, WaitForWindowElement(session, primary, "OutlineHeading:1").Name);
      AssertSavedContentsPreferences(session, enabled: false);
      RetainDocumentWindow(session, primary, "contents-truncation-and-markers");
   }



   /// <summary>The appearance draft survives a cancelled secondary theme editor without saving either draft.</summary>
   [Fact]
   public void Settings_ThemeEditorCancel_RetainsCssDraftUntilWindowClose()
   {
      using var session = new AppSession(document: "# Settings cancellation fixture");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Settings cancellation fixture")), "the Settings fixture heading", TimeSpan.FromSeconds(30));
      string saved = session.ReadSettings();
      session.InvokeMenuItem("File", "SettingsMenuItem");
      session.EnterText("ReadingAppearanceCss", "html { background: #112233; }");
      session.WaitForElement("SettingsThemesTab").Patterns.SelectionItem.Pattern.Select();
      session.ClickElement("EditDocumentThemeMenu");
      session.EnterText("ThemeEditorName", "Cancelled Settings theme");
      session.ClickButton("Cancel");
      session.WaitForElement("SettingsWindowRoot");
      session.WaitForElement("SettingsAppearanceTab").Patterns.SelectionItem.Pattern.Select();
      Assert.Equal("html { background: #112233; }", session.WaitForElement("ReadingAppearanceCss").Patterns.Value.Pattern.Value.Value);
      Assert.Equal(saved, session.ReadSettings());
      CloseSettings(session);
      Assert.Equal(saved, session.ReadSettings());
      Assert.NotNull(session.WaitForElement("mv-heading-0"));
      session.InvokeMenuItem("View", "DocumentThemesMenu");
      Assert.Null(session.FindInApp(cf => cf.ByAutomationId("EditDocumentThemeMenu")));
      Assert.Null(session.FindInApp(cf => cf.ByAutomationId("ManageDocumentThemesMenu")));
      session.ClickElement("DocumentTheme:Catppuccin Mocha");
      RetainDocumentWindow(session, session.MainWindow, "settings-draft-cancel");
   }



   /// <summary>A normal native settings window stays resizable/nonmodal and saves routine preferences without applying CSS drafts.</summary>
   [Fact]
   public void Settings_NativeWindowLayoutAndNavigation_SavesRoutineFieldsOnly()
   {
      using var session = new AppSession(document: "# Standard settings fixture");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Standard settings fixture")), "the native Settings fixture", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("File", "SettingsMenuItem");
      Window window = session.TopLevelWindows().Single(element => string.Equals(element.Name, session.DisplayName + " — Settings", StringComparison.Ordinal)).AsWindow();
      Assert.True(session.MainWindow.IsEnabled);
      Assert.True(window.Patterns.Transform.Pattern.CanResize.Value);
      window.Patterns.Transform.Pattern.Resize(780, 600);
      session.WaitForElement("ReadingAppearanceWidth").Patterns.RangeValue.Pattern.SetValue(320);
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("ReadingAppearanceLimit")) is { IsEnabled: true } control ? control : null, "the immediate width save");
      AutomationElement limit = session.WaitForElement("ReadingAppearanceLimit");
      if(limit.Patterns.Toggle.Pattern.ToggleState.Value != ToggleState.On)
      {
         limit.Patterns.Toggle.Pattern.Toggle();
      }
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("ReadingAppearanceCss")) is { IsEnabled: true } control ? control : null, "the immediate width toggle save");
      session.EnterText("ReadingAppearanceCss", "html { background: #334455; }");
      session.WaitForElement("SettingsThemesTab").Patterns.SelectionItem.Pattern.Select();
      Assert.NotNull(session.WaitForElement("EditDocumentThemeMenu"));
      session.WaitForElement("SettingsEditorsTab").Patterns.SelectionItem.Pattern.Select();
      Assert.NotNull(session.WaitForElement("ConfigureEditorMenuItem"));
      session.WaitForElement("SettingsAppearanceTab").Patterns.SelectionItem.Pattern.Select();
      Assert.Equal("html { background: #334455; }", session.WaitForElement("ReadingAppearanceCss").Patterns.Value.Pattern.Value.Value);
      CloseSettings(session);
      using var saved = JsonDocument.Parse(session.ReadSettings());
      Assert.Equal(320, saved.RootElement.GetProperty("ContentWidthPx").GetDouble());
      Assert.True(saved.RootElement.GetProperty("ContentWidthEnabled").GetBoolean());
      Assert.Equal("", saved.RootElement.GetProperty("GlobalCss").GetString());
      Assert.NotNull(session.WaitForElement("mv-heading-0"));
   }



   /// <summary>Advanced startup controls persist the actual choices and survive a sibling reader's unrelated save.</summary>
   [Fact]
   public void Settings_AdvancedStartup_PersistsAcrossSettingsAndSiblingSaves()
   {
      using var session = new AppSession(document: "# Startup settings fixture", documentFileName: "startup.md",
         additionalDocuments: new Dictionary<string, string>(StringComparer.Ordinal) { ["sibling.md"] = "# Startup sibling fixture" });
      Window sibling = WaitForDocumentWindow(session, "sibling.md");
      WaitForWindowHeading(session, sibling, "Startup sibling fixture");
      Assert.Null(session.FindInApp(cf => cf.ByAutomationId("AppKitSplash")));
      session.InvokeMenuItem("File", "SettingsMenuItem");
      session.WaitForElement("SettingsAdvancedTab").Patterns.SelectionItem.Pattern.Select();
      AutomationElement splash = session.WaitForElement("SettingsSplashToggle");
      AutomationElement duration = session.WaitForElement("SettingsSplashDurationBox");
      Assert.Equal(ToggleState.Off, splash.Patterns.Toggle.Pattern.ToggleState.Value);
      Assert.False(duration.IsEnabled);
      splash.Patterns.Toggle.Pattern.Toggle();
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("SettingsSplashDurationBox")) is { IsEnabled: true } control ? control : null, "the enabled splash duration");
      duration.Patterns.RangeValue.Pattern.SetValue(1250);
      session.WaitForElement("SettingsStartupUpdatesToggle").Patterns.Toggle.Pattern.Toggle();
      CloseSettings(session);
      InvokeDocumentMenu(session, sibling, "OutlineToggle", menuName: "View");
      using(var saved = JsonDocument.Parse(session.ReadSettings()))
      {
         Assert.True(saved.RootElement.GetProperty("ShowSplashScreen").GetBoolean());
         Assert.Equal(1250, saved.RootElement.GetProperty("SplashMilliseconds").GetInt32());
         Assert.True(saved.RootElement.GetProperty("CheckForUpdatesOnStartup").GetBoolean());
         Assert.True(saved.RootElement.GetProperty("ShowOutline").GetBoolean());
      }
      session.InvokeMenuItem("File", "SettingsMenuItem");
      session.WaitForElement("SettingsAdvancedTab").Patterns.SelectionItem.Pattern.Select();
      Assert.Equal(1250, session.WaitForElement("SettingsSplashDurationBox").Patterns.RangeValue.Pattern.Value.Value);
      session.WaitForElement("SettingsSplashToggle").Patterns.Toggle.Pattern.Toggle();
      Assert.False(session.WaitForElement("SettingsSplashDurationBox").IsEnabled);
      CloseSettings(session);
      using var disabled = JsonDocument.Parse(session.ReadSettings());
      Assert.False(disabled.RootElement.GetProperty("ShowSplashScreen").GetBoolean());
      Assert.Equal(1250, disabled.RootElement.GetProperty("SplashMilliseconds").GetInt32());
      RetainDocumentWindow(session, session.MainWindow, "advanced-startup-settings");
   }



   /// <summary>The native page advertises user-selected defaults and enables its OS action only for a registered installed executable.</summary>
   [Fact]
   public void Settings_DefaultFileTypes_RespectsInstalledRegistration()
   {
      using var session = new AppSession(document: "# Default file settings fixture");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Default file settings fixture")), "the default file settings fixture", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("File", "SettingsMenuItem");
      session.WaitForElement("SettingsAppearanceTab").Patterns.SelectionItem.Pattern.Select();
      AutomationElement guidance = session.WaitForElement("SettingsDefaultFilesGuidance");
      Assert.Contains(".md", guidance.Name, StringComparison.Ordinal);
      Assert.Contains(".txt) is optional", guidance.Name, StringComparison.Ordinal);
      AutomationElement choose = session.WaitForElement("SettingsDefaultFilesButton");
      var stable = new FileTypeRegistration(Microsoft.Win32.Registry.CurrentUser, session.ExePath, "stable");
      var dev = new FileTypeRegistration(Microsoft.Win32.Registry.CurrentUser, session.ExePath, "dev");
      bool registered = stable.GetDefaultAppsUri(supportsAppPage: true) is not null || dev.GetDefaultAppsUri(supportsAppPage: true) is not null;
      Assert.Equal(registered, choose.IsEnabled);
      string? installedFixture = Environment.GetEnvironmentVariable("K22_UITEST_INSTALLED");
      if(string.Equals(installedFixture, "1", StringComparison.Ordinal))
      {
         FileTypeRegistration installedRegistration = string.Equals(Environment.GetEnvironmentVariable("K22_UITEST_CHANNEL"), "dev", StringComparison.Ordinal) ? dev : stable;
         Assert.NotNull(installedRegistration.GetDefaultAppsUri(supportsAppPage: true));
         Assert.True(choose.IsEnabled);
      }
      else if(string.Equals(installedFixture, "0", StringComparison.Ordinal))
      {
         Assert.False(registered);
      }
      if(!registered)
      {
         Assert.NotNull(session.FindInApp(cf => cf.ByName("Install or repair MarkdownViewr to make it available in Open with and Default Apps. This preview does not change Windows file associations.")));
      }
      CloseSettings(session);
      Assert.NotNull(session.WaitForElement("mv-heading-0"));
      RetainDocumentWindow(session, session.MainWindow, "default-file-settings");
   }



   /// <summary>Reads the actual saved presentation choices and proves they did not opt new windows into either pane.</summary>
   private static void AssertSavedContentsPreferences(AppSession session, bool enabled)
   {
      using var saved = JsonDocument.Parse(session.ReadSettings());
      Assert.Equal(enabled, saved.RootElement.GetProperty("WrapOutlineEntries").GetBoolean());
      Assert.Equal(enabled, saved.RootElement.GetProperty("ShowOutlineDepthMarkers").GetBoolean());
      Assert.False(saved.RootElement.GetProperty("ShowOutlineByDefault").GetBoolean());
      Assert.False(saved.RootElement.GetProperty("ShowSourceByDefault").GetBoolean());
   }



   /// <summary>All configuration tests enter through the actual File Settings command and native category items.</summary>
   private static void OpenSettingsAction(AppSession session, string identity, string tab = "SettingsThemesTab")
   {
      session.InvokeMenuItem("File", "SettingsMenuItem");
      session.WaitForElement(tab).Patterns.SelectionItem.Pattern.Select();
      session.ClickElement(identity);
   }



   /// <summary>Separately saved theme/editor actions return to the same parent draft; closing discards unsubmitted CSS.</summary>
   private static void CloseSettings(AppSession session)
   {
      session.WaitForElement("SettingsWindowRoot", TimeSpan.FromSeconds(30));
      session.TopLevelWindows().Single(window => string.Equals(window.Name, session.DisplayName + " — Settings", StringComparison.Ordinal)).AsWindow().Close();
      session.WaitForElementGone("SettingsWindowRoot");
   }
}
