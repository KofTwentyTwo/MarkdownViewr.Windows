/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>Native Advanced controls expose the same splash toggle, bounds and duration steps as gclo.</summary>
internal sealed partial class SettingsWindow
{
   private readonly ToggleSwitch _showSplash = new() { Header = "Show splash screen at startup" };
   private readonly NumberBox _splashDuration = new() { Header = "Splash duration (milliseconds)", Minimum = ShellSettings.MinSplashMilliseconds, Maximum = ShellSettings.MaxSplashMilliseconds, SmallChange = 250, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline, HorizontalAlignment = HorizontalAlignment.Left, Width = 280 };
   private readonly ToggleSwitch _checkStartupUpdates = new() { Header = "Check for updates at startup" };
   private readonly ToggleSwitch _showOutlineByDefault = new() { Header = "Show Contents in new windows" };
   private readonly ToggleSwitch _showSourceByDefault = new() { Header = "Show Source in new windows" };
   private readonly Func<bool, int, bool, bool, bool, bool> _saveStartup;



   /// <summary>Initial launch owns the shared branded AppKit overlay; later document windows open directly into their reader.</summary>
   private StackPanel AdvancedPage(Core.AppSettings current)
   {
      var page = new StackPanel { Spacing = 16, MaxWidth = 720 };
      page.Children.Add(new TextBlock { Text = "Startup", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
      page.Children.Add(new TextBlock { Text = "Changes apply the next time you launch MarkdownViewr. The splash fades away automatically while your document loads.", TextWrapping = TextWrapping.Wrap });
      AutomationProperties.SetAutomationId(_showSplash, "SettingsSplashToggle");
      AutomationProperties.SetAutomationId(_splashDuration, "SettingsSplashDurationBox");
      AutomationProperties.SetAutomationId(_checkStartupUpdates, "SettingsStartupUpdatesToggle");
      ToolTipService.SetToolTip(_splashDuration, "Time before the splash fades: 500–30000 milliseconds, in 250 millisecond steps.");
      UpdateStartup(current);
      _showSplash.Toggled += (_, _) => SaveStartup();
      _splashDuration.ValueChanged += (_, _) => SaveStartup();
      _checkStartupUpdates.Toggled += (_, _) => SaveStartup();
      page.Children.Add(_showSplash);
      page.Children.Add(_splashDuration);
      page.Children.Add(_checkStartupUpdates);
      return page;
   }



   /// <summary>Saved sibling preferences refresh without generating a new save or altering an unsaved CSS draft.</summary>
   internal void UpdateStartup(Core.AppSettings settings)
   {
      if(_closed)
      {
         return;
      }
      bool previous = _updating;
      _updating = true;
      try
      {
         _showSplash.IsOn = settings.ShowSplashScreen;
         _splashDuration.Value = settings.SplashMilliseconds;
         _splashDuration.IsEnabled = settings.ShowSplashScreen;
         _checkStartupUpdates.IsOn = settings.CheckForUpdatesOnStartup;
         _showOutlineByDefault.IsOn = settings.ShowOutlineByDefault;
         _showSourceByDefault.IsOn = settings.ShowSourceByDefault;
      }
      finally
      {
         _updating = previous;
      }
   }



   /// <summary>Routine startup choices save immediately; invalid numeric input never reaches persisted preferences.</summary>
   private void SaveStartup()
   {
      if(_closed || _updating)
      {
         return;
      }
      double duration = _splashDuration.Value;
      if(!double.IsFinite(duration))
      {
         _status.Text = "Enter a splash duration between 500 and 30000 milliseconds.";
         return;
      }
      _splashDuration.IsEnabled = _showSplash.IsOn;
      int milliseconds = (int)Math.Round(Math.Clamp(duration, ShellSettings.MinSplashMilliseconds, ShellSettings.MaxSplashMilliseconds));
      try
      {
         _status.Text = _saveStartup(_showSplash.IsOn, milliseconds, _checkStartupUpdates.IsOn, _showOutlineByDefault.IsOn, _showSourceByDefault.IsOn) ? "Saved. Applies to new windows and the next launch." : "Could not save the setting. Please try again.";
      }
      catch(Exception exception)
      {
         ReportError(exception);
      }
   }
}
