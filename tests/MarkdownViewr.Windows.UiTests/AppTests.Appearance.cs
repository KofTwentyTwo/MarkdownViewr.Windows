/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Drawing;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using MarkdownViewr.Windows.Core.Printing;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Accepts actual reading width/CSS paint and proves that generated print output remains independent.</summary>
public partial class AppTests
{
   /// <summary>Native reading preferences wrap actual glyphs across themes while source/find, print output and cancel remain intact.</summary>
   [Fact]
   public async Task Appearance_WidthCssAndPrint_PreservesSourceFindAndCancel()
   {
      const string Title = "APPEARANCE WIDTH PROOF WITH MANY LONG WORDS";
      using var session = new AppSession(document: "# " + Title + "\n\nOne paired appearance needle.");
      AutomationElement initial = session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-0")), "the actual appearance fixture heading", TimeSpan.FromSeconds(30));
      Assert.True(initial.BoundingRectangle.Width > 350);
      double height = initial.BoundingRectangle.Height;
      EditReadingAppearance(session, 320, "#content { background: #112233; }");
      WaitForAppearanceWindowColor(session, session.MainWindow, Color.FromArgb(17, 34, 51));
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-0")) is { } heading && heading.BoundingRectangle.Width <= 321 && heading.BoundingRectangle.Height > height ? heading : null, "the actual heading wrapped within the saved320px reading width");
      session.InvokeMenuItem("View", "SourceToggle");
      string source = session.WaitForElement("SourceText").Patterns.Value.Pattern.Value.Value;
      Assert.Contains(Title, source, StringComparison.Ordinal);
      session.InvokeMenuItem("Edit", "FindMenuItem");
      session.EnterText("FindText", "paired appearance needle");
      WaitForFindCount(session, "1 of 1");
      session.InvokeMenuItem("View", "DocumentThemesMenu");
      session.ClickElement("DocumentTheme:GitHub Light");
      WaitForAppearanceWindowColor(session, session.MainWindow, Color.FromArgb(17, 34, 51));
      using(Bitmap pdf = await ExportPrintFixtureAsync(session, "reader-appearance", new PrintOptions("Active Theme", Backgrounds: true, FullPage: false)))
      {
         Assert.Equal(Color.White.ToArgb(), pdf.GetPixel(pdf.Width / 2, pdf.Height / 2).ToArgb());
         Assert.True(FirstPrintedInkBand(pdf, pixel => pixel.R < 100 && pixel.G < 100).Width > pdf.Width * 0.55, "Actual printed heading width must exceed the narrow screen column, using the chosen theme's blue ink.");
      }
      WaitForAppearanceWindowColor(session, session.MainWindow, Color.FromArgb(17, 34, 51));
      Assert.Equal(source, session.WaitForElement("SourceText").Patterns.Value.Pattern.Value.Value);
      WaitForFindCount(session, "1 of 1");
      string accepted = session.ReadSettings();
      Rectangle before = session.WaitForElement("mv-heading-0").BoundingRectangle;
      EditReadingAppearance(session, 320, "#content { background: #445566; }", save: false);
      Assert.Equal(accepted, session.ReadSettings());
      Assert.Equal(before, session.WaitForElement("mv-heading-0").BoundingRectangle);
      EditReadingAppearance(session, 320, "#content { background: #112233; }", limit: false);
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-0")) is { } heading && heading.BoundingRectangle.Width > 321 ? heading : null, "the actual heading expanding after the native width limit is disabled");
      using(var settings = JsonDocument.Parse(session.ReadSettings()))
      {
         Assert.False(settings.RootElement.GetProperty("ContentWidthEnabled").GetBoolean());
         Assert.Equal(320, settings.RootElement.GetProperty("ContentWidthPx").GetDouble());
         Assert.Equal("#content { background: #112233; }", settings.RootElement.GetProperty("GlobalCss").GetString());
      }
      RetainDocumentWindow(session, session.MainWindow, "reader-appearance");
   }



