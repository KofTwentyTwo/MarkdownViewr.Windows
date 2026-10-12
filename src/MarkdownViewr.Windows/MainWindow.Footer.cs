/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>Snapshot metadata and stale state remain local to each reader; only a deliberate click opens Explorer.</summary>
public sealed partial class MainWindow
{
   private readonly DispatcherTimer _footerPoll = new() { Interval = TimeSpan.FromSeconds(2) };
   private DateTime _loadedModifiedUtc;
   private long _loadedByteLength;



   /// <summary>A light metadata poll covers missed directory notifications without reading or automatically reopening document content.</summary>
   private void InitializeDocumentFooter()
   {
      _footerPoll.Tick += (_, _) => CheckDocumentFooter();
      _footerPoll.Start();
   }



   /// <summary>Successful reads commit timestamp and length together with the displayed document; failed reloads retain this snapshot.</summary>
   private void CommitDocumentFooter(DocumentFiles files)
   {
      _loadedModifiedUtc = files.ModifiedUtc;
      _loadedByteLength = files.ByteLength;
      FileModifiedText.Text = "Modified " + files.ModifiedUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
      ToolTipService.SetToolTip(FileModifiedText, "Displayed version last modified: " + files.ModifiedUtc.ToLocalTime().ToString("F", CultureInfo.CurrentCulture));
      FileModifiedText.Visibility = Visibility.Visible;
      FileStaleButton.Visibility = Visibility.Collapsed;
      OpenDocumentFolderButton.IsEnabled = true;
      CheckDocumentFooter();
   }



   /// <summary>Watcher notifications and failed reloads expose an actionable stale indicator until a successful snapshot commit.</summary>
   private void MarkDocumentStale()
   {
      if(_closed || _documentPath is null)
      {
         return;
      }
      bool exists = File.Exists(_documentPath);
      FileStaleButton.Content = exists ? "Changes detected — reload" : "File unavailable — reload";
      FileStaleButton.Visibility = Visibility.Visible;
      RefreshEditorCommands();
   }



   /// <summary>Metadata changes mark the displayed copy stale; a later metadata match never clears an unsuccessful reload.</summary>
   private void CheckDocumentFooter()
   {
      if(_closed || _documentPath is null)
      {
         return;
      }
      try
      {
         var file = new FileInfo(_documentPath);
         if(!file.Exists || file.LastWriteTimeUtc != _loadedModifiedUtc || file.Length != _loadedByteLength)
         {
            MarkDocumentStale();
         }
         OpenDocumentFolderButton.IsEnabled = Directory.Exists(Path.GetDirectoryName(_documentPath));
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
      {
         MarkDocumentStale();
      }
   }



   /// <summary>Launches a literal containing directory through the Windows folder API, with no shell arguments or authored commands.</summary>
   private async void OpenDocumentFolder_Click(object sender, RoutedEventArgs e)
   {
      if(_closed || _documentPath is null)
      {
         return;
      }
      try
      {
         string directory = Path.GetDirectoryName(_documentPath)!;
         global::Windows.Storage.StorageFolder folder = await global::Windows.Storage.StorageFolder.GetFolderFromPathAsync(directory);
         if(!await global::Windows.System.Launcher.LaunchFolderAsync(folder) && !_closed)
         {
            StatusText.Text = "The document folder could not be opened.";
         }
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or ArgumentException or System.Runtime.InteropServices.COMException)
      {
         if(!_closed)
         {
            StatusText.Text = "The document folder is unavailable.";
         }
         App.Log.Warning(FormattableString.Invariant($"The document folder could not be opened ({exception.GetType().Name}, 0x{exception.HResult:X8})."));
      }
   }
}
