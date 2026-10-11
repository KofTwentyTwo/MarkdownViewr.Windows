/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows;

/// <summary>Native theme preference changes update every open reader on the existing UI thread.</summary>
public partial class App
{
   private ulong _themeRevision;



   /// <summary>Newer theme saves supersede older asynchronous broadcasts.</summary>
   internal async Task ApplyThemeSettingsAsync(Core.AppSettings settings)
   {
      ulong revision = ++_themeRevision;
      foreach(MainWindow window in _windows.ToArray())
      {
         if(revision != _themeRevision)
         {
            return;
         }
         await window.ApplyThemeSettingsAsync(settings);
      }
   }
}
