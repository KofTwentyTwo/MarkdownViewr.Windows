/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>General settings expose native Contents presentation and independent new-window defaults.</summary>
internal sealed partial class SettingsWindow
{
   private readonly ToggleSwitch _wrapOutlineEntries = new() { Header = "Wrap long entries in Contents" };
   private readonly ToggleSwitch _showOutlineDepthMarkers = new() { Header = "Show heading-depth markers in Contents" };
   private readonly Func<bool, bool, bool> _saveContents;



   /// <summary>Related controls share one page; presentation updates live while panel defaults apply to new windows.</summary>
   private void AddContentsPreferences(StackPanel page, Core.AppSettings current)
   {
      page.Children.Add(new TextBlock { Text = "New document windows", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
      page.Children.Add(new TextBlock { Text = "Contents and Source start hidden by default. Toolbar changes affect only the current window.", TextWrapping = TextWrapping.Wrap });
      AutomationProperties.SetAutomationId(_showOutlineByDefault, "SettingsDefaultContentsToggle");
      AutomationProperties.SetAutomationId(_showSourceByDefault, "SettingsDefaultSourceToggle");
      _showOutlineByDefault.Toggled += (_, _) => SaveStartup();
      _showSourceByDefault.Toggled += (_, _) => SaveStartup();
      page.Children.Add(_showOutlineByDefault);
      page.Children.Add(_showSourceByDefault);
      page.Children.Add(new TextBlock { Text = "Contents", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
      AutomationProperties.SetAutomationId(_wrapOutlineEntries, "SettingsContentsWrapToggle");
      AutomationProperties.SetAutomationId(_showOutlineDepthMarkers, "SettingsContentsDepthMarkersToggle");
      UpdateContents(current);
      _wrapOutlineEntries.Toggled += (_, _) => SaveContents();
      _showOutlineDepthMarkers.Toggled += (_, _) => SaveContents();
      page.Children.Add(_wrapOutlineEntries);
      page.Children.Add(new TextBlock { Text = "When off, long headings end with an ellipsis. Hover over an entry to see its full title.", TextWrapping = TextWrapping.Wrap, Opacity = 0.7 });
      page.Children.Add(_showOutlineDepthMarkers);
      page.Children.Add(new TextBlock { Text = "A different shape marks each heading level. These changes apply to all open windows.", TextWrapping = TextWrapping.Wrap, Opacity = 0.7 });
   }



   /// <summary>Saved sibling changes refresh the toggles without creating recursive saves.</summary>
   internal void UpdateContents(Core.AppSettings settings)
   {
      if(_closed)
      {
         return;
      }
      bool previous = _updating;
      _updating = true;
      try
      {
         _wrapOutlineEntries.IsOn = settings.WrapOutlineEntries;
         _showOutlineDepthMarkers.IsOn = settings.ShowOutlineDepthMarkers;
      }
      finally
      {
         _updating = previous;
      }
   }



   /// <summary>Persistence precedes visible changes; failure stays visible in the native status area.</summary>
   private void SaveContents()
   {
      if(_closed || _updating)
      {
         return;
      }
      try
      {
         _status.Text = _saveContents(_wrapOutlineEntries.IsOn, _showOutlineDepthMarkers.IsOn) ? "Saved. Applies to all open windows." : "Could not save the setting. Please try again.";
      }
      catch(Exception exception)
      {
         ReportError(exception);
      }
   }
}
