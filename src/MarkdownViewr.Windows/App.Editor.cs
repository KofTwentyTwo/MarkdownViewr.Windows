/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows;

/// <summary>Explicit editor saves reach every open reader on the existing native UI thread.</summary>
public partial class App
{
   /// <summary>Each window receives a separate settings copy; no background callback can mutate another reader's editor array.</summary>
   internal void ApplyEditorSettings(Core.Editors.EditorConfiguration[] choices)
   {
      foreach(MainWindow window in _windows.ToArray())
      {
         window.ApplyEditorSettings(choices);
      }
   }
}