   /// <summary>Latest global appearance repaints an already-open sibling; its later local preference save cannot restore old CSS.</summary>
   [Fact]
   public void Appearance_GlobalBroadcast_PreservesSiblingReadersAndPreferences()
   {
      using var session = new AppSession(document: "# Global appearance first");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Global appearance first")), "the actual first reader", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("File", "NewWindowMenuItem");
      Window sibling = session.WaitFor(() => DocumentWindows(session).FirstOrDefault(window => string.Equals(window.Name, session.DisplayName, StringComparison.Ordinal)), "an already-open blank sibling reader").AsWindow();
      EditReadingAppearance(session, 320, "#content { background: #112233; }");
      EditReadingAppearance(session, 320, "#content { background: #445566; }");
      WaitForAppearanceWindowColor(session, session.MainWindow, Color.FromArgb(68, 85, 102));
      WaitForAppearanceWindowColor(session, sibling, Color.FromArgb(68, 85, 102));
      InvokeDocumentMenu(session, sibling, "SourceToggle", menuName: "View");
      session.WaitFor(() => sibling.FindFirstDescendant(cf => cf.ByAutomationId("SourceText")), "the sibling saving its window-local source preference");
      using(var settings = JsonDocument.Parse(session.ReadSettings()))
      {
         Assert.Equal("#content { background: #445566; }", settings.RootElement.GetProperty("GlobalCss").GetString());
         Assert.Equal(320, settings.RootElement.GetProperty("ContentWidthPx").GetDouble());
      }
      WaitForAppearanceWindowColor(session, sibling, Color.FromArgb(68, 85, 102));
      Assert.Equal(2, DocumentWindows(session).Length);
      Assert.NotNull(session.MainWindow.FindFirstDescendant(cf => cf.ByName("Global appearance first")));
      Assert.Equal("# Global appearance first", File.ReadAllText(Path.Combine(session.DataDirectory, "document.md")));
      RetainDocumentWindow(session, sibling, "shared-global-appearance");
   }



   /// <summary>Changes actual native draft controls and explicitly saves/cancels, with no mirrored browser test properties.</summary>
   private static void EditReadingAppearance(AppSession session, double width, string css, bool limit = true, bool save = true)
   {
      session.InvokeMenuItem("File", "SettingsMenuItem");
      session.WaitForElement("ReadingAppearanceWidth").Patterns.RangeValue.Pattern.SetValue(width);
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("ReadingAppearanceCss")) is { IsEnabled: true } field ? field : null, "the native layout save to finish");
      session.EnterText("ReadingAppearanceCss", css);
      AutomationElement checkbox = session.WaitForElement("ReadingAppearanceLimit");
      bool enabled = checkbox.Patterns.Toggle.Pattern.ToggleState.Value == ToggleState.On;
      if(enabled != limit)
      {
         checkbox.Patterns.Toggle.Pattern.Toggle();
      }
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("ReadingAppearanceCss")) is { IsEnabled: true } field ? field : null, "the native toggle save to finish");
      if(save)
      {
         session.ClickElement("SettingsApplyCss");
         session.WaitFor(() =>
         {
            using var settings = JsonDocument.Parse(session.ReadSettings());
            return string.Equals(settings.RootElement.GetProperty("GlobalCss").GetString(), css, StringComparison.Ordinal) ? session.MainWindow : null;
         }, "the actual persisted CSS apply");
      }
      CloseSettings(session);
      session.WaitForElementGone("ReadingAppearanceCss");
   }



   /// <summary>Samples only the explicitly foreground owned WebView surface, excluding native source panes and other readers.</summary>
   private static void WaitForAppearanceWindowColor(AppSession session, Window window, Color expected)
   {
      window.SetForeground();
      Wait.UntilInputIsProcessed();
      session.WaitFor(() =>
      {
         if(GetForegroundWindow() != window.Properties.NativeWindowHandle.Value)
         {
            return null;
         }
         AutomationElement? view = window.FindFirstDescendant(cf => cf.ByAutomationId("DocumentView"));
         if(view is null)
         {
            return null;
         }
         using Bitmap image = view.Capture();
         return image.GetPixel(image.Width / 2, image.Height / 2).ToArgb() == expected.ToArgb() ? view : null;
      }, "actual owned reader background " + expected);
   }
}
