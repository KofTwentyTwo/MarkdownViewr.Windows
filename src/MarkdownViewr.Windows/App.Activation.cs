/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows;

/// <summary>Deliberate OS/process activation opens independent readers, or focuses an already-open document, on the native UI thread.</summary>
public partial class App
{
   /// <summary>Empty launch focuses an existing reader; canonical duplicate paths preserve its watcher, find, source and zoom state.</summary>
   private void OpenActivation(ActivationRequest request)
   {
      if(request.Paths.Length == 0)
      {
         if(_windows.Count == 0)
         {
            NewDocumentWindow();
         }
         else
         {
            _windows[^1].Activate();
         }
      }
      else
      {
         foreach(string path in request.Paths)
         {
            NewDocumentWindow(path);
         }
      }
      if(request.HasInvalidPath)
      {
         _windows[^1].ReportActivationFailure();
      }
   }
}
