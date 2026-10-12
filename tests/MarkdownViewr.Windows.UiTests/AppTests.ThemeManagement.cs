/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Drawing;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.WindowsAPI;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Drives actual theme management, menu geometry, physical cycling keys and shared reader paint.</summary>
public partial class AppTests
{
   /// <summary>Native saved order/disable state changes the real submenu and physical cycling; cancelled deletion and text focus remain untouched.</summary>
   [Fact]
   public void Themes_OrderDisableCycleAndCancel_UsesActualNativeChoices()
   {
      using var session = new AppSession(document: "# Native theme management");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Native theme management")), "the actual theme fixture heading", TimeSpan.FromSeconds(30));
      OpenSettingsAction(session, "ManageDocumentThemesMenu");
      SelectThemeDraft(session, "Catppuccin Mocha");
      session.ClickElement("ThemeListUp");
      SelectThemeDraft(session, "Catppuccin Latte");
      session.WaitForElement("ThemeListEnabled").Patterns.Toggle.Pattern.Toggle();
      session.ClickButton("Save themes");
      session.WaitForElementGone("ThemeList");
      CloseSettings(session);
      session.InvokeMenuItem("View", "DocumentThemesMenu");
      session.WaitForElement("DocumentTheme:Catppuccin Mocha");
      Assert.Null(session.FindInApp(cf => cf.ByAutomationId("DocumentTheme:Catppuccin Latte")));
      AutomationElement[] menu = session.TopLevelWindows().SelectMany(window => window.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem)))
         .Where(element => element.Properties.AutomationId.ValueOrDefault?.StartsWith("DocumentTheme:", StringComparison.Ordinal) == true && !element.IsOffscreen)
         .OrderBy(element => element.BoundingRectangle.Top).ToArray();
      Assert.Equal("DocumentTheme:Catppuccin Mocha", menu[0].Properties.AutomationId.Value);
      Assert.Equal("DocumentTheme:Dracula", menu[1].Properties.AutomationId.Value);
      session.ClickElement("DocumentTheme:Catppuccin Mocha");
      FocusBrowser(session);
      SendReadingKey(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.DOWN);
      WaitForDocumentColor(session, Color.FromArgb(40, 42, 54));
      FocusBrowser(session);
      SendReadingKey(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.UP);
      WaitForDocumentColor(session, Color.FromArgb(30, 30, 46));
      string accepted = session.ReadSettings();
      OpenSettingsAction(session, "ManageDocumentThemesMenu");
      SelectThemeDraft(session, "Catppuccin Mocha");
      session.ClickElement("ThemeListDelete");
      session.ClickButton("Cancel");
      session.WaitForElementGone("ThemeList");
      CloseSettings(session);
      Assert.Equal(accepted, session.ReadSettings());
      WaitForDocumentColor(session, Color.FromArgb(30, 30, 46));
      session.InvokeMenuItem("Edit", "FindMenuItem");
      session.EnterText("FindText", "Native theme management");
      WaitForFindCount(session, "1 of 1");
      session.WaitForElement("FindText").Focus();
      SendReadingKey(VirtualKeyShort.CONTROL, VirtualKeyShort.SHIFT, VirtualKeyShort.DOWN);
      Assert.Equal(accepted, session.ReadSettings());
      Assert.Equal("Native theme management", session.WaitForElement("FindText").Patterns.Value.Pattern.Value.Value);
      RetainDocumentWindow(session, session.MainWindow, "theme-order-disable-cycle-cancel");
   }



   /// <summary>Actual native palette pixels and explicit default/enabled state stay in the copied manager draft until Save.</summary>
   [Fact]
   public void Themes_PalettePreviewAndDefaultChoice_PreservesDraftAndPaintsSavedSelection()
   {
      using var session = new AppSession(document: "# Native palette preview fixture");
      session.WaitForElement("mv-heading-0", TimeSpan.FromSeconds(30));
      string saved = session.ReadSettings();
      OpenSettingsAction(session, "ManageDocumentThemesMenu");
      SelectThemeDraft(session, "Nord");
      AutomationElement choice = session.WaitForElement("ThemeChoice:Nord");
      choice.Patterns.ScrollItem.Pattern.ScrollIntoView();
      Assert.Equal("Built-in · Enabled", session.WaitForElement("ThemeState:Nord").Name);
      Assert.False(session.WaitForElement("ThemeListDefault").Patterns.SelectionItem.Pattern.IsSelected.Value);
      WaitForThemePaletteColor(session, "Nord", "background", Color.FromArgb(46, 52, 64));
      Assert.Contains("color #", session.WaitForElement("ThemePalette:Nord:text").Name, StringComparison.Ordinal);
      Assert.Contains("color #", session.WaitForElement("ThemePalette:Nord:heading1").Name, StringComparison.Ordinal);
      Assert.Contains("color #", session.WaitForElement("ThemePalette:Nord:heading2").Name, StringComparison.Ordinal);
      Assert.Contains("color #", session.WaitForElement("ThemePalette:Nord:link").Name, StringComparison.Ordinal);
      session.WaitForElement("ThemeListEnabled").Patterns.Toggle.Pattern.Toggle();
      Assert.Equal("Built-in · Disabled", session.WaitForElement("ThemeState:Nord").Name);
      session.WaitForElement("ThemeListDefault").Patterns.SelectionItem.Pattern.Select();
      Assert.Equal(ToggleState.On, session.WaitForElement("ThemeListEnabled").Patterns.Toggle.Pattern.ToggleState.Value);
      Assert.True(session.WaitForElement("ThemeListDefault").Patterns.SelectionItem.Pattern.IsSelected.Value);
      Assert.Equal("Built-in · Enabled · Default", session.WaitForElement("ThemeState:Nord").Name);
      Assert.Equal(saved, session.ReadSettings());
      RetainDocumentWindow(session, session.TopLevelWindows().Single(element => string.Equals(element.Name, session.DisplayName + " — Settings", StringComparison.Ordinal)).AsWindow(), "native-theme-palettes-default-draft");
      session.ClickButton("Save themes");
      session.WaitForElementGone("ThemeList");
      CloseSettings(session);
      WaitForSavedDocumentTheme(session, "Nord");
      WaitForDocumentColor(session, Color.FromArgb(46, 52, 64));
      OpenSettingsAction(session, "ManageDocumentThemesMenu");
      SelectThemeDraft(session, "Nord");
      Assert.True(session.WaitForElement("ThemeListDefault").Patterns.SelectionItem.Pattern.IsSelected.Value);
      Assert.Equal("Built-in · Enabled · Default", session.WaitForElement("ThemeState:Nord").Name);
      session.ClickButton("Cancel");
      CloseSettings(session);
   }



   /// <summary>Measures an actually visible swatch's painted center rather than only its label or serialized palette.</summary>
   private static void WaitForThemePaletteColor(AppSession session, string name, string field, Color expected)
   {
      session.WaitFor(() =>
      {
         AutomationElement swatch = session.WaitForElement("ThemePalette:" + name + ":" + field);
         if(swatch.IsOffscreen)
         {
            return null;
         }
         using Bitmap image = swatch.Capture();
         return image.GetPixel(image.Width / 2, image.Height / 2).ToArgb() == expected.ToArgb() ? swatch : null;
      }, "the actual native theme palette color " + expected);
   }



   /// <summary>Saved deletion removes a real choice and updates sibling paint; later local saves cannot resurrect deleted/shared state.</summary>
   [Fact]
   public void Themes_SharedDeleteRestoreAndSiblingSave_RetainsActualCatalog()
   {
      using var session = new AppSession(document: "# Shared theme management");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Shared theme management")), "the actual first reader", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("File", "NewWindowMenuItem");
      Window sibling = session.WaitFor(() => DocumentWindows(session).FirstOrDefault(window => string.Equals(window.Name, session.DisplayName, StringComparison.Ordinal)), "the blank theme sibling").AsWindow();
      session.InvokeMenuItem("View", "DocumentThemesMenu");
      session.ClickElement("DocumentTheme:GitHub Light");
      WaitForAppearanceWindowColor(session, session.MainWindow, Color.White);
      WaitForAppearanceWindowColor(session, sibling, Color.White);
      OpenSettingsAction(session, "ManageDocumentThemesMenu");
      SelectThemeDraft(session, "GitHub Light");
      session.ClickElement("ThemeListDelete");
      session.ClickButton("Save themes");
      session.WaitForElementGone("ThemeList");
      CloseSettings(session);
      WaitForAppearanceWindowColor(session, session.MainWindow, Color.FromArgb(239, 241, 245));
      WaitForAppearanceWindowColor(session, sibling, Color.FromArgb(239, 241, 245));
      InvokeDocumentMenu(session, sibling, "SourceToggle", menuName: "View");
      session.WaitFor(() => sibling.FindFirstDescendant(cf => cf.ByAutomationId("SourceText")), "the sibling's later local preference save");
      using(var settings = JsonDocument.Parse(session.ReadSettings()))
      {
         Assert.Equal("GitHub Light", Assert.Single(settings.RootElement.GetProperty("DeletedBuiltInThemes").EnumerateArray()).GetString());
         Assert.Equal("Catppuccin Latte", settings.RootElement.GetProperty("DocumentTheme").GetString());
      }
      session.InvokeMenuItem("View", "DocumentThemesMenu");
      session.WaitForElement("DocumentTheme:Catppuccin Latte");
      Assert.Null(session.FindInApp(cf => cf.ByAutomationId("DocumentTheme:GitHub Light")));
      session.ClickElement("DocumentTheme:Catppuccin Latte");
      OpenSettingsAction(session, "ManageDocumentThemesMenu");
      session.ClickElement("ThemeListRestore");
      Assert.NotNull(session.WaitForElement("ThemeChoice:GitHub Light"));
      session.ClickButton("Save themes");
      session.WaitForElementGone("ThemeList");
      CloseSettings(session);
      session.InvokeMenuItem("View", "DocumentThemesMenu");
      session.ClickElement("DocumentTheme:GitHub Light");
      WaitForAppearanceWindowColor(session, session.MainWindow, Color.White);
      WaitForAppearanceWindowColor(session, sibling, Color.White);
      Assert.Equal(2, DocumentWindows(session).Length);
      Assert.Equal("# Shared theme management", File.ReadAllText(Path.Combine(session.DataDirectory, "document.md")));
      RetainDocumentWindow(session, sibling, "theme-shared-delete-restore");
   }



   /// <summary>Waits for an atomic saved selection before checking restoration that deliberately retains the same painted palette.</summary>
   private static void WaitForSavedDocumentTheme(AppSession session, string name)
      => session.WaitFor(() =>
      {
         using var settings = JsonDocument.Parse(session.ReadSettings());
         return string.Equals(settings.RootElement.GetProperty("DocumentTheme").GetString(), name, StringComparison.Ordinal) ? session.MainWindow : null;
      }, "the saved document theme " + name);




   /// <summary>Native selection loads the actual copied manager choice, including disabled entries.</summary>
   private static void SelectThemeDraft(AppSession session, string name)
      => session.WaitForElement("ThemeChoice:" + name).Patterns.SelectionItem.Pattern.Select();
}
