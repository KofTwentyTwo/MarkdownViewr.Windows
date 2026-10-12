/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;
using Microsoft.Web.WebView2.Core;


namespace MarkdownViewr.Windows;

/// <summary>Prints the current passive snapshot through deliberate native commands; reading DOM state remains unchanged.</summary>
internal sealed partial class DocumentBrowser
{
   /// <summary>Opens the Windows print dialog without assuming that the void native API reports completion or cancellation.</summary>
   public async Task<bool> ShowPrintUiAsync(Core.Printing.PrintOptions options, IEnumerable<Core.Themes.ReaderTheme> themes)
   {
      if(!await PreparePrintAsync(options, themes))
      {
         App.Log.Warning("System print preparation was rejected; the document may have changed or is not ready.");
         return false;
      }
      App.Log.Info("Requesting the Windows system print dialog with WebView2 " + _view.CoreWebView2.Environment.BrowserVersionString + ".");
      _view.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System);
      return true;
   }



   /// <summary>Exports an explicitly chosen PDF destination through native printing, independent of physical printers.</summary>
   public async Task<bool> SavePdfAsync(string path, Core.Printing.PrintOptions options, IEnumerable<Core.Themes.ReaderTheme> themes)
   {
      if(!await PreparePrintAsync(options, themes))
      {
         return false;
      }
      Uri document = _policy.DocumentUri;
      ulong navigation = _navigationId;
      Core.Documents.DocumentScrollState? reading = await CaptureScrollStateAsync();
      if(reading is null || !IsMatchingFindDocument(document, navigation))
      {
         return false;
      }
      CoreWebView2PrintSettings settings = CreatePdfSettings(options.Backgrounds);
      try
      {
         bool saved = await _view.CoreWebView2.PrintToPdfAsync(path, settings);
         if(!saved)
         {
            App.Log.Warning("The WebView2 PDF export returned failure.");
            return false;
         }
         global::Windows.Storage.StorageFile file = await global::Windows.Storage.StorageFile.GetFileFromPathAsync(path);
         global::Windows.Data.Pdf.PdfDocument pdf = await global::Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);
         bool readable = pdf.PageCount > 0;
         App.Log.Info(readable ? "The generated PDF was verified readable." : "The generated PDF contains no pages.");
         return readable;
      }
      finally
      {
         if(IsMatchingFindDocument(document, navigation))
         {
            await ScrollToSourcePositionAsync(reading.Position);
         }
      }
   }



   /// <summary>Configures reference page margins and font-only scaling without browser headers or footers.</summary>
   private CoreWebView2PrintSettings CreatePdfSettings(bool backgrounds)
   {
      CoreWebView2PrintSettings settings = _environment!.CreatePrintSettings();
      settings.ShouldPrintBackgrounds = backgrounds;
      settings.ShouldPrintHeaderAndFooter = false;
      settings.ScaleFactor = 1;
      // The reference uses 36-point page margins: WebView2's native settings use inches.
      settings.MarginTop = settings.MarginBottom = settings.MarginLeft = settings.MarginRight = 0.5;
      return settings;
   }



   /// <summary>Installs CSS only into a host-owned print-media stylesheet, guarding asynchronous work against navigation.</summary>
   private async Task<bool> PreparePrintAsync(Core.Printing.PrintOptions options, IEnumerable<Core.Themes.ReaderTheme> themes)
   {
      if(_closed || !_pageReady || _files is null || !IsCurrentDocument())
      {
         return false;
      }
      Uri document = _policy.DocumentUri;
      ulong navigation = _navigationId;
      string css = Core.Printing.PrintStyles.Build(options, _theme, themes, _zoomScale);
      string? result = await ExecuteDocumentScriptAsync("""
         (()=>{let s=document.getElementById('mv-print-theme');
         if(!s){s=document.createElement('style');s.id='mv-print-theme';s.media='print';document.head.appendChild(s);}
         s.textContent=
         """ + JsonSerializer.Serialize(css) + ";return true;})()");
      return string.Equals(result, "true", StringComparison.Ordinal) && IsMatchingFindDocument(document, navigation);
   }
}
