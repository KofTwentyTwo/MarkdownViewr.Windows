/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows;

/// <summary>Startup preferences are shared by independent reader windows without changing an in-progress launch.</summary>
public partial class App
{
   /// <summary>Updates native Contents and saved baselines only after the shared presentation preferences were persisted.</summary>
   internal void ApplyContentsSettings(Core.AppSettings settings)
   {
      foreach(MainWindow window in _windows.ToArray())
      {
         window.ApplyContentsSettings(settings);
      }
   }



   /// <summary>Updates each native reader and its saved baseline only after startup preferences were persisted successfully.</summary>
   internal void ApplyStartupSettings(Core.AppSettings settings)
   {
      foreach(MainWindow window in _windows.ToArray())
      {
         window.ApplyStartupSettings(settings);
      }
   }
}
