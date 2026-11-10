/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text;
using System.Text.Json;


namespace MarkdownViewr.Windows.Core;

/// <summary>
/// The settings file shared by every window and every process of one edition. A change is
/// applied to the latest stored settings under a cross-process lock and replaces the file
/// atomically, so two instances cannot erase each other's change. A file that cannot be
/// read is never treated as empty: only invalid content is replaced, and it is kept aside.
/// </summary>
public sealed class AppSettingsStore
{
   /// <summary>Attempts for the lock, a read, and the final replace before the operation gives up.</summary>
   public const int MaximumAttempts = 20;

   /// <summary>A larger file is not this application's settings and is never loaded into memory.</summary>
   public const int MaximumBytes = 16 * 1024 * 1024;

   private static readonly TimeSpan s_retryDelay = TimeSpan.FromMilliseconds(50);
   private readonly Action<SettingsFault, Exception?> _report;
   private readonly Action<TimeSpan> _wait;



   /// <summary>Creates the store; faults go to <paramref name="report"/>, and tests supply <paramref name="wait"/> to count attempts instead of sleeping.</summary>
   public AppSettingsStore(string path, Action<SettingsFault, Exception?>? report = null, Action<TimeSpan>? wait = null)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(path);
      Path = System.IO.Path.GetFullPath(path);
      _report = report ?? ((_, _) => { });
      _wait = wait ?? Thread.Sleep;
   }



   /// <summary>The settings file.</summary>
   public string Path { get; }



   /// <summary>Reads the stored settings for display. A missing, invalid, or unreadable file yields defaults and is left untouched.</summary>
   public AppSettings Load()
   {
      _ = TryRead(out AppSettings settings);
      return settings;
   }



   /// <summary>
   /// Applies one change to the latest stored settings and saves the result. Returns the saved
   /// settings, or null when nothing was written: the lock stayed busy, the file could not be
   /// read, or the write failed. The previous file is intact in every null case.
   /// </summary>
   public AppSettings? Update(Func<AppSettings, AppSettings> change)
   {
      ArgumentNullException.ThrowIfNull(change);
      try
      {
         Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
         using FileStream? gate = Acquire();
         if(gate is null)
         {
            return null;
         }
         SettingsRead read = TryRead(out AppSettings current);
         if(read == SettingsRead.Unreadable)
         {
            return null;
         }
         if(read == SettingsRead.Corrupt)
         {
            Retry(() => File.Move(Path, Path + ".corrupt", overwrite: true));
         }
         AppSettings edited = change(current);
         edited.Sanitize();
         RemoveAbandonedTemporaryFiles();
         Replace(JsonSerializer.SerializeToUtf8Bytes(edited, AppSettingsContext.Default.AppSettings));
         return edited;
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
      {
         _report(SettingsFault.WriteFailed, exception);
         return null;
      }
   }



   /// <summary>Holds an exclusive handle beside the settings file; the operating system releases it if the process dies.</summary>
   private FileStream? Acquire()
   {
      try
      {
         FileStream? gate = null;
         Retry(() => gate = new FileStream(Path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
         return gate;
      }
      catch(IOException exception) when(IsTransient(exception))
      {
         _report(SettingsFault.Busy, exception);
         return null;
      }
   }



   /// <summary>Distinguishes a missing file, invalid content, and a file that could not be read; only the last keeps the caller from writing.</summary>
   private SettingsRead TryRead(out AppSettings settings)
   {
      settings = new AppSettings();
      SettingsRead result;
      try
      {
         string? json = null;
         Retry(() => json = ReadText());
         AppSettings? loaded = json is null ? null : JsonSerializer.Deserialize(json, AppSettingsContext.Default.AppSettings);
         if(loaded is null)
         {
            _report(SettingsFault.Corrupt, null);
            result = SettingsRead.Corrupt;
         }
         else
         {
            settings = loaded;
            result = SettingsRead.Loaded;
         }
      }
      catch(Exception exception) when(exception is FileNotFoundException or DirectoryNotFoundException)
      {
         result = SettingsRead.Missing;
      }
      catch(JsonException exception)
      {
         _report(SettingsFault.Corrupt, exception);
         result = SettingsRead.Corrupt;
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
      {
         _report(SettingsFault.Unreadable, exception);
         result = SettingsRead.Unreadable;
      }
      settings.Sanitize();
      return result;
   }



   /// <summary>Reads without blocking a concurrent replace; an oversized file reads as invalid content instead of exhausting memory.</summary>
   private string? ReadText()
   {
      using var stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
      if(stream.Length > MaximumBytes)
      {
         return null;
      }
      using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
      return reader.ReadToEnd();
   }



   /// <summary>Writes beside the file, flushes to disk, then renames over it; a failure leaves the previous file and no temporary file.</summary>
   private void Replace(byte[] content)
   {
      string temporary = Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
      bool replaced = false;
      try
      {
         using(var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
         {
            stream.Write(content);
            stream.Flush(flushToDisk: true);
         }
         Retry(() => File.Move(temporary, Path, overwrite: true));
         replaced = true;
      }
      finally
      {
         if(!replaced)
         {
            TryDelete(temporary);
         }
      }
   }



   /// <summary>A writer that died before its rename left a temporary file; the lock holder is the only writer, so every one found is abandoned.</summary>
   private void RemoveAbandonedTemporaryFiles()
   {
      foreach(string file in Directory.EnumerateFiles(System.IO.Path.GetDirectoryName(Path)!, System.IO.Path.GetFileName(Path) + "*.tmp"))
      {
         TryDelete(file);
      }
   }



   /// <summary>Repeats an operation only for a sharing violation or a pending delete, a bounded number of times; the last failure is thrown.</summary>
   private void Retry(Action operation)
   {
      for(int attempt = 1; ; attempt++)
      {
         try
         {
            operation();
            return;
         }
         catch(Exception exception) when(attempt < MaximumAttempts && IsTransient(exception))
         {
            _wait(s_retryDelay);
         }
      }
   }



   /// <summary>Another handle on the file (an editor, a scanner, a second instance) or a delete that has not completed; a missing file is not transient.</summary>
   private static bool IsTransient(Exception exception)
   {
      if(exception is UnauthorizedAccessException)
      {
         return true;
      }
      return exception is IOException and not (FileNotFoundException or DirectoryNotFoundException) && (exception.HResult & 0xFFFF) is 32 or 33;
   }



   /// <summary>Cleanup only: a temporary file that cannot be removed now is removed by the next successful save.</summary>
   private static void TryDelete(string path)
   {
      try
      {
         File.Delete(path);
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
      {
         // The outcome of the save is already decided.
      }
   }
}
