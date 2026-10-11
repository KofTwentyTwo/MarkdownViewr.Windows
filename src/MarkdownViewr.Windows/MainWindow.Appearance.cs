/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */



namespace MarkdownViewr.Windows;

/// <summary>Applies deliberately saved reader-wide appearance independently of theme and print data.</summary>
public sealed partial class MainWindow
{
   /// <summary>Accepts global preferences as the new baseline; later window-local saves cannot resurrect older appearance fields.</summary>
   internal async Task ApplyReadingAppearanceAsync(Core.Themes.ReadingAppearance appearance)
   {
      if(_closed)
      {
         return;
      }
      _settings.ContentWidthEnabled = _lastSavedSettings.ContentWidthEnabled = appearance.LimitWidth;
      _settings.ContentWidthPx = _lastSavedSettings.ContentWidthPx = appearance.WidthPixels;
      _settings.GlobalCss = _lastSavedSettings.GlobalCss = appearance.GlobalCss;
      _settingsWindow?.UpdateAppearance(appearance);
      try
      {
         _browserInitialization ??= _browser.InitializeAsync();
         await _browserInitialization;
         await _browser.ApplyAppearanceAsync(appearance);
      }
      catch(System.Runtime.InteropServices.COMException exception)
      {
         if(!_closed)
         {
            StatusText.Text = "The reader appearance could not be displayed.";
            App.Log.Error("The reader appearance could not be displayed.", exception);
         }
      }
   }
}
