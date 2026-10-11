/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.Storage.Pickers;


namespace MarkdownViewr.Windows;

/// <summary>Coordinates deliberate printing and saved AppKit preferences without mutating the reading surface.</summary>
public sealed partial class MainWindow
{
   private bool _printing;



   /// <summary>Print is available only after a committed document is ready.</summary>
   private void InitializePrinting()
   {
      _browser.DocumentReady += (_, _) => RefreshPrintCommand();
      RefreshPrintCommand();
   }



   /// <summary>The native File command and Ctrl+P share the same guarded print path.</summary>
   private async void Print_Click(object sender, RoutedEventArgs args) => await PrintDocumentAsync();



   /// <summary>The Windows shortcut does not expose a document-created native command.</summary>
   private async void Print_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
   {
      args.Handled = true;
      await PrintDocumentAsync();
   }



   /// <summary>Commits only explicit print/PDF choices; cancellation, errors and completion preserve all reading state.</summary>
   private async Task PrintDocumentAsync()
   {
      if(_closed || _printing || !_browser.IsReady || _documentPath is null)
      {
         return;
      }
      _printing = true;
      RefreshPrintCommand();
      try
      {
         var dialog = new PrintOptionsDialog(_settings.Print, _themes);
         if(await dialog.ShowAsync(Content.XamlRoot) is not { } options || _closed)
         {
            return;
         }
         _settings.Print = options;
         Save();
         if(dialog.SaveAsPdf)
         {
            var picker = new FileSavePicker(AppWindow.Id) { SuggestedFileName = Path.GetFileNameWithoutExtension(_documentPath), DefaultFileExtension = ".pdf" };
            picker.FileTypeChoices.Add("PDF document", [".pdf"]);
            PickFileResult? file = await picker.PickSaveFileAsync();
            if(file is not null && !_closed)
            {
               StatusText.Text = "Saving PDF...";
               bool saved = await _browser.SavePdfAsync(file.Path, options, _themes);
               if(!_closed)
               {
                  StatusText.Text = saved ? "PDF saved." : "The PDF could not be saved.";
               }
            }
         }
         else
         {
            bool opened = await _browser.ShowPrintUiAsync(options, _themes);
            if(!_closed)
            {
               StatusText.Text = opened ? "Use the Windows print dialog to print or cancel." : "The print dialog could not be opened.";
            }
         }
      }
      catch(Exception exception) when(exception is System.Runtime.InteropServices.COMException or ObjectDisposedException or InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException or NotImplementedException)
      {
         if(!_closed)
         {
            StatusText.Text = "Printing could not be completed. Check the destination and WebView2 Runtime.";
            App.Log.Warning(FormattableString.Invariant($"Printing could not be completed ({exception.GetType().Name}, 0x{exception.HResult:X8})."));
         }
      }
      finally
      {
         _printing = false;
         RefreshPrintCommand();
      }
   }



   /// <summary>Busy and uncommitted documents cannot open multiple native print operations.</summary>
   private void RefreshPrintCommand() => PrintMenuItem.IsEnabled = !_closed && !_printing && _documentPath is not null && _browser.IsReady;
}
