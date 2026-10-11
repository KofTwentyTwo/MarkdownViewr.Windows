/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Drawing;
using System.Drawing.Imaging;
using FlaUI.Core.AutomationElements;
using MarkdownViewr.Windows.Core.Printing;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Measures real printed glyphs and block geometry through the native options and Windows PDF rasterizer.</summary>
public partial class AppTests
{
   /// <summary>Printed font scale and padding change actual glyphs while active paired find, source and reading paint remain intact.</summary>
   [Fact]
   public async Task Print_ZoomAndPadding_ChangesRealOutputAndPreservesFind()
   {
      using var session = new AppSession(document: "# PRINT GEOMETRY\n\n## Find fixture\n\nOne paired needle in literal source.");
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-0")), "the print geometry heading", TimeSpan.FromSeconds(30));
      double height = session.WaitForElement("mv-heading-0").BoundingRectangle.Height;
      for(int step = 0; step < 4; step++)
      {
         double previous = session.WaitForElement("mv-heading-0").BoundingRectangle.Height;
         session.ClickElement("ZoomInButton");
         string percentage = Math.Round(Math.Pow(1.1, step + 1) * 100).ToString(System.Globalization.CultureInfo.InvariantCulture);
         session.WaitFor(() =>
         {
            AutomationElement heading = session.WaitForElement("mv-heading-0");
            AutomationElement reset = session.WaitForElement("ZoomResetButton");
            return heading.BoundingRectangle.Height > previous && reset.IsEnabled && string.Equals(reset.Name, "Actual size (" + percentage + "%)", StringComparison.Ordinal) ? heading : null;
         }, "the real larger heading at the completed requested zoom step");
      }
      // UIA text boxes use rounded font metrics (35px becomes 44px at 121%); exact font scale is measured in the PDF below.
      Assert.InRange(session.WaitForElement("mv-heading-0").BoundingRectangle.Height / height, 1.35, 1.6);
      session.InvokeMenuItem("View", "SourceToggle");
      session.InvokeMenuItem("Edit", "FindMenuItem");
      session.EnterText("FindText", "paired needle");
      WaitForFindCount(session, "1 of 1");
      (Rectangle Glyph, Rectangle View) reading = WaitForStablePrintViewport(session);
      string source = session.WaitForElement("SourceText").Patterns.Value.Pattern.Value.Value;
      var standardOptions = new PrintOptions("Active Theme");
      using Bitmap standard = await ExportPrintFixtureAsync(session, "standard", standardOptions);
      Rectangle original = FirstPrintedInkBand(standard);
      AssertPrintFindState(session, reading, source);
      using Bitmap matched = await ExportPrintFixtureAsync(session, "matched", standardOptions with { MatchScreenZoom = true });
      Rectangle scaled = FirstPrintedInkBand(matched);
      Assert.InRange((double)scaled.Height / original.Height, 1.35, 1.6);
      Assert.InRange((double)scaled.Width / original.Width, 1.35, 1.6);
      AssertPrintFindState(session, reading, source);
      using Bitmap padded = await ExportPrintFixtureAsync(session, "padded", standardOptions with { PaddingPixels = 40 });
      Rectangle inset = FirstPrintedInkBand(padded);
      Assert.InRange(inset.Left - original.Left, 38, 42);
      Assert.InRange(inset.Height - original.Height, -1, 1);
      Assert.True(inset.Top > original.Top + 20);
      AssertPrintFindState(session, reading, source);
   }



   /// <summary>A saved custom width and optional borders alter real printed block edges; full-page mode overrides the narrow width.</summary>
   [Fact]
   public async Task Print_UserWidthAndBorders_ChangesRealBlockEdges()
   {
      using var session = new AppSession(document: "# WIDTH\n\n## Borders\n\n```text\nx\n```");
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-0")), "the print width heading", TimeSpan.FromSeconds(30));
      OpenSettingsAction(session, "EditDocumentThemeMenu");
      session.EnterText("ThemeEditorName", "Print geometry");
      session.EnterText("ThemeEditorCss", "body { max-width: 240px; margin: 0 auto; } pre { border: none; border-radius: 0; }");
      session.ClickButton("Save theme");
      session.WaitForElementGone("ThemeEditorName");
      CloseSettings(session);
      session.InvokeMenuItem("View", "SourceToggle");
      (Rectangle Glyph, Rectangle View) reading = WaitForStablePrintViewport(session);
      string source = session.WaitForElement("SourceText").Patterns.Value.Pattern.Value.Value;
      var options = new PrintOptions("Print geometry", FullPage: false);
      using Bitmap plain = await ExportPrintFixtureAsync(session, "without-borders", options);
      (int Count, int Left) without = LongestPrintedInkRow(plain);
      AssertPrintScreenState(session, reading, source);
      using Bitmap narrow = await ExportPrintFixtureAsync(session, "narrow-border", options with { Borders = true });
      (int Count, int Left) content = LongestPrintedInkRow(narrow);
      Assert.True(content.Count > without.Count + 100, "Optional borders must add a real continuous printed block edge.");
      Assert.InRange(content.Count, 220, 260);
      AssertPrintScreenState(session, reading, source);
      using Bitmap full = await ExportPrintFixtureAsync(session, "full-border", options with { Borders = true, FullPage = true });
      (int Count, int Left) page = LongestPrintedInkRow(full);
      Assert.True(page.Count > content.Count * 2, "Full-page mode must override the custom narrow document width.");
      Assert.True(page.Left < content.Left - 150, "The full-page border must reach the actual page content edge.");
      AssertPrintScreenState(session, reading, source);
   }



