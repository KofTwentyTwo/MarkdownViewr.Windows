/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows;

/// <summary>Watches the containing directory so replacing or temporarily removing a file does not lose the subscription.</summary>
internal sealed class DocumentWatcher : IDisposable
{
   private readonly Lock _sync = new();
   private readonly string _path;
   private readonly FileSystemWatcher _watcher;
   private readonly Timer _timer;
   private readonly Action _onChange;
   private readonly Action _onError;
   private bool _disposed;



   /// <summary>Subscribes before returning; save bursts schedule one quiet-period notification.</summary>
   public DocumentWatcher(string path, Action onChange, Action onError)
   {
      _path = Path.GetFullPath(path);
      _onChange = onChange;
      _onError = onError;
      _watcher = new FileSystemWatcher(Path.GetDirectoryName(_path)!)
      {
         NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
      };
      _timer = new Timer(Notify, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
      _watcher.Changed += Changed;
      _watcher.Created += Changed;
      _watcher.Deleted += Changed;
      _watcher.Renamed += Renamed;
      _watcher.Error += (_, _) =>
      {
         lock(_sync)
         {
            if(!_disposed)
            {
               _onError();
               Schedule();
            }
         }
      };
      try
      {
         _watcher.EnableRaisingEvents = true;
      }
      catch
      {
         Dispose();
         throw;
      }
   }



   /// <summary>Only the selected path can request a refresh; unrelated files and nested folders are ignored.</summary>
   private void Changed(object sender, FileSystemEventArgs args)
   {
      if(string.Equals(args.FullPath, _path, StringComparison.OrdinalIgnoreCase))
      {
         Schedule();
      }
   }



   /// <summary>Both halves of an atomic replacement matter: the previous file may be moved away or a new file renamed into place.</summary>
   private void Renamed(object sender, RenamedEventArgs args)
   {
      if(string.Equals(args.FullPath, _path, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(args.OldFullPath, _path, StringComparison.OrdinalIgnoreCase))
      {
         Schedule();
      }
   }



   /// <summary>Resets the debounce clock for each event without running document work on a filesystem callback.</summary>
   private void Schedule()
   {
      lock(_sync)
      {
         if(!_disposed)
         {
            _timer.Change(TimeSpan.FromMilliseconds(350), Timeout.InfiniteTimeSpan);
         }
      }
   }



   /// <summary>Invokes the dispatcher callback only while the subscription is alive.</summary>
   private void Notify(object? state)
   {
      lock(_sync)
      {
         if(!_disposed)
         {
            _onChange();
         }
      }
   }



   /// <summary>Stops callbacks and releases directory/timer resources when changing documents or closing a window.</summary>
   public void Dispose()
   {
      lock(_sync)
      {
         _disposed = true;
         _timer.Dispose();
         _watcher.Dispose();
      }
   }
}
