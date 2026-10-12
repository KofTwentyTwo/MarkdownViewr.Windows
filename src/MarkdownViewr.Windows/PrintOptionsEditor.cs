/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Printing;
using MarkdownViewr.Windows.Core.Themes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>Settings and the print dialog share the same native bounded options and validation.</summary>
internal sealed class PrintOptionsEditor
{
   private static readonly string[] s_specialThemes = ["Clean Printing", "Active Theme", "Plain HTML"];
   private readonly ComboBox _theme = new() { Name = "PrintTheme", Header = "Print theme", HorizontalAlignment = HorizontalAlignment.Stretch };
   private readonly CheckBox _backgrounds = new() { Name = "PrintBackgrounds", Content = "Print backgrounds" };
   private readonly CheckBox _images = new() { Name = "PrintImages", Content = "Print images" };
   private readonly ComboBox _zoom = new() { Name = "PrintZoom", Header = "Font zoom", HorizontalAlignment = HorizontalAlignment.Stretch };
   private readonly ComboBox _width = new() { Name = "PrintWidth", Header = "Content width", HorizontalAlignment = HorizontalAlignment.Stretch };
   private readonly CheckBox _borders = new() { Name = "PrintBorders", Content = "Add borders to code and content blocks" };
   private readonly NumberBox _padding = new() { Name = "PrintPadding", Header = "Page padding (px)", Minimum = 0, Maximum = 96, SmallChange = 8, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline };
   private readonly TextBlock _error = new() { Name = "PrintError", TextWrapping = TextWrapping.Wrap };



   /// <summary>Restores saved options before subscribing to user changes, using the available named themes.</summary>
   public PrintOptionsEditor(PrintOptions options, IEnumerable<ReaderTheme> themes)
   {
      SetThemes(themes);
      _zoom.Items.Add("Standard (100%)");
      _zoom.Items.Add("Match screen");
      _width.Items.Add("Full page");
      _width.Items.Add("Document width");
      Update(options);
      foreach(FrameworkElement control in new FrameworkElement[] { _theme, _backgrounds, _images, _zoom, _width, _borders, _padding, _error })
      {
         AutomationProperties.SetAutomationId(control, control.Name);
         Fields.Children.Add(control);
      }
      foreach(ComboBox control in new[] { _theme, _zoom, _width })
      {
         control.SelectionChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
      }
      foreach(CheckBox control in new[] { _backgrounds, _images, _borders })
      {
         control.Checked += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
         control.Unchecked += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
      }
      _padding.ValueChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
   }



   /// <summary>One native form is hosted in either the regular Settings scroll region or the deliberate print dialog.</summary>
   public StackPanel Fields { get; } = new() { Spacing = 12, MinWidth = 300 };



   /// <summary>Hosts can retain unsubmitted drafts when another window saves newer print defaults.</summary>
   public event EventHandler? Changed;



   /// <summary>The current enabled theme catalog replaces stale names without losing the other print choices.</summary>
   public void SetThemes(IEnumerable<ReaderTheme> themes)
   {
      string? selected = (_theme.SelectedItem as ComboBoxItem)?.Content as string;
      _theme.Items.Clear();
      foreach(string name in s_specialThemes.Concat(themes.Select(theme => theme.Name)).Distinct(StringComparer.Ordinal))
      {
         _theme.Items.Add(new ComboBoxItem { Content = name });
      }
      SelectTheme(selected);
   }



   /// <summary>Only a known saved name is selected; removed named themes use Clean Printing.</summary>
   private void SelectTheme(string? name)
      => _theme.SelectedItem = _theme.Items.OfType<ComboBoxItem>().FirstOrDefault(item => string.Equals((string)item.Content, name, StringComparison.Ordinal)) ?? _theme.Items[0];



   /// <summary>Committed defaults refresh routine fields while the host controls whether a draft should be preserved.</summary>
   public void Update(PrintOptions options)
   {
      options = options.Sanitize();
      SelectTheme(options.Theme);
      _backgrounds.IsChecked = options.Backgrounds;
      _images.IsChecked = options.Images;
      _zoom.SelectedIndex = options.MatchScreenZoom ? 1 : 0;
      _width.SelectedIndex = options.FullPage ? 0 : 1;
      _borders.IsChecked = options.Borders;
      _padding.Value = options.PaddingPixels;
      _error.Text = "";
   }



   /// <summary>Invalid page padding remains a visible draft and cannot reach saved preferences or output.</summary>
   public PrintOptions? Read()
   {
      if(!double.IsFinite(_padding.Value) || _padding.Value is < 0 or > 96)
      {
         _error.Text = "Enter page padding between 0 and 96 pixels.";
         return null;
      }
      _error.Text = "";
      return new PrintOptions((string)((ComboBoxItem)_theme.SelectedItem).Content,
         _backgrounds.IsChecked == true, _images.IsChecked == true, _zoom.SelectedIndex == 1,
         _width.SelectedIndex == 0, _borders.IsChecked == true, _padding.Value);
   }
}
