/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.WinUI;


namespace MarkdownViewr.Windows;

/// <summary>Tracks native document windows on their shared UI thread and stores private successful-open history.</summary>
public partial class App
{
   private readonly List<MainWindow> _windows = [];



   /// <summary>Creates an independent XAML window, retaining it until its native Closed event.</summary>
   internal void NewDocumentWindow(string? path = null)
   {
      if(path is not null)
      {
         try
         {
            path = Path.GetFullPath(path);
         }
         catch(Exception exception) when(exception is ArgumentException or NotSupportedException or IOException)
         {
            Log.Warning("An invalid document launch path will be reported in its reader window.");
         }
         MainWindow? existing = _windows.FirstOrDefault(window => string.Equals(window.DocumentPath, path, StringComparison.OrdinalIgnoreCase));
         if(existing is not null)
         {
            existing.Activate();
            return;
         }
      }
      var window = new MainWindow(path, initialWindow: _windows.Count == 0);
      _windows.Add(window);
      window.Closed += (_, _) =>
      {
         _windows.Remove(window);
         if(_windows.Count == 0)
         {
            LogWindow.CloseCurrent();
         }
      };
      window.Activate();
   }



   /// <summary>Focuses an already-open path, fills a blank window or opens a separate document without replacing a reader.</summary>
   internal async Task OpenDocumentAsync(MainWindow requester, string path)
   {
      string canonical;
      try
      {
         canonical = Path.GetFullPath(path);
      }
      catch(Exception exception) when(exception is ArgumentException or NotSupportedException or IOException)
      {
         await requester.OpenDocumentAsync(path);
         return;
      }
      MainWindow? existing = _windows.FirstOrDefault(window => string.Equals(window.DocumentPath, canonical, StringComparison.OrdinalIgnoreCase));
      if(existing is not null)
      {
         existing.Activate();
      }
      else if(requester.DocumentPath is null)
      {
         await requester.OpenDocumentAsync(canonical);
      }
      else
      {
         NewDocumentWindow(canonical);
      }
   }



   /// <summary>File/Exit closes a snapshot of all document windows while native close/Ctrl+W affect only one.</summary>
   internal void CloseDocumentWindows()
   {
      foreach(MainWindow window in _windows.ToArray())
      {
         window.Close();
      }
   }



   /// <summary>Records a successfully committed path through the latest store snapshot, then refreshes every recent menu.</summary>
   internal bool RecordRecentDocument(string path)
   {
      Core.AppSettings settings = AppIdentity.SettingsStore.Load();
      settings.RecentFiles = Core.Documents.RecentDocuments.Record(settings.RecentFiles, path);
      bool saved = AppIdentity.SettingsStore.Save(settings);
      RefreshRecentMenus();
      return saved;
   }



   /// <summary>Clears only the local history, leaving open documents, reading preferences and files intact.</summary>
   internal bool ClearRecentDocuments()
   {
      Core.AppSettings settings = AppIdentity.SettingsStore.Load();
      settings.RecentFiles = [];
      bool saved = AppIdentity.SettingsStore.Save(settings);
      RefreshRecentMenus();
      return saved;
   }



   /// <summary>All menus read the latest store; no document paths enter the shared activity log.</summary>
   private void RefreshRecentMenus()
   {
      foreach(MainWindow window in _windows)
      {
         window.RefreshRecentDocuments();
      }
   }
}
