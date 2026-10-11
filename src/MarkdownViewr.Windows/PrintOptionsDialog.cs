/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Printing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>Provides a copied print draft with separate native print, PDF save and cancellation actions.</summary>
internal sealed class PrintOptionsDialog
{
   private readonly PrintOptionsEditor _editor;
   private readonly CheckBox _openPdf = new() { Name = "PrintOpenPdf", Content = "Open PDF after saving" };
   private readonly CheckBox _openFolder = new() { Name = "PrintOpenPdfFolder", Content = "Open containing folder after saving" };
   private readonly ContentDialog _dialog;



   /// <summary>Restores bounded saved choices; missing named themes fall back to Clean Printing.</summary>
   public PrintOptionsDialog(PrintOptions options, IEnumerable<Core.Themes.ReaderTheme> themes)
   {
      _editor = new PrintOptionsEditor(options, themes);
      var panel = new StackPanel { Spacing = 8, MinWidth = 300 };
      panel.Children.Add(_editor.Fields);
      foreach(FrameworkElement control in new FrameworkElement[] { _openPdf, _openFolder })
      {
         AutomationProperties.SetAutomationId(control, control.Name);
         panel.Children.Add(control);
      }
      _dialog = new ContentDialog
      {
         Title = "Print document",
         Content = new ScrollViewer { Content = panel, MaxHeight = 320 },
         PrimaryButtonText = "Print...",
         SecondaryButtonText = "Save PDF...",
         CloseButtonText = "Cancel",
         DefaultButton = ContentDialogButton.Primary,
      };
      AutomationProperties.SetAutomationId(_dialog, "PrintOptionsDialog");
      _dialog.Resources["ContentDialogMaxWidth"] = 640d;
      _dialog.PrimaryButtonClick += (_, args) => args.Cancel = _editor.Read() is null;
      _dialog.SecondaryButtonClick += (_, args) => args.Cancel = _editor.Read() is null;
   }



   /// <summary>Only committed print/PDF actions return preferences; closing the draft leaves saved choices untouched.</summary>
   public async Task<PrintOptions?> ShowAsync(XamlRoot root)
   {
      double width = Math.Max(0, Math.Min(520, root.Size.Width - 96));
      ((FrameworkElement)((ScrollViewer)_dialog.Content).Content).MinWidth = width;
      _dialog.Resources["ContentDialogMinWidth"] = width + 48;
      _dialog.XamlRoot = root;
      ContentDialogResult result = await _dialog.ShowAsync();
      SaveAsPdf = result == ContentDialogResult.Secondary;
      return result == ContentDialogResult.None ? null : _editor.Read();
   }



   /// <summary>The selected destination is an action, not a persisted output path.</summary>
   public bool SaveAsPdf { get; private set; }



   /// <summary>A deliberate post-export action, never run after cancellation or failed generation.</summary>
   public bool OpenPdfAfterSave => _openPdf.IsChecked == true;



   /// <summary>The folder may be opened independently or together with the generated PDF.</summary>
   public bool OpenFolderAfterSave => _openFolder.IsChecked == true;
}
