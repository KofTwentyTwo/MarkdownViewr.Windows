/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;


namespace MarkdownViewr.Windows.Core;

/// <summary>
/// The settings file shared by every window and every process of one edition. A change is
/// applied to the latest stored settings under a cross-process lock and swapped in atomically,
/// so two instances cannot erase each other's change. A file that cannot be read, or that
/// another program is writing, is never treated as empty; content that is invalid while
/// nobody is writing it is repaired where possible and the original is kept aside.
/// See ADR-0006 for the on-disk protocol.
/// </summary>
public sealed class AppSettingsStore
{
   /// <summary>Attempts for the lock, a read, and the final swap before the operation gives up.</summary>
   public const int MaximumAttempts = 20;

   /// <summary>A larger file is not this application's settings and is never loaded into memory.</summary>
   public const int MaximumBytes = 16 * 1024 * 1024;

   /// <summary>Invalid files kept aside before the newest slot is reused.</summary>
   public const int MaximumKeptAside = 9;

   /// <summary>The pause between attempts while another handle is in the way.</summary>
   public static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

   private const int SharingViolation = unchecked((int)0x80070020);
   private const int LockViolation = unchecked((int)0x80070021);
   private static readonly JsonDocumentOptions s_lenient = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
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



   /// <summary>The settings file, as an absolute path.</summary>
   public string Path { get; }



   /// <summary>Reads the stored settings for display. A missing, invalid, or unreadable file yields defaults and is left untouched.</summary>
   public AppSettings Load()
   {
      _ = TryRead(out AppSettings settings, out FileStream? stored);
      stored?.Dispose();
      return settings;
   }



