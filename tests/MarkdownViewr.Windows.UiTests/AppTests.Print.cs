/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Drawing;
using System.Drawing.Imaging;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using MarkdownViewr.Windows.Core.Printing;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Exercises native printing with owned PDF fixtures, actual Windows rasterization and explicit cancellation.</summary>
public partial class AppTests
{
   /// <summary>Physical Ctrl+P opens a copied draft; cancellation retains actual source/find/zoom and saved preferences.</summary>
   [Fact]
   public void Print_KeyboardDraftCancel_PreservesReadingStateAndPreferences()
   {
      using var session = new AppSession(document: "# Print cancellation fixture\n\nSearch needle for paired find.\n\n## Print child\nMore text.");
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-0")), "the print fixture", TimeSpan.FromSeconds(30));
      session.ClickElement("ZoomInButton");
      session.InvokeMenuItem("View", "SourceToggle");
      session.InvokeMenuItem("Edit", "FindMenuItem");
      session.WaitForElement("FindText").AsTextBox().Text = "Search needle";
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("FindCountText").And(cf.ByName("1 of 1"))), "the actual paired print-fixture match");
      double height = session.FindInApp(cf => cf.ByAutomationId("mv-heading-0"))!.BoundingRectangle.Height;
      string settings = session.ReadSettings();
      string source = session.WaitForElement("SourceText").Patterns.Value.Pattern.Value.Value;
      FocusBrowser(session);
      SendReadingKey(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_P);
      session.WaitForElement("PrintImages").AsCheckBox().IsChecked = false;
      Assert.True(session.WaitForElement("PrintTheme").BoundingRectangle.Width >= 440, "The native print options must provide a readable wide layout.");
      Assert.NotNull(session.WaitForElement("PrintOpenPdf"));
      Assert.NotNull(session.WaitForElement("PrintOpenPdfFolder"));
      session.ClickButton("Cancel");
      session.WaitForElementGone("PrintTheme");
      Assert.Equal(settings, session.ReadSettings());
      Assert.Equal(source, session.WaitForElement("SourceText").Patterns.Value.Pattern.Value.Value);
      Assert.Equal("Search needle", session.WaitForElement("FindText").AsTextBox().Text);
      Assert.Equal("1 of 1", session.WaitForElement("FindCountText").Name);
      WaitForZoomGlyph(session, height);
   }



   /// <summary>The real Windows print dialog is opened then cancelled; no job is sent to any printer.</summary>
   [Fact]
   public void Print_WindowsDialogCancel_PreservesReadyDocument()
   {
      using var session = new AppSession(document: "# Windows print fixture\n\nNever submit this fixture to a physical printer.");
      AutomationElement heading = session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-0")), "the Windows print fixture", TimeSpan.FromSeconds(30));
      double height = heading.BoundingRectangle.Height;
      session.InvokeMenuItem("File", "PrintMenuItem");
      session.ClickButton("Print...");
      session.WaitForElementGone("PrintTheme");
      session.WaitFor(() => session.FindInApp(cf => cf.ByControlType(ControlType.Button).And(cf.ByName("Cancel")).And(cf.ByClassName("Button"))), "the owned Windows print dialog's Cancel button");
      session.ClickButton("Cancel");
      WaitForZoomGlyph(session, height);
      Assert.NotNull(session.FindInApp(cf => cf.ByName("Windows print fixture")));
   }



   /// <summary>Actual native PDFs expand folded content, apply image/background preferences and leave screen collapse/zoom intact.</summary>
   [Fact]
   public async Task Print_SavePdf_RendersPrintOptionsAndPreservesScreen()
   {
      byte[] imageBytes = CreatePrintImage();
      string paragraphs = string.Join("\n\n", Enumerable.Repeat("Reading position fixture text retains its native viewport and source location through deliberate printing.", 10));
      using var session = new AppSession(document: "# Printed heading fixture\n\n" + paragraphs + "\n\n## Folded image\n\n![owned red image](red.png)\n\nHidden print text.", documentAssets: new Dictionary<string, byte[]>(StringComparer.Ordinal) { ["red.png"] = imageBytes });
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-0")), "the PDF fixture heading", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "OutlineToggle");
      session.ClickElement("OutlineHeading:1");
      session.ClickElement("ToggleSection");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("owned red image")) is null ? session.MainWindow : null, "the actual image hidden by section collapse");
      session.ClickElement("OutlineHeading:0");
      double original = session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-0")), "the visible PDF fixture heading").BoundingRectangle.Height;
      Assert.True(original > 0);
      session.ClickElement("ZoomInButton");
      WaitForZoomGlyph(session, original * 1.1);
      session.InvokeMenuItem("View", "SourceToggle");
      session.ClickElement("OutlineHeading:1");
      (Rectangle Glyph, Rectangle View) reading = WaitForStablePrintViewport(session);
      Assert.True(session.WaitForElement("mv-heading-0").BoundingRectangle.Top < reading.View.Top);
      string source = session.WaitForElement("SourceText").Patterns.Value.Pattern.Value.Value;
      string first = Path.Combine(session.DataDirectory, "default-print.pdf");
      SavePrintPdf(session, first);
      string firstEvidence = RetainPrintedFixture(session, first);
      using Bitmap clean = await RenderFirstPdfPageAsync(first);
      clean.Save(firstEvidence + ".png", ImageFormat.Png);
      Assert.True(CountPixels(clean, pixel => pixel.R > 180 && pixel.G < 80 && pixel.B < 80) > 400, "The expanded print PDF is missing the owned red image.");
      Assert.True(CountPixels(clean, pixel => pixel.R > 245 && pixel.G > 245 && pixel.B > 245) > clean.Width * clean.Height * 0.8, "Ink-saving print output must have a predominantly white page.");
      Assert.True(CountPixels(clean, pixel => pixel.R < 100 && pixel.G < 100 && pixel.B < 100) > 100, "The actual PDF is missing printed heading/body glyphs.");
      AssertPrintScreenState(session, reading, source);
      string second = Path.Combine(session.DataDirectory, "themed-print.pdf");
      SavePrintPdf(session, second, new PrintOptions("Active Theme", true, false, true, false, true));
      string secondEvidence = RetainPrintedFixture(session, second);
      using Bitmap themed = await RenderFirstPdfPageAsync(second);
      themed.Save(secondEvidence + ".png", ImageFormat.Png);
      Assert.Equal(0, CountPixels(themed, pixel => pixel.R > 180 && pixel.G < 80 && pixel.B < 80));
      Assert.True(CountPixels(themed, pixel => Math.Abs(pixel.R - 30) < 3 && Math.Abs(pixel.G - 30) < 3 && Math.Abs(pixel.B - 46) < 3) > 1000, "Active-theme backgrounds did not reach the actual printed PDF.");
      AssertPrintScreenState(session, reading, source);
   }



   /// <summary>Creates only the owned red image used to prove expanded print content and the images toggle.</summary>
   private static byte[] CreatePrintImage()
   {
      using var asset = new Bitmap(40, 40);
      using(var graphics = Graphics.FromImage(asset))
      {
         graphics.Clear(Color.Red);
      }
      using var bytes = new MemoryStream();
      asset.Save(bytes, ImageFormat.Png);
      return bytes.ToArray();
   }



   /// <summary>Checks the real viewport, visible glyph size, hidden browser image and literal native source after printing.</summary>
   private static void AssertPrintScreenState(AppSession session, (Rectangle Glyph, Rectangle View) reading, string source)
   {
      Assert.Null(session.FindInApp(cf => cf.ByName("owned red image")));
      try
      {
         session.WaitFor(() =>
         {
            AutomationElement heading = session.WaitForElement("mv-heading-1");
            return heading.BoundingRectangle == reading.Glyph && session.WaitForElement("DocumentView").BoundingRectangle == reading.View ? heading : null;
         }, "the unchanged actual heading and reading viewport after PDF output");
      }
      catch(TimeoutException exception)
      {
         throw new TimeoutException(exception.Message + " " + session.ReadLogs(), exception);
      }
      Assert.Equal(source, session.WaitForElement("SourceText").Patterns.Value.Pattern.Value.Value);
   }



   /// <summary>Uses the actual app options and Windows Save picker; output is always an owned fixture path.</summary>
   private void SavePrintPdf(AppSession session, string path, PrintOptions? options = null)
   {
      try
      {
         session.InvokeMenuItem("File", "PrintMenuItem");
         session.WaitForElement("PrintTheme");
         if(options is not null)
         {
            ConfigurePrintOptions(session, options);
         }
         session.ClickButton("Save PDF...");
         AutomationElement filename = session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("1001").And(cf.ByControlType(ControlType.Edit))), "the owned Windows PDF filename field");
         EnterPickerFilename(session, filename, path);
         session.ClickButton("Save");
         session.WaitFor(() => File.Exists(path) && new FileInfo(path).Length > 100 ? session.FindInApp(cf => cf.ByAutomationId("StatusText").And(cf.ByName("PDF saved."))) : null, "the completed actual PDF export", TimeSpan.FromSeconds(30));
      }
      catch(TimeoutException)
      {
         _output.WriteLine("Owned export files: " + string.Join(", ", Directory.GetFiles(session.DataDirectory, "*.pdf").Select(Path.GetFileName)));
         _output.WriteLine(session.ReadLogs());
         _output.WriteLine(session.DescribeDocumentStatus());
         throw;
      }
   }



   /// <summary>Waits for settled actual glyph/view rectangles; WebView2 can report stale out-of-range UIA scroll percentages.</summary>
   private static (Rectangle Glyph, Rectangle View) WaitForStablePrintViewport(AppSession session)
   {
      (Rectangle Glyph, Rectangle View) previous = default;
      int stable = 0;
      session.WaitFor(() =>
      {
         AutomationElement heading = session.WaitForElement("mv-heading-1");
         (Rectangle Glyph, Rectangle View) current = (heading.BoundingRectangle, session.WaitForElement("DocumentView").BoundingRectangle);
         stable = current == previous ? stable + 1 : 0;
         previous = current;
         return current.Glyph.Height > 0 && current.View.IntersectsWith(current.Glyph) && stable >= 4 ? heading : null;
      }, "the settled actual heading and reading viewport before printing");
      return previous;
   }



   /// <summary>Physical text entry commits the Windows picker's actual destination instead of only its UIA value.</summary>
   private static void EnterPickerFilename(AppSession session, AutomationElement filename, string path)
   {
      filename.Focus();
      SendReadingKey(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
      Keyboard.Type(path);
      Wait.UntilInputIsProcessed();
      try
      {
         session.WaitFor(() => string.Equals(filename.AsTextBox().Text, path, StringComparison.Ordinal) ? filename : null, "the fully committed exact owned picker filename");
      }
      catch(TimeoutException exception)
      {
         throw new TimeoutException(exception.Message + "; Expected=" + path + "; Actual=" + filename.AsTextBox().Text + "; Last invocation=" + session.LastInvocation, exception);
      }
   }



   /// <summary>Windows' built-in PDF renderer verifies actual exported bytes without another parser/renderer dependency.</summary>
   private static async Task<Bitmap> RenderFirstPdfPageAsync(string path)
   {
      global::Windows.Storage.StorageFile file = await global::Windows.Storage.StorageFile.GetFileFromPathAsync(path);
      global::Windows.Data.Pdf.PdfDocument document = await global::Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);
      Assert.True(document.PageCount > 0);
      using global::Windows.Data.Pdf.PdfPage page = document.GetPage(0);
      using var image = new global::Windows.Storage.Streams.InMemoryRandomAccessStream();
      await page.RenderToStreamAsync(image, new global::Windows.Data.Pdf.PdfPageRenderOptions { DestinationWidth = 816, DestinationHeight = 1056 });
      image.Seek(0);
      using Stream stream = image.AsStreamForRead();
      using var bitmap = new Bitmap(stream);
      return new Bitmap(bitmap);
   }



   /// <summary>Counts actual rendered fixture pixels; no application-reported theme or PDF-success flag can satisfy image assertions.</summary>
   private static int CountPixels(Bitmap bitmap, Func<Color, bool> predicate)
   {
      int count = 0;
      for(int y = 0; y < bitmap.Height; y++)
      {
         for(int x = 0; x < bitmap.Width; x++)
         {
            count += predicate(bitmap.GetPixel(x, y)) ? 1 : 0;
         }
      }
      return count;
   }



   /// <summary>Retains only owned generated fixture output alongside existing installed acceptance evidence.</summary>
   private static string RetainPrintedFixture(AppSession session, string pdf)
   {
      string directory = Path.Combine(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? session.DataDirectory, "artifacts", "installed-smoke-print");
      Directory.CreateDirectory(directory);
      string name = Guid.NewGuid().ToString("N");
      string output = Path.Combine(directory, name);
      File.Copy(pdf, output + ".pdf");
      return output;
   }
}
