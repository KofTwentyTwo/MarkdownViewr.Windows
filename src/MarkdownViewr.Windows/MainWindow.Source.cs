/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;


namespace MarkdownViewr.Windows;

/// <summary>Displays the same committed snapshot as literal text, without passing source through HTML.</summary>
public sealed partial class MainWindow
{
   private string _sourceMarkdown = "";
   private double _sourceFraction = 0.5;



   /// <summary>Restores the independent source preference after XAML initialization.</summary>
   private void InitializeSource()
   {
      SourceText.SetTheme(ActiveDocumentTheme());
      SourceText.CopyFailed += (_, _) =>
      {
         if(!_closed)
         {
            StatusText.Text = "The source could not be copied. Please try again.";
         }
      };
      SourceToggle.IsChecked = _settings.ShowSource;
      RefreshSource();
      InitializeSourceSync();
   }



   /// <summary>A hidden source pane relinquishes its columns and accessibility surface.</summary>
   private void RefreshSource()
   {
      ApplyReadingWidths();
      RefreshToolbarReadingState();
      SourceText.Visibility = _settings.ShowSource ? Visibility.Visible : Visibility.Collapsed;
      string text = _settings.ShowSource ? _sourceDocument?.Text ?? _sourceMarkdown : "";
      bool changed = !string.Equals(SourceText.Text, text, StringComparison.Ordinal);
      SourceText.SetSource(text, _sourceDocument);
      if(changed)
      {
         HoldSourceSync(preserveSelection: false);
      }
   }



   /// <summary>Splits the document/source space proportionally, excluding the outline and its divider.</summary>
   private void ApplyReadingWidths()
   {
      double available = Math.Max(0, RootGrid.ActualWidth - 200);
      double outline = _settings.ShowOutline ? Math.Min(_settings.OutlineWidth, Math.Max(0, available - 6 - (_settings.ShowSource ? 166 : 0))) : 0;
      double shared = Math.Max(0, RootGrid.ActualWidth - outline - (_settings.ShowOutline ? 6 : 0) - 6);
      double fraction = Core.Layout.SourcePaneLayout.Fraction(shared, _sourceFraction);
      OutlineColumn.Width = new GridLength(outline);
      OutlineDividerColumn.Width = new GridLength(_settings.ShowOutline ? 6 : 0);
      DocumentColumn.Width = new GridLength(_settings.ShowSource ? 1 - fraction : 1, GridUnitType.Star);
      SourceColumn.Width = _settings.ShowSource ? new GridLength(fraction, GridUnitType.Star) : new GridLength(0);
      SourceDividerColumn.Width = new GridLength(_settings.ShowSource ? 6 : 0);
      SourceDivider.IsTabStop = _settings.ShowSource;
   }



   /// <summary>Keeps at least 200 reading pixels when the window shrinks or both panes are open.</summary>
   private void ReadingArea_SizeChanged(object sender, SizeChangedEventArgs e) => ApplyReadingWidths();


   /// <summary>Persists the view choice while preserving the actual Markdown, including hidden frontmatter.</summary>
   private void Source_Click(object sender, RoutedEventArgs e)
   {
      _settings.ShowSource = SourceToggle.IsChecked;
      if(_settings.ShowSource)
      {
         _sourceFraction = 0.5;
      }
      Save();
      RefreshSource();
      QueueFind();
   }



   /// <summary>The source pane grows toward the left within the available shared reading area.</summary>
   private void SourceDivider_DragDelta(object sender, DragDeltaEventArgs e) => ResizeSource(e.HorizontalChange);



   /// <summary>Keyboard users resize with Left/Right or restore equal panes with Home.</summary>
   private void SourceDivider_KeyDown(object sender, KeyRoutedEventArgs e)
   {
      if(!_settings.ShowSource)
      {
         return;
      }
      switch(e.Key)
      {
         case global::Windows.System.VirtualKey.Left:
            ResizeSource(-24);
            break;
         case global::Windows.System.VirtualKey.Right:
            ResizeSource(24);
            break;
         case global::Windows.System.VirtualKey.Home:
            _sourceFraction = 0.5;
            ApplyReadingWidths();
            break;
         default:
            return;
      }
      e.Handled = true;
   }



   /// <summary>The ratio belongs to this window; reopening source intentionally starts again at50/50.</summary>
   private void ResizeSource(double horizontalChange)
   {
      double shared = DocumentColumn.ActualWidth + SourceColumn.ActualWidth;
      _sourceFraction = Core.Layout.SourcePaneLayout.Resize(shared, SourceColumn.ActualWidth, horizontalChange);
      ApplyReadingWidths();
   }
}
