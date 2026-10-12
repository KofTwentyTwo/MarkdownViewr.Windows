/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Printing;


namespace MarkdownViewr.Windows;

/// <summary>Printing defaults are shared without changing each document's reading theme, zoom or viewport.</summary>
public partial class App
{
   /// <summary>Checked Settings/output saves update all readers and prevent stale unrelated saves from restoring older defaults.</summary>
   internal void ApplyPrintingSettings(PrintOptions options)
   {
      foreach(MainWindow window in _windows.ToArray())
      {
         window.ApplyPrintingSettings(options);
      }
   }
}