   /// <summary>
   /// Applies one change to the latest stored settings and saves the result. Returns the saved
   /// settings, or null when nothing was written: the lock stayed busy, the file could not be
   /// read or was being written by another program, or the write failed. The stored file is
   /// unchanged in every null case.
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
         SettingsRead read = TryRead(out AppSettings current, out FileStream? stored);
         // The read handle denies writers until the swap, so the content judged here is the content replaced.
         using(stored)
         {
            if(read == SettingsRead.Unreadable)
            {
               return null;
            }
            AppSettings edited = change(current);
            edited.Sanitize();
            RemoveAbandonedTemporaryFiles();
            Replace(JsonSerializer.SerializeToUtf8Bytes(edited, AppSettingsContext.Default.AppSettings), stored is not null, read is SettingsRead.Corrupt or SettingsRead.Repaired);
            return edited;
         }
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
      {
         _report(SettingsFault.WriteFailed, exception);
         return null;
      }
   }



   /// <summary>
   /// Holds an exclusive handle beside the settings file; the operating system releases it if the
   /// process dies. A lock that stays held, or cannot be opened for another I/O reason, is busy.
   /// </summary>
   private FileStream? Acquire()
   {
      try
      {
         FileStream? gate = null;
         Retry(() => gate = new FileStream(Path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
         return gate;
      }
      catch(IOException exception)
      {
         _report(SettingsFault.Busy, exception);
         return null;
      }
   }



   /// <summary>
   /// Opens the file denying writers, so a program caught rewriting it in place reads as busy
   /// rather than as invalid content, then distinguishes missing, valid, repaired and invalid.
   /// The open handle is returned so the caller can keep writers out until it has finished.
   /// </summary>
   private SettingsRead TryRead(out AppSettings settings, out FileStream? stored)
   {
      settings = new AppSettings();
      stored = null;
      SettingsRead result;
      try
      {
         FileStream? stream = null;
         Retry(() => stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete));
         stored = stream;
         result = Parse(stream!, ref settings);
      }
      catch(Exception exception) when(exception is FileNotFoundException or DirectoryNotFoundException)
      {
         result = SettingsRead.Missing;
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
      {
         _report(SettingsFault.Unreadable, exception);
         result = SettingsRead.Unreadable;
      }
      settings.Sanitize();
      return result;
   }



   /// <summary>Reads at most <see cref="MaximumBytes"/>; one invalid value costs only that value, never the whole profile.</summary>
   private SettingsRead Parse(FileStream stream, ref AppSettings settings)
   {
      if(stream.Length > MaximumBytes)
      {
         _report(SettingsFault.Corrupt, null);
         return SettingsRead.Corrupt;
      }
      using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
      string json = reader.ReadToEnd();
      try
      {
         if(JsonSerializer.Deserialize(json, AppSettingsContext.Default.AppSettings) is { } loaded)
         {
            settings = loaded;
            return SettingsRead.Loaded;
         }
      }
      catch(JsonException)
      {
         if(Salvage(json) is { } repaired)
         {
            settings = repaired;
            _report(SettingsFault.Repaired, null);
            return SettingsRead.Repaired;
         }
      }
      _report(SettingsFault.Corrupt, null);
      return SettingsRead.Corrupt;
   }



   /// <summary>Keeps every stored value that is valid on its own and drops the ones that are not; null when the file is not a JSON object at all.</summary>
   private static AppSettings? Salvage(string json)
   {
      try
      {
         if(JsonNode.Parse(json, documentOptions: s_lenient) is not JsonObject stored)
         {
            return null;
         }
         foreach(string name in stored.Select(property => property.Key).ToArray())
         {
            try
            {
               _ = new JsonObject { [name] = stored[name]?.DeepClone() }.Deserialize(AppSettingsContext.Default.AppSettings);
            }
            catch(JsonException)
            {
               stored.Remove(name);
            }
         }
         return stored.Deserialize(AppSettingsContext.Default.AppSettings);
      }
      catch(Exception exception) when(exception is JsonException or ArgumentException)
      {
         // Not JSON, or an object that repeats a property name.
         return null;
      }
   }



   /// <summary>
   /// Writes beside the file and flushes to disk, then swaps it in. A failure leaves the stored
   /// file and no temporary file. Invalid content being replaced is kept under a free name.
   /// </summary>
   private void Replace(byte[] content, bool exists, bool keepAside)
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
         if(exists)
         {
            string? aside = keepAside ? KeptAsideName() : null;
            Retry(() => File.Replace(temporary, Path, aside, ignoreMetadataErrors: true));
         }
         else
         {
            // No overwrite: a file that appeared since the read is not this instance's to replace.
            File.Move(temporary, Path);
         }
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



   /// <summary>The first free name for an invalid file; once every slot is used the last slot is reused.</summary>
   private string KeptAsideName()
   {
      string candidate = Path + ".corrupt";
      for(int slot = 2; slot <= MaximumKeptAside && File.Exists(candidate); slot++)
      {
         candidate = Path + ".corrupt-" + slot.ToString(System.Globalization.CultureInfo.InvariantCulture);
      }
      return candidate;
   }



   /// <summary>A writer that died before its swap left a temporary file with this store's own name pattern; under the lock every such file is abandoned.</summary>
   private void RemoveAbandonedTemporaryFiles()
   {
      string prefix = System.IO.Path.GetFileName(Path) + ".";
      foreach(string file in Directory.EnumerateFiles(System.IO.Path.GetDirectoryName(Path)!, prefix + "*.tmp"))
      {
         string name = System.IO.Path.GetFileName(file);
         if(name.Length == prefix.Length + 36 && Guid.TryParseExact(name.AsSpan(prefix.Length, 32), "N", out _))
         {
            TryDelete(file);
         }
      }
   }



   /// <summary>Repeats an operation only while another handle is in the way, a bounded number of times; the last failure is thrown.</summary>
   private void Retry(Action operation)
   {
      for(int attempt = 1; ; attempt++)
      {
         try
         {
            operation();
            return;
         }
         catch(IOException exception) when(attempt < MaximumAttempts && IsTransient(exception))
         {
            _wait(RetryDelay);
         }
      }
   }



   /// <summary>A sharing or byte-range lock violation: another handle on the file that is expected to go away. Access denied is not transient.</summary>
   private static bool IsTransient(IOException exception) => exception.HResult is SharingViolation or LockViolation;



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
