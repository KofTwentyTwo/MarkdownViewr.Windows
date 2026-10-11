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
      RefreshToolbarReadingState();
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
      RefreshToolbarReadingState();
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
            var label = new TextBlock
            {
               Tag = heading,
               Text = heading.Title,
               TextWrapping = _settings.WrapOutlineEntries ? TextWrapping.Wrap : TextWrapping.NoWrap,
               TextTrimming = _settings.WrapOutlineEntries ? TextTrimming.None : TextTrimming.CharacterEllipsis,
               MaxLines = _settings.WrapOutlineEntries ? 0 : 1,
            };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(label, "OutlineTitle:" + heading.Index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var row = new Grid { Margin = new Thickness((heading.Level - 1) * 12, 0, 0, 0), ColumnSpacing = 6 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            if(_settings.ShowOutlineDepthMarkers)
            {
               var marker = new TextBlock { Text = "●○▪▫–·"[(heading.Level - 1)..heading.Level], VerticalAlignment = VerticalAlignment.Top, Width = 16 };
               Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(marker, "OutlineDepthMarker:" + heading.Index.ToString(System.Globalization.CultureInfo.InvariantCulture));
               Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(marker, "Heading level " + heading.Level.ToString(System.Globalization.CultureInfo.InvariantCulture));
               row.Children.Add(marker);
            }
            Grid.SetColumn(label, 1);
            row.Children.Add(label);
            var item = new ListViewItem { Content = row, Tag = heading, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(item, heading.Title);
            var tooltip = new ToolTip { Content = new TextBlock { Text = heading.Title, TextWrapping = TextWrapping.Wrap, MaxWidth = 320 }, Placement = PlacementMode.Right, PlacementTarget = item, IsHitTestVisible = false };
            label.IsTextTrimmedChanged += (_, _) => ToolTipService.SetToolTip(item, label.IsTextTrimmed ? tooltip : null);
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
