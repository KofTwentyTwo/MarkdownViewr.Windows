/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;


namespace MarkdownViewr.Windows;

/// <summary>Commits copied theme changes to the latest store and refreshes owned readers.</summary>
public sealed partial class MainWindow
{
   private ulong _themeSettingsRevision;



   /// <summary>The native manager edits a snapshot; cancellation cannot affect another reader or persisted state.</summary>
   private async Task ManageDocumentThemesAsync()
   {
      Core.AppSettings before = Core.SettingsChanges.Copy(_settings);
      Core.AppSettings? edited = await new ThemeManagementDialog(before, ConfigurationWindowId).ShowAsync(ConfigurationPage, () => !_closed);
      if(edited is not null && !_closed)
      {
         await CommitThemeDraftAsync(before, edited);
      }
   }



   /// <summary>Native menu cycling follows enabled saved order.</summary>
   private async void NextDocumentTheme_Click(object sender, RoutedEventArgs e) => await CycleDocumentThemeAsync(forward: true);



   /// <summary>Previous cycles backward with wraparound.</summary>
   private async void PreviousDocumentTheme_Click(object sender, RoutedEventArgs e) => await CycleDocumentThemeAsync(forward: false);



   /// <summary>An intentionally empty enabled list has no cycle action and retains the safe current surface.</summary>
   private async Task CycleDocumentThemeAsync(bool forward)
   {
      Core.AppSettings before = Core.SettingsChanges.Copy(_settings);
      Core.Themes.ReaderTheme? next = Core.Themes.ThemeManagement.Cycle(before, forward);
      if(next is not null)
      {
         Core.AppSettings draft = Core.SettingsChanges.Copy(before);
         draft.DocumentTheme = next.Name;
         await CommitThemeDraftAsync(before, draft);
      }
   }



   /// <summary>Failed persistence does not apply an unsaved theme draft; sibling fields untouched by this edit retain their latest values.</summary>
   private async Task<bool> CommitThemeDraftAsync(Core.AppSettings before, Core.AppSettings edited)
   {
      Core.AppSettings merged = Core.SettingsChanges.Merge(AppIdentity.SettingsStore.Load(), before, edited);
      if(!AppIdentity.SettingsStore.Save(merged))
      {
         StatusText.Text = "Could not save document themes.";
         return false;
      }
      await ((App)Application.Current).ApplyThemeSettingsAsync(merged);
      return true;
   }



   /// <summary>Shared palettes and their menu state update each window's save baseline before asynchronous rendering.</summary>
   internal async Task ApplyThemeSettingsAsync(Core.AppSettings settings)
   {
      if(_closed)
      {
         return;
      }
      ulong revision = ++_themeSettingsRevision;
      _settings.DocumentTheme = _lastSavedSettings.DocumentTheme = settings.DocumentTheme;
      _settings.UserThemes = settings.UserThemes.ToArray();
      _lastSavedSettings.UserThemes = settings.UserThemes.ToArray();
      _settings.ThemeOrder = settings.ThemeOrder.ToArray();
      _lastSavedSettings.ThemeOrder = settings.ThemeOrder.ToArray();
      _settings.DisabledThemes = settings.DisabledThemes.ToArray();
      _lastSavedSettings.DisabledThemes = settings.DisabledThemes.ToArray();
      _settings.DeletedBuiltInThemes = settings.DeletedBuiltInThemes.ToArray();
      _lastSavedSettings.DeletedBuiltInThemes = settings.DeletedBuiltInThemes.ToArray();
      RefreshDocumentThemes();
      try
      {
         _browserInitialization ??= _browser.InitializeAsync();
         await _browserInitialization;
         if(!_closed && revision == _themeSettingsRevision)
         {
            await ApplyDocumentThemeAsync(ActiveDocumentTheme());
         }
      }
      catch(System.Runtime.InteropServices.COMException exception)
      {
         if(!_closed)
         {
            StatusText.Text = "The document themes could not be displayed.";
            App.Log.Error("The document themes could not be displayed.", exception);
         }
      }
   }
}
