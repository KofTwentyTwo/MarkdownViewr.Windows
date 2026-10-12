/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Printing;
using Microsoft.UI.Xaml;


namespace MarkdownViewr.Windows;

/// <summary>Print preferences save through the same checked path from Settings and the deliberate output dialog.</summary>
public sealed partial class MainWindow
{
   /// <summary>Only print defaults change in the newest store, preserving unrelated sibling and reading preferences.</summary>
   private bool SavePrintingSettings(PrintOptions options)
   {
      if(_closed)
      {
         return false;
      }
      Core.AppSettings settings = AppIdentity.SettingsStore.Load();
      settings.Print = options.Sanitize();
      if(!AppIdentity.SettingsStore.Save(settings))
      {
         return false;
      }
      ((App)Application.Current).ApplyPrintingSettings(settings.Print);
      return true;
   }



   /// <summary>Committed shared defaults refresh each reader baseline without altering its live document or pending Settings draft.</summary>
   internal void ApplyPrintingSettings(PrintOptions options)
   {
      if(_closed)
      {
         return;
      }
      _settings.Print = _lastSavedSettings.Print = options;
      _settingsWindow?.UpdatePrinting(options);
   }
}
