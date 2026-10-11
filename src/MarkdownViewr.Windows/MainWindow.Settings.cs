/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.WinUI;
using Microsoft.UI.Xaml;


namespace MarkdownViewr.Windows;

/// <summary>Settings owns an ordinary native window and commits validated changes using the existing shared save paths.</summary>
public sealed partial class MainWindow
{
   private SettingsWindow? _settingsWindow;



   /// <summary>Configuration dialogs and pickers belong to the live settings window; regular reading dialogs retain their own root.</summary>
   private SettingsWindow ConfigurationPage => _settingsWindow ?? throw new InvalidOperationException("Open Settings before editing configuration.");



   /// <summary>Short confirmation dialogs remain owned by the current Settings root.</summary>
   private XamlRoot ConfigurationRoot => _settingsWindow?.DialogRoot ?? Content.XamlRoot;



   /// <summary>Native file pickers use the settings window's identity instead of a background document window.</summary>
   private Microsoft.UI.WindowId ConfigurationWindowId => _settingsWindow?.AppWindow.Id ?? AppWindow.Id;



   /// <summary>Reuses a modeless settings window instead of hiding/reopening a modal tabbed shell for each action.</summary>
   private void Settings_Click(object sender, RoutedEventArgs e)
   {
      if(_closed)
      {
         return;
      }
      if(_settingsWindow is null)
      {
         _settingsWindow = new SettingsWindow(_settings, SaveReadingAppearanceAsync, SaveStartupSettings, RunSettingsActionAsync);
         WinUIShell.ApplyTheme((FrameworkElement)_settingsWindow.Content, _settings.ThemeKind);
         _settingsWindow.Closed += (_, _) => _settingsWindow = null;
      }
      _settingsWindow.Activate();
   }



   /// <summary>Checked persistence precedes sibling updates; unrelated window and theme settings merge from the latest store.</summary>
   private async Task<bool> SaveReadingAppearanceAsync(Core.Themes.ReadingAppearance appearance)
   {
      if(_closed)
      {
         return false;
      }
      Core.AppSettings before = Core.SettingsChanges.Copy(_settings);
      Core.AppSettings draft = Core.SettingsChanges.Copy(before);
      draft.ContentWidthEnabled = appearance.LimitWidth;
      draft.ContentWidthPx = appearance.WidthPixels;
      draft.GlobalCss = appearance.GlobalCss;
      Core.AppSettings merged = Core.SettingsChanges.Merge(AppIdentity.SettingsStore.Load(), before, draft);
      if(!AppIdentity.SettingsStore.Save(merged))
      {
         return false;
      }
      await ((App)Application.Current).ApplyReadingAppearanceAsync(merged.ReadingAppearance);
      return true;
   }



   /// <summary>Only startup fields change in the latest store; checked persistence precedes reader and Settings broadcasts.</summary>
   private bool SaveStartupSettings(bool showSplash, int milliseconds, bool checkUpdates, bool showOutline, bool showSource)
   {
      if(_closed)
      {
         return false;
      }
      Core.AppSettings settings = AppIdentity.SettingsStore.Load();
      settings.ShowSplashScreen = showSplash;
      settings.SplashMilliseconds = milliseconds;
      settings.CheckForUpdatesOnStartup = checkUpdates;
      settings.ShowOutlineByDefault = showOutline;
      settings.ShowSourceByDefault = showSource;
      settings.Sanitize();
      if(!AppIdentity.SettingsStore.Save(settings))
      {
         return false;
      }
      ((App)Application.Current).ApplyStartupSettings(settings);
      return true;
   }



   /// <summary>Shared saved startup options become the baseline so later reader saves cannot restore older choices.</summary>
   internal void ApplyStartupSettings(Core.AppSettings settings)
   {
      if(_closed)
      {
         return;
      }
      _settings.ShowSplashScreen = _lastSavedSettings.ShowSplashScreen = settings.ShowSplashScreen;
      _settings.SplashMilliseconds = _lastSavedSettings.SplashMilliseconds = settings.SplashMilliseconds;
      _settings.CheckForUpdatesOnStartup = _lastSavedSettings.CheckForUpdatesOnStartup = settings.CheckForUpdatesOnStartup;
      _settings.ShowOutlineByDefault = _lastSavedSettings.ShowOutlineByDefault = settings.ShowOutlineByDefault;
      _settings.ShowSourceByDefault = _lastSavedSettings.ShowSourceByDefault = settings.ShowSourceByDefault;
      _settingsWindow?.UpdateStartup(settings);
   }



   /// <summary>Only fixed native Settings controls launch configuration; document content cannot request these operations.</summary>
   private Task RunSettingsActionAsync(string action) => action switch
   {
      "EditDocumentThemeMenu" => EditDocumentThemeAsync(),
      "ManageDocumentThemesMenu" => ManageDocumentThemesAsync(),
      "ImportDocumentThemesMenu" => ImportThemesAsync(),
      "ExportDocumentThemesMenu" => ExportThemesAsync(),
      "RestoreDocumentThemesMenu" => RestoreDocumentThemesAsync(),
      "ConfigureEditorMenuItem" => ConfigureEditorAsync(),
      _ => Task.CompletedTask,
   };
}
