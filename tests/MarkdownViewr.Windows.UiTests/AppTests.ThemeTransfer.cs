/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Real native JSON pickers, explicit conflict/version choices and exported bytes prove theme transfer independently of settings alone.</summary>
public partial class AppTests
{
   /// <summary>Every actual conflict button has distinct persisted and painted outcomes; Skip leaves committed settings unchanged.</summary>
   [Theory]
   [InlineData("Overwrite", "Nord", 1)]
   [InlineData("Keep both", "Nord (2)", 1)]
   [InlineData("Skip", "", 0)]
   public void ThemeTransfer_NativeConflictChoice_PreservesExpectedPalette(string choice, string importedName, int count)
   {
      byte[] incoming = ThemeFixture("Nord", "#112233");
      using var session = new AppSession(document: "# JSON conflict fixture", documentAssets: new Dictionary<string, byte[]>(StringComparer.Ordinal) { ["incoming.json"] = incoming });
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("JSON conflict fixture")), "the actual conflict document", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "DocumentThemesMenu");
      session.ClickElement("DocumentTheme:Nord");
      WaitForDocumentColor(session, Color.FromArgb(46, 52, 64));
      string before = session.ReadSettings();
      ChooseThemeImports(session, "incoming.json");
      session.ClickButton(choice);
      WaitForThemeImportResult(session, count, 1 - count, 0);
      using var settings = JsonDocument.Parse(session.ReadSettings());
      Assert.Equal(count, settings.RootElement.GetProperty("UserThemes").GetArrayLength());
      if(count == 0)
      {
         Assert.Equal(before, session.ReadSettings());
         WaitForDocumentColor(session, Color.FromArgb(46, 52, 64));
      }
      else
      {
         string json = Assert.Single(settings.RootElement.GetProperty("UserThemes").EnumerateArray()).GetString()!;
         using var imported = JsonDocument.Parse(json);
         Assert.Equal(importedName, imported.RootElement.GetProperty("name").GetString());
         if(string.Equals(choice, "Keep both", StringComparison.Ordinal))
         {
            WaitForDocumentColor(session, Color.FromArgb(46, 52, 64));
            session.InvokeMenuItem("View", "DocumentThemesMenu");
            session.ClickElement("DocumentTheme:" + importedName);
         }
         WaitForDocumentColor(session, Color.FromArgb(17, 34, 51));
      }
      Assert.Equal(incoming, File.ReadAllBytes(Path.Combine(session.DataDirectory, "incoming.json")));
      RetainDocumentWindow(session, session.MainWindow, "theme-conflict-" + choice.Replace(' ', '-'));
   }



   /// <summary>Newer-schema skip/approval and a mixed multiple-file batch retain metadata; exporting a disabled theme writes the exact chosen JSON.</summary>
   [Fact]
   public void ThemeTransfer_BatchFutureInvalidAndDisabledExport_PreservesActualJson()
   {
      byte[] future = ThemeFixture("Owned future", "#334455", version: 2);
      var assets = new Dictionary<string, byte[]>(StringComparer.Ordinal)
      {
         ["future.json"] = future,
         ["other.json"] = ThemeFixture("Owned other", "#556677"),
         ["invalid.json"] = Encoding.UTF8.GetBytes("{invalid"),
         ["oversized.json"] = Encoding.UTF8.GetBytes(new string(' ', ReaderTheme.MaximumCharacters + 1024)),
      };
      using var session = new AppSession(document: "# JSON batch fixture", documentAssets: assets);
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("JSON batch fixture")), "the actual batch document", TimeSpan.FromSeconds(30));
      string original = session.ReadSettings();
      ChooseThemeImports(session, "future.json");
      session.ClickButton("Skip");
      WaitForThemeImportResult(session, 0, 1, 0);
      Assert.Equal(original, session.ReadSettings());
      ChooseThemeImports(session, "future.json", "other.json", "invalid.json", "oversized.json");
      session.ClickButton("Import anyway");
      WaitForThemeImportResult(session, 2, 0, 2);
      session.InvokeMenuItem("View", "DocumentThemesMenu");
      session.ClickElement("DocumentTheme:Owned future");
      WaitForDocumentColor(session, Color.FromArgb(51, 68, 85));
      OpenSettingsAction(session, "ManageDocumentThemesMenu");
      SelectThemeDraft(session, "Owned future");
      session.WaitForElement("ThemeListEnabled").Patterns.Toggle.Pattern.Toggle();
      session.ClickButton("Save themes");
      session.WaitForElementGone("ThemeList");
      CloseSettings(session);
      string beforeExport = session.ReadSettings();
      string destination = Path.Combine(session.DataDirectory, "export & café $HOME.json");
      ExportSelectedTheme(session, "Owned future", destination);
      Assert.Equal(beforeExport, session.ReadSettings());
      Assert.Equal(future, File.ReadAllBytes(destination));
      using(var exported = JsonDocument.Parse(File.ReadAllText(destination)))
      {
         Assert.Equal(2, exported.RootElement.GetProperty("schemaVersion").GetInt32());
         Assert.Equal("Owned café metadata", exported.RootElement.GetProperty("metadata").GetProperty("note").GetString());
      }
      foreach((string name, byte[] bytes) in assets)
      {
         Assert.Equal(bytes, File.ReadAllBytes(Path.Combine(session.DataDirectory, name)));
      }
      Assert.DoesNotContain("Owned café metadata", session.ReadLogs(), StringComparison.Ordinal);
      Assert.DoesNotContain("future.json", session.ReadLogs(), StringComparison.Ordinal);
      Assert.NotNull(session.FindInApp(cf => cf.ByName("JSON batch fixture")));
      RetainDocumentWindow(session, session.MainWindow, "theme-batch-future-disabled-export");
   }



   /// <summary>Manager new/copy/edit/default remain within one copied draft across dialogs; duplicate renaming stays open and Cancel discards every change.</summary>
   [Fact]
   public void ThemeManager_NewCopyDisabledEditAndCancel_RetainsCopiedDraft()
   {
      using var session = new AppSession(document: "# JSON manager fixture");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("JSON manager fixture")), "the actual manager document", TimeSpan.FromSeconds(30));
      string original = session.ReadSettings();
      OpenSettingsAction(session, "ManageDocumentThemesMenu");
      SelectThemeDraft(session, "Nord");
      session.WaitForElement("ThemeListEnabled").Patterns.Toggle.Pattern.Toggle();
      session.ClickElement("ThemeListCopy");
      Assert.Equal("Nord Copy", session.WaitForElement("ThemeEditorName").Patterns.Value.Pattern.Value.Value);
      session.EnterText("ThemeEditorColor:background", "#112233");
      session.ClickButton("Save theme");
      session.WaitForElement("ThemeChoice:Nord Copy");
      Assert.Equal(original, session.ReadSettings());
      session.ClickElement("ThemeListNew");
      session.EnterText("ThemeEditorName", "Owned new draft");
      session.ClickButton("Save theme");
      session.WaitForElement("ThemeChoice:Owned new draft");
      Assert.Equal(original, session.ReadSettings());
      RejectDuplicateThemeRename(session);
      SelectThemeDraft(session, "Nord");
      session.WaitForElement("ThemeListDefault").Patterns.SelectionItem.Pattern.Select();
      Assert.Equal(ToggleState.On, session.WaitForElement("ThemeListEnabled").Patterns.Toggle.Pattern.ToggleState.Value);
      session.ClickButton("Cancel");
      session.WaitForElementGone("ThemeList");
      CloseSettings(session);
      Assert.Equal(original, session.ReadSettings());
      WaitForDocumentColor(session, Color.FromArgb(30, 30, 46));
      OpenSettingsAction(session, "ManageDocumentThemesMenu");
      SelectThemeDraft(session, "Nord");
      session.ClickElement("ThemeListCopy");
      session.EnterText("ThemeEditorColor:background", "#445566");
      session.ClickButton("Save theme");
      session.WaitForElement("ThemeChoice:Nord Copy");
      session.ClickButton("Save themes");
      session.WaitForElementGone("ThemeList");
      CloseSettings(session);
      WaitForDocumentColor(session, Color.FromArgb(68, 85, 102));
      WaitForSavedDocumentTheme(session, "Nord Copy");
      RetainDocumentWindow(session, session.MainWindow, "theme-manager-copied-drafts");
   }



   /// <summary>Actual import/export selector and destination cancels preserve source/glyphs/settings and create no theme file.</summary>
   [Fact]
   public void ThemeTransfer_NativePickerCancels_PreservesReaderAndFiles()
   {
      using var session = new AppSession(document: "# Theme transfer cancellation");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Theme transfer cancellation")), "the actual cancellation document", TimeSpan.FromSeconds(30));
      InvokeDocumentMenu(session, session.MainWindow, "SourceToggle", menuName: "View");
      string source = session.WaitForElement("SourceText").Patterns.Value.Pattern.Value.Value;
      string settings = session.ReadSettings();
      Rectangle glyph = session.WaitForElement("mv-heading-0").BoundingRectangle;
      string[] files = Directory.GetFiles(session.DataDirectory, "*.json").Order(StringComparer.Ordinal).ToArray();
      OpenSettingsAction(session, "ImportDocumentThemesMenu");
      _ = WaitForOpenPickerFilename(session);
      session.ClickButton("Cancel");
      session.WaitForElementGone("1001");
      CloseSettings(session);
      OpenSettingsAction(session, "ExportDocumentThemesMenu");
      session.WaitForElement("ThemeExportChoice");
      session.ClickButton("Cancel");
      session.WaitForElementGone("ThemeExportChoice");
      CloseSettings(session);
      OpenSettingsAction(session, "ExportDocumentThemesMenu");
      session.WaitForElement("ThemeExportChoice");
      session.ClickButton("Export theme");
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("1001").And(cf.ByControlType(ControlType.Edit))), "the actual theme destination before cancellation");
      session.ClickButton("Cancel");
      session.WaitForElementGone("1001");
      CloseSettings(session);
      Assert.Equal(settings, session.ReadSettings());
      Assert.Equal(source, session.WaitForElement("SourceText").Patterns.Value.Pattern.Value.Value);
      Assert.Equal(glyph, session.WaitForElement("mv-heading-0").BoundingRectangle);
      Assert.Equal(files, Directory.GetFiles(session.DataDirectory, "*.json").Order(StringComparer.Ordinal), StringComparer.Ordinal);
      Assert.Equal("# Theme transfer cancellation", File.ReadAllText(Path.Combine(session.DataDirectory, "document.md")));
      RetainDocumentWindow(session, session.MainWindow, "theme-transfer-native-cancels");
   }




   /// <summary>The real native editor rejects a renamed collision and returns to the untouched manager draft on Cancel.</summary>
   private static void RejectDuplicateThemeRename(AppSession session)
   {
      SelectThemeDraft(session, "Nord");
      session.ClickElement("ThemeListEdit");
      session.EnterText("ThemeEditorName", "Dracula");
      session.ClickButton("Save theme");
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("ThemeEditorError")) is { } error && error.Name.Contains("already exists", StringComparison.Ordinal) ? error : null, "the actual duplicate-name validation error");
      Assert.NotNull(session.FindInApp(cf => cf.ByAutomationId("ThemeEditorName")));
      session.ClickButton("Cancel");
      session.WaitForElement("ThemeList");
   }




   /// <summary>Fixture JSON is real versioned theme data with Unicode metadata, never application-reported paint values.</summary>
   private static byte[] ThemeFixture(string name, string color, int version = 1)
   {
      JsonNode json = JsonNode.Parse(ThemeCatalog.Find("Nord").Json)!;
      json["name"] = name;
      json["schemaVersion"] = version;
      json["colors"]!["background"] = color;
      json["metadata"] = new JsonObject { ["note"] = "Owned café metadata" };
      return Encoding.UTF8.GetBytes(json.ToJsonString());
   }



   /// <summary>Navigates the owned native picker before selecting literal names, retaining all batch files within its filename capacity.</summary>
   private static void ChooseThemeImports(AppSession session, params string[] files)
   {
      OpenSettingsAction(session, "ImportDocumentThemesMenu");
      AutomationElement filename = WaitForOpenPickerFilename(session);
      if(files.Length > 1)
      {
         string directory = session.DataDirectory + Path.DirectorySeparatorChar;
         EnterPickerFilename(session, filename, directory);
         SendReadingKey(VirtualKeyShort.RETURN);
         session.WaitFor(() => !string.Equals(WaitForOpenPickerFilename(session).AsTextBox().Text, directory, StringComparison.Ordinal) ? filename : null, "the native picker completing navigation into the fixture folder");
         filename = WaitForOpenPickerFilename(session);
      }
      string paths = string.Join(" ", files.Select(file => "\"" + (files.Length > 1 ? file : Path.Combine(session.DataDirectory, file)) + "\""));
      EnterPickerFilename(session, filename, paths);
      session.ClickButton("Open");
   }



   /// <summary>Waits for native completion counts after all actual parser/choice outcomes, not a fixed delay.</summary>
   private static void WaitForThemeImportResult(AppSession session, int imported, int skipped, int failed)
   {
      CloseSettings(session);
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("StatusText").And(cf.ByName(FormattableString.Invariant($"Themes imported: {imported}. Skipped: {skipped}. Failed: {failed}.")))), "completed native theme batch results", TimeSpan.FromSeconds(30));
   }



   /// <summary>Export selection includes disabled choices; the exact native destination must produce a real JSON file and native success.</summary>
   private static void ExportSelectedTheme(AppSession session, string name, string destination)
   {
      OpenSettingsAction(session, "ExportDocumentThemesMenu");
      session.WaitForElement("ThemeExportChoice").Patterns.ExpandCollapse.Pattern.Expand();
      session.WaitForElement("ThemeExport:" + name).Patterns.SelectionItem.Pattern.Select();
      session.ClickButton("Export theme");
      AutomationElement filename = session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("1001").And(cf.ByControlType(ControlType.Edit))), "the exact owned Windows theme export destination");
      EnterPickerFilename(session, filename, destination);
      session.ClickButton("Save");
      CloseSettings(session);
      session.WaitFor(() => File.Exists(destination) ? session.FindInApp(cf => cf.ByAutomationId("StatusText").And(cf.ByName("Theme exported."))) : null, "actual completed theme JSON export", TimeSpan.FromSeconds(30));
   }
}
