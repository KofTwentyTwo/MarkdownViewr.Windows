/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using System.Text.Json;
using MarkdownViewr.Windows.Core.Themes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;


namespace MarkdownViewr.Windows;

/// <summary>Native theme rows preview validated palettes and state without depending on color alone.</summary>
internal sealed partial class ThemeManagementDialog
{
   /// <summary>Accessible names and textual built-in/enabled/default labels remain readable in light, dark and high contrast.</summary>
   private static StackPanel ThemeRow(ReaderTheme theme, bool enabled, bool isDefault, bool edited)
   {
      var row = new StackPanel { Spacing = 4, Padding = new Thickness(0, 4, 0, 4) };
      row.Children.Add(new TextBlock { Text = theme.Name, TextWrapping = TextWrapping.Wrap, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
      var palette = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
      using var document = JsonDocument.Parse(theme.Json);
      JsonElement colors = document.RootElement.GetProperty("colors");
      foreach((string field, string label) in new[] { ("background", "Background"), ("text", "Text"), ("heading1", "Heading 1"), ("heading2", "Heading 2"), ("link", "Link") })
      {
         palette.Children.Add(ColorPreview(theme.Name, field, label, colors.GetProperty(field).GetString()!));
      }
      row.Children.Add(palette);
      bool builtIn = ThemeCatalog.BuiltIn.Any(item => string.Equals(item.Name, theme.Name, StringComparison.Ordinal));
      string kind = builtIn ? (edited ? "Built-in (edited)" : "Built-in") : "Custom";
      var state = new TextBlock { Text = kind + " · " + (enabled ? "Enabled" : "Disabled") + (isDefault ? " · Default" : ""), TextWrapping = TextWrapping.Wrap, FontSize = 12, Opacity = 0.7 };
      AutomationProperties.SetAutomationId(state, "ThemeState:" + theme.Name);
      row.Children.Add(state);
      return row;
   }



   /// <summary>Only validated six-digit palette colors become native brushes; the hexadecimal value also has a textual name/tooltip.</summary>
   private static Border ColorPreview(string theme, string field, string label, string value)
   {
      var color = global::Windows.UI.Color.FromArgb(255,
         byte.Parse(value.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
         byte.Parse(value.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
         byte.Parse(value.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
      var brush = new SolidColorBrush(color);
      var swatch = new TextBlock { Text = "●", Width = 18, Height = 18, FontSize = 14, TextAlignment = TextAlignment.Center, Foreground = brush };
      AutomationProperties.SetName(swatch, label + " color " + value);
      AutomationProperties.SetAutomationId(swatch, "ThemePalette:" + theme + ":" + field);
      var border = new Border { Width = 20, Height = 20, CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 128, 128, 128)), Background = brush, Child = swatch };
      ToolTipService.SetToolTip(border, label + ": " + value);
      return border;
   }
}
