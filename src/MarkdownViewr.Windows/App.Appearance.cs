/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows;

/// <summary>Reader-wide native appearance changes reach each open document on the existing UI thread.</summary>
public partial class App
{
   private ulong _appearanceRevision;



   /// <summary>A newer native save supersedes an older asynchronous broadcast without reopening or replacing any document.</summary>
   internal async Task ApplyReadingAppearanceAsync(Core.Themes.ReadingAppearance appearance)
   {
      ulong revision = ++_appearanceRevision;
      foreach(MainWindow window in _windows.ToArray())
      {
         if(revision != _appearanceRevision)
         {
            return;
         }
         await window.ApplyReadingAppearanceAsync(appearance);
      }
   }
}
