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
   private static readonly string[] s_specialThemes = ["Clean Printing", "Active Theme", "Plain HTML"];
   private readonly ComboBox _theme = new() { Name = "PrintTheme", Header = "Print theme", HorizontalAlignment = HorizontalAlignment.Stretch };
   private readonly CheckBox _backgrounds = new() { Name = "PrintBackgrounds", Content = "Print backgrounds" };
   private readonly CheckBox _images = new() { Name = "PrintImages", Content = "Print images" };
   private readonly ComboBox _zoom = new() { Name = "PrintZoom", Header = "Font zoom", HorizontalAlignment = HorizontalAlignment.Stretch };
   private readonly ComboBox _width = new() { Name = "PrintWidth", Header = "Content width", HorizontalAlignment = HorizontalAlignment.Stretch };
   private readonly CheckBox _borders = new() { Name = "PrintBorders", Content = "Add borders to code and content blocks" };
   private readonly NumberBox _padding = new() { Name = "PrintPadding", Header = "Page padding (px)", Minimum = 0, Maximum = 96, SmallChange = 8, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
   private readonly CheckBox _openPdf = new() { Name = "PrintOpenPdf", Content = "Open PDF after saving" };
   private readonly CheckBox _openFolder = new() { Name = "PrintOpenPdfFolder", Content = "Open containing folder after saving" };
   private readonly TextBlock _error = new() { Name = "PrintError", TextWrapping = TextWrapping.Wrap };
   private readonly ContentDialog _dialog;



   /// <summary>Restores bounded saved choices; missing named themes fall back to Clean Printing.</summary>
   public PrintOptionsDialog(PrintOptions options, IEnumerable<Core.Themes.ReaderTheme> themes)
   {
      options = options.Sanitize();
      foreach(string name in s_specialThemes.Concat(themes.Select(theme => theme.Name)).Distinct(StringComparer.Ordinal))
      {
         _theme.Items.Add(new ComboBoxItem { Content = name });
      }
      _theme.SelectedItem = _theme.Items.OfType<ComboBoxItem>().FirstOrDefault(item => string.Equals((string)item.Content, options.Theme, StringComparison.Ordinal)) ?? _theme.Items[0];
      _backgrounds.IsChecked = options.Backgrounds;
      _images.IsChecked = options.Images;
      _zoom.Items.Add("Standard (100%)");
      _zoom.Items.Add("Match screen");
      _zoom.SelectedIndex = options.MatchScreenZoom ? 1 : 0;
      _width.Items.Add("Full page");
      _width.Items.Add("Document width");
      _width.SelectedIndex = options.FullPage ? 0 : 1;
      _borders.IsChecked = options.Borders;
      _padding.Value = options.PaddingPixels;
      var panel = new StackPanel { Spacing = 8, MinWidth = 300 };
      foreach(FrameworkElement control in new FrameworkElement[] { _theme, _backgrounds, _images, _zoom, _width, _borders, _padding, _openPdf, _openFolder, _error })
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
      _dialog.PrimaryButtonClick += (_, args) => args.Cancel = !ValidatePadding();
      _dialog.SecondaryButtonClick += (_, args) => args.Cancel = !ValidatePadding();
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
      return result == ContentDialogResult.None ? null : new PrintOptions((string)((ComboBoxItem)_theme.SelectedItem).Content,
         _backgrounds.IsChecked == true, _images.IsChecked == true, _zoom.SelectedIndex == 1,
         _width.SelectedIndex == 0, _borders.IsChecked == true, _padding.Value);
   }



   /// <summary>The selected destination is an action, not a persisted output path.</summary>
   public bool SaveAsPdf { get; private set; }



   /// <summary>A deliberate post-export action, never run after cancellation or failed generation.</summary>
   public bool OpenPdfAfterSave => _openPdf.IsChecked == true;



   /// <summary>The folder may be opened independently or together with the generated PDF.</summary>
   public bool OpenFolderAfterSave => _openFolder.IsChecked == true;



   /// <summary>A blank or invalid number stays in the draft instead of silently changing the user's page layout.</summary>
   private bool ValidatePadding()
   {
      bool valid = double.IsFinite(_padding.Value) && _padding.Value is >= 0 and <= 96;
      _error.Text = valid ? "" : "Enter page padding between 0 and 96 pixels.";
      return valid;
   }
}