   /// <summary>Edits all requested print choices through their actual native controls, including the bounded NumberBox.</summary>
   private static void ConfigurePrintOptions(AppSession session, PrintOptions options, AutomationElement? owner = null)
   {
      FindPrintControl(session, owner, "PrintTheme").AsComboBox().Select(options.Theme);
      FindPrintControl(session, owner, "PrintBackgrounds").AsCheckBox().IsChecked = options.Backgrounds;
      FindPrintControl(session, owner, "PrintImages").AsCheckBox().IsChecked = options.Images;
      FindPrintControl(session, owner, "PrintZoom").AsComboBox().Select(options.MatchScreenZoom ? "Match screen" : "Standard (100%)");
      FindPrintControl(session, owner, "PrintWidth").AsComboBox().Select(options.FullPage ? "Full page" : "Document width");
      FindPrintControl(session, owner, "PrintBorders").AsCheckBox().IsChecked = options.Borders;
      FindPrintControl(session, owner, "PrintPadding").Patterns.RangeValue.Pattern.SetValue(options.PaddingPixels);
   }



   /// <summary>Concurrent Settings and output dialogs retain distinct native control ownership despite sharing option identities.</summary>
   private static AutomationElement FindPrintControl(AppSession session, AutomationElement? owner, string identity)
      => owner is null ? session.WaitForElement(identity) : session.WaitFor(() => owner.FindFirstDescendant(cf => cf.ByAutomationId(identity)), "the owned print control " + identity);



   /// <summary>Retains the real generated PDF before decoding it and its rendered page before any geometric assertion.</summary>
   private async Task<Bitmap> ExportPrintFixtureAsync(AppSession session, string name, PrintOptions options)
   {
      string pdf = Path.Combine(session.DataDirectory, name + ".pdf");
      SavePrintPdf(session, pdf, options);
      string evidence = RetainPrintedFixture(session, pdf);
      Bitmap bitmap = await RenderFirstPdfPageAsync(pdf);
      bitmap.Save(evidence + ".png", ImageFormat.Png);
      return bitmap;
   }



   /// <summary>Checks actual source selection, query/count, reading paint and native/browser geometry after each successful PDF.</summary>
   private static void AssertPrintFindState(AppSession session, (Rectangle Glyph, Rectangle View) reading, string source)
   {
      AssertPrintScreenState(session, reading, source);
      WaitForFindCount(session, "1 of 1");
      Assert.Equal("paired needle", session.WaitForElement("FindText").AsTextBox().Text);
      AutomationElement text = session.WaitForElement("SourceText");
      session.WaitFor(() => string.Equals(ReadSourceSelection(text), "paired needle", StringComparison.Ordinal) ? text : null, "the unchanged real paired source selection after printing");
      WaitForDocumentColor(session, Color.FromArgb(30, 30, 46));
   }



   /// <summary>Measures the first contiguous dark glyph band, excluding light rules and later body text.</summary>
   private static Rectangle FirstPrintedInkBand(Bitmap bitmap, Func<Color, bool>? isInk = null)
   {
      isInk ??= IsPrintedInk;
      int top = -1;
      int bottom = -1;
      int left = bitmap.Width;
      int right = -1;
      for(int y = 0; y < bitmap.Height; y++)
      {
         bool ink = false;
         for(int x = 0; x < bitmap.Width; x++)
         {
            if(isInk(bitmap.GetPixel(x, y)))
            {
               ink = true;
               left = Math.Min(left, x);
               right = Math.Max(right, x);
            }
         }
         if(ink)
         {
            top = top < 0 ? y : top;
            bottom = y;
         }
         else if(top >= 0)
         {
            break;
         }
      }
      Assert.True(top >= 0 && right >= left, "The actual PDF must contain a printed heading glyph band.");
      return Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
   }



   /// <summary>Finds the real longest dark row and its left edge to distinguish optional block borders and page width.</summary>
   private static (int Count, int Left) LongestPrintedInkRow(Bitmap bitmap)
   {
      (int Count, int Left) longest = default;
      for(int y = 0; y < bitmap.Height; y++)
      {
         int count = 0;
         int left = -1;
         for(int x = 0; x < bitmap.Width; x++)
         {
            if(IsPrintedInk(bitmap.GetPixel(x, y)))
            {
               count++;
               left = left < 0 ? x : left;
            }
         }
         if(count > longest.Count)
         {
            longest = (count, left);
         }
      }
      return longest;
   }



   /// <summary>Counts dark printed ink only; backgrounds are disabled and the fixture's light heading rules are excluded.</summary>
   private static bool IsPrintedInk(Color pixel) => pixel.R < 100 && pixel.G < 100 && pixel.B < 100;
}
