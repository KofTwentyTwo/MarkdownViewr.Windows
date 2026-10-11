/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;


namespace MarkdownViewr.Windows;

/// <summary>Displays the same committed snapshot as literal text, without passing source through HTML.</summary>
public sealed partial class MainWindow
{
   private string _sourceMarkdown = "";



   /// <summary>Restores the independent source preference after XAML initialization.</summary>
   private void InitializeSource()
   {
      SourceToggle.IsChecked = _settings.ShowSource;
      RefreshSource();
      InitializeSourceSync();
   }



   /// <summary>A hidden source pane relinquishes its columns and accessibility surface.</summary>
   private void RefreshSource()
   {
      ApplyReadingWidths();
      SourceText.Visibility = _settings.ShowSource ? Visibility.Visible : Visibility.Collapsed;
      string text = _settings.ShowSource ? _sourceDocument?.Text ?? _sourceMarkdown : "";
      if(!string.Equals(SourceText.Text, text, StringComparison.Ordinal))
      {
         SourceText.Text = text;
         HoldSourceSync(preserveSelection: false);
      }
   }



   /// <summary>Constrains displayed pane widths while retaining the user's preferred widths for larger windows.</summary>
   private void ApplyReadingWidths()
   {
      double available = Math.Max(0, RootGrid.ActualWidth - 200);
      double outline = _settings.ShowOutline ? Math.Min(_settings.OutlineWidth, Math.Max(0, available - 6 - (_settings.ShowSource ? 166 : 0))) : 0;
      double source = _settings.ShowSource ? Math.Min(_settings.SourceWidth, Math.Max(0, available - outline - (_settings.ShowOutline ? 6 : 0) - 6)) : 0;
      OutlineColumn.Width = new GridLength(outline);
      OutlineDividerColumn.Width = new GridLength(_settings.ShowOutline ? 6 : 0);
      SourceColumn.Width = new GridLength(source);
      SourceDividerColumn.Width = new GridLength(_settings.ShowSource ? 6 : 0);
   }



   /// <summary>Keeps at least 200 reading pixels when the window shrinks or both panes are open.</summary>
   private void ReadingArea_SizeChanged(object sender, SizeChangedEventArgs e) => ApplyReadingWidths();


   /// <summary>Persists the view choice while preserving the actual Markdown, including hidden frontmatter.</summary>
   private void Source_Click(object sender, RoutedEventArgs e)
   {
      _settings.ShowSource = SourceToggle.IsChecked;
      Save();
      RefreshSource();
      QueueFind();
   }



   /// <summary>The source pane grows toward the left and stays within its validated width range.</summary>
   private void SourceDivider_DragDelta(object sender, DragDeltaEventArgs e)
   {
      _settings.SourceWidth = Math.Clamp(SourceColumn.ActualWidth - e.HorizontalChange, 160, 600);
      ApplyReadingWidths();
   }



   /// <summary>Writes the completed resize through AppKit's atomic settings store.</summary>
   private void SourceDivider_DragCompleted(object sender, DragCompletedEventArgs e) => Save();
}
