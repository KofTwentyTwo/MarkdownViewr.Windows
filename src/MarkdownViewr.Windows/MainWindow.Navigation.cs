/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;


namespace MarkdownViewr.Windows;

/// <summary>Native outline controls route only bounded numeric heading operations to the passive browser.</summary>
public sealed partial class MainWindow
{
   private Core.Documents.DocumentOutline? _outline;
   private bool _navigationReady;
   private bool _refreshingOutline;



   /// <summary>Applies persisted sidebar appearance after XAML event initialization has finished.</summary>
   private void InitializeOutline()
   {
      OutlineToggle.IsChecked = _settings.ShowOutline;
      OutlineDepth.SelectedIndex = _settings.OutlineDepth - 1;
      _navigationReady = true;
      ApplyOutlineVisibility();
   }



   /// <summary>A hidden sidebar relinquishes its width entirely to the reader.</summary>
   private void ApplyOutlineVisibility()
   {
      OutlinePanel.Visibility = _settings.ShowOutline ? Visibility.Visible : Visibility.Collapsed;
      ApplyReadingWidths();
   }



   /// <summary>The view menu saves a persistent sidebar choice.</summary>
   private void Outline_Click(object sender, RoutedEventArgs e)
   {
      _settings.ShowOutline = OutlineToggle.IsChecked;
      Save();
      ApplyOutlineVisibility();
   }



   /// <summary>Depth selection filters headings without renumbering their document identities.</summary>
   private void OutlineDepth_Changed(object sender, SelectionChangedEventArgs e)
   {
      if(!_navigationReady || OutlineDepth.SelectedIndex < 0)
      {
         return;
      }
      _settings.OutlineDepth = OutlineDepth.SelectedIndex + 1;
      Save();
      RefreshOutline();
   }



   /// <summary>Rebuilds native labels only after a complete sanitized snapshot has been committed.</summary>
   private void RefreshOutline()
   {
      var selected = (OutlineList.SelectedItem as ListViewItem)?.Tag as Core.Documents.DocumentHeading;
      _refreshingOutline = true;
      OutlineList.Items.Clear();
      ToggleSection.IsEnabled = false;
      if(_outline is not null)
      {
         foreach(Core.Documents.DocumentHeading heading in _outline.Headings.Where(heading => heading.Level <= _settings.OutlineDepth))
         {
            var label = new TextBlock { Tag = heading, Text = heading.Title, TextWrapping = TextWrapping.Wrap, MaxLines = 2, Margin = new Thickness((heading.Level - 1) * 12, 0, 0, 0) };
            var item = new ListViewItem { Content = label, Tag = heading };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(item, "OutlineHeading:" + heading.Index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            OutlineList.Items.Add(item);
            if(heading == selected)
            {
               OutlineList.SelectedItem = item;
               ToggleSection.IsEnabled = true;
            }
         }
      }
      _refreshingOutline = false;
   }



   /// <summary>Heading jumps reveal collapsed ancestors and target only the selected numeric identity.</summary>
   private async void OutlineSelection_Changed(object sender, SelectionChangedEventArgs e)
   {
      if(_refreshingOutline || OutlineList.SelectedItem is not ListViewItem { Tag: Core.Documents.DocumentHeading heading })
      {
         return;
      }
      ToggleSection.IsEnabled = true;
      await _browser.JumpToHeadingAsync(heading.Index);
   }



   /// <summary>Activating the same heading again returns to it after scrolling; selection changes still support keyboard navigation.</summary>
   private async void Outline_ItemClick(object sender, ItemClickEventArgs args)
   {
      if(string.Equals(Environment.GetEnvironmentVariable("MARKDOWNVIEWR_INPUT_TRACE"), "1", StringComparison.Ordinal))
      {
         int index = (args.ClickedItem as FrameworkElement)?.Tag is Core.Documents.DocumentHeading clicked ? clicked.Index : -1;
         App.Log.Info($"[DEBUG-outline-activation] clicked type={args.ClickedItem?.GetType().Name}; refreshing={_refreshingOutline}; index={index}.");
      }
      if(!_refreshingOutline && args.ClickedItem is FrameworkElement { Tag: Core.Documents.DocumentHeading heading })
      {
         await _browser.JumpToHeadingAsync(heading.Index);
      }
   }



   /// <summary>The selected heading remains visible while its descendants toggle together.</summary>
   private async void ToggleSection_Click(object sender, RoutedEventArgs e)
   {
      if(OutlineList.SelectedItem is ListViewItem { Tag: Core.Documents.DocumentHeading heading })
      {
         await _browser.ToggleSectionAsync(heading.Index);
      }
   }



   /// <summary>Collapses all sections while retaining their headings.</summary>
   private async void CollapseSections_Click(object sender, RoutedEventArgs e) => await _browser.SetSectionsAsync(collapsed: true);



   /// <summary>Expands all levels, including children hidden before their parent was collapsed.</summary>
   private async void ExpandSections_Click(object sender, RoutedEventArgs e) => await _browser.SetSectionsAsync(collapsed: false);



   /// <summary>Resizes within the validated reading range while the pointer drags the native divider.</summary>
   private void OutlineDivider_DragDelta(object sender, DragDeltaEventArgs e)
   {
      _settings.OutlineWidth = Math.Clamp(OutlineColumn.ActualWidth + e.HorizontalChange, 140, 500);
      ApplyReadingWidths();
   }



   /// <summary>Writes only the completed resize to AppKit's atomic preferences.</summary>
   private void OutlineDivider_DragCompleted(object sender, DragCompletedEventArgs e) => Save();
}
