/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;


namespace MarkdownViewr.Windows.Core;

/// <summary>
/// The settings file shared by every window and every process of one edition. A change is
/// applied to the latest stored settings under a cross-process lock and renamed into place in
/// one step, so two instances cannot erase each other's change and the file is never absent.
/// A file that cannot be read, or that another program is writing, is never treated as empty;
/// content that is invalid while nobody is writing it is repaired where possible and a copy of
/// the original is kept aside. ADR-0006 describes the files involved.
/// </summary>
public sealed class AppSettingsStore
{
   /// <summary>Attempts for the lock, a read, and the final rename before the operation gives up.</summary>
   public const int MaximumAttempts = 20;

   /// <summary>A larger file is not this application's settings and is never loaded into memory.</summary>
   public const int MaximumBytes = 16 * 1024 * 1024;

   /// <summary>Copies of invalid files kept aside before the last name is reused.</summary>
   public const int MaximumKeptAside = 9;

   /// <summary>The pause between attempts while another handle is in the way.</summary>
   public static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

   private const int SharingViolation = unchecked((int)0x80070020);
   private static readonly JsonDocumentOptions s_lenient = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
   private readonly Action<SettingsFault, Exception?> _report;
   private readonly Action<TimeSpan> _wait;
   private readonly Func<string, string, bool> _renameOver;



   /// <summary>Creates the store; faults go to <paramref name="report"/>, and tests supply <paramref name="wait"/> to count attempts instead of sleeping.</summary>
   public AppSettingsStore(string path, Action<SettingsFault, Exception?>? report = null, Action<TimeSpan>? wait = null)
      : this(path, report, wait, FileSwap.TryRenameOver)
   {
   }



   /// <summary>Creates the store over a given rename, so tests can stand in for a volume that cannot rename over an open file.</summary>
   internal AppSettingsStore(string path, Action<SettingsFault, Exception?>? report, Action<TimeSpan>? wait, Func<string, string, bool> renameOver)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(path);
      Path = System.IO.Path.GetFullPath(path);
      _report = report ?? ((_, _) => { });
      _wait = wait ?? Thread.Sleep;
      _renameOver = renameOver;
   }



   /// <summary>The settings file, as an absolute path.</summary>
   public string Path { get; }



   /// <summary>
   /// Reads the stored settings for display. A missing, empty, invalid or unreadable file yields
   /// defaults and is left untouched. A handle that another program merely holds with write
   /// access does not delay the read.
   /// </summary>
   public AppSettings Load()
   {
      _ = TryRead(forUpdate: false, out AppSettings settings, out _);
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
      FileStream? stored = null;
      try
      {
         Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
         using FileStream? gate = Acquire();
         if(gate is null)
         {
            return null;
         }
         // The read handle denies writers until the rename, so the content judged here is the content replaced.
         SettingsRead read = TryRead(forUpdate: true, out AppSettings current, out stored);
         if(read == SettingsRead.Unreadable)
         {
            return null;
         }
         AppSettings edited = change(current);
         edited.Sanitize();
         RemoveAbandonedTemporaryFiles();
         string? aside = read is SettingsRead.Corrupt or SettingsRead.Repaired ? KeepAside(stored!) : null;
         Replace(JsonSerializer.SerializeToUtf8Bytes(edited, AppSettingsContext.Default.AppSettings), stored, aside);
         return edited;
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
      {
         _report(SettingsFault.WriteFailed, exception);
         return null;
      }
      finally
      {
         stored?.Dispose();
      }
   }



   /// <summary>
   /// Removes the copies of invalid files that saves have kept aside. They hold whatever the
   /// file held, including the recent-document history, so clearing that history removes them.
   /// </summary>
   public void DeleteKeptAside()
   {
      for(int slot = 1; slot <= MaximumKeptAside; slot++)
      {
         TryDelete(KeptAsideName(slot));
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
   /// Reads the file, waiting out other handles a bounded number of times, and reports what it
   /// found. For an update the open handle is returned so writers stay out until the rename.
   /// </summary>
   private SettingsRead TryRead(bool forUpdate, out AppSettings settings, out FileStream? stored)
   {
      settings = new AppSettings();
      stored = null;
      SettingsRead? read = null;
      for(int attempt = 1; read is null; attempt++)
      {
         if(attempt > 1)
         {
            _wait(RetryDelay);
         }
         read = Attempt(forUpdate, attempt == MaximumAttempts, ref settings, ref stored);
      }
      if(read is SettingsRead.Corrupt or SettingsRead.Repaired)
      {
         _report(read == SettingsRead.Corrupt ? SettingsFault.Corrupt : SettingsFault.Repaired, null);
      }
      settings.Sanitize();
      return read.Value;
   }



   /// <summary>
   /// One read. Returns null to try again: another handle is in the way, the file is empty while
   /// a save is waiting for whoever cleared it, or the content read beside a writer did not parse.
   /// On the last attempt it always answers.
   /// </summary>
   private SettingsRead? Attempt(bool forUpdate, bool last, ref AppSettings settings, ref FileStream? stored)
   {
      FileStream? stream = null;
      bool keep = false;
      try
      {
         stream = Open(forUpdate, out bool writersExcluded);
         SettingsRead read = Parse(stream, ref settings);
         if(read == SettingsRead.Empty && forUpdate && !last)
         {
            // A program that clears the file before rewriting it is about to write.
            return null;
         }
         if(read == SettingsRead.Corrupt && !writersExcluded)
         {
            // Content read while another program can write may simply be half written.
            return last ? Unreadable(null) : null;
         }
         keep = forUpdate;
         stored = keep ? stream : null;
         return read;
      }
      catch(Exception exception) when(exception is FileNotFoundException or DirectoryNotFoundException)
      {
         return SettingsRead.Missing;
      }
      catch(IOException exception) when(IsTransient(exception) && !last)
      {
         return null;
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
      {
         return Unreadable(exception);
      }
      finally
      {
         if(!keep)
         {
            stream?.Dispose();
         }
      }
   }



   /// <summary>Reports a file whose content could not be established and answers that it must not be replaced.</summary>
   private SettingsRead Unreadable(Exception? exception)
   {
      _report(SettingsFault.Unreadable, exception);
      return SettingsRead.Unreadable;
   }



   /// <summary>
   /// Opens the file denying writers, so a program rewriting it in place reads as busy rather
   /// than as invalid content. A read for display falls back to reading beside a handle that has
   /// write access, because such a handle is usually idle; the caller then knows the content may
   /// be half written.
   /// </summary>
   private FileStream Open(bool forUpdate, out bool writersExcluded)
   {
      writersExcluded = true;
      try
      {
         return new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
      }
      catch(IOException exception) when(IsTransient(exception) && !forUpdate)
      {
         writersExcluded = false;
         return new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
      }
   }



   /// <summary>Reads at most <see cref="MaximumBytes"/> and classifies the content; an invalid property costs that property, never the whole profile.</summary>
   private static SettingsRead Parse(FileStream stream, ref AppSettings settings)
   {
      if(stream.Length == 0)
      {
         return SettingsRead.Empty;
      }
      if(stream.Length > MaximumBytes)
      {
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
      catch(Exception exception) when(IsInvalidContent(exception))
      {
         if(Salvage(json) is { } repaired)
         {
            settings = repaired;
            return SettingsRead.Repaired;
         }
      }
      return SettingsRead.Corrupt;
   }



   /// <summary>Keeps every stored property that is valid on its own and drops the ones that are not; null when the file is not a JSON object at all.</summary>
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
            catch(Exception exception) when(IsInvalidContent(exception))
            {
               stored.Remove(name);
            }
         }
         return stored.Deserialize(AppSettingsContext.Default.AppSettings);
      }
      catch(Exception exception) when(IsInvalidContent(exception) || exception is ArgumentException)
      {
         // Not JSON, or an object that repeats a property name.
         return null;
      }
   }



   /// <summary>
   /// What the JSON reader throws for content that is not valid settings: a syntax or type error,
   /// or text it cannot decode, such as an unpaired surrogate escape.
   /// </summary>
   private static bool IsInvalidContent(Exception exception) => exception is JsonException or InvalidOperationException;



   /// <summary>
   /// Copies an invalid file to the first free kept-aside name before it is replaced, or over the
   /// last name once all are taken, and returns the copy's path. The copy is a courtesy: failing
   /// to make it is reported and does not stop the save, since refusing would block every later
   /// change as well; null is returned then.
   /// </summary>
   private string? KeepAside(FileStream stored)
   {
      int slot = 1;
      while(slot < MaximumKeptAside && (File.Exists(KeptAsideName(slot)) || Directory.Exists(KeptAsideName(slot))))
      {
         slot++;
      }
      try
      {
         using var copy = new FileStream(KeptAsideName(slot), FileMode.Create, FileAccess.Write, FileShare.None);
         stored.Position = 0;
         stored.CopyTo(copy);
         copy.Flush(flushToDisk: true);
         return KeptAsideName(slot);
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
      {
         _report(SettingsFault.NotKeptAside, exception);
         return null;
      }
   }



   /// <summary>The kept-aside names: <c>.corrupt</c>, then <c>.corrupt-2</c> to <c>.corrupt-9</c>.</summary>
   private string KeptAsideName(int slot) => slot == 1 ? Path + ".corrupt" : Path + ".corrupt-" + slot.ToString(CultureInfo.InvariantCulture);



   /// <summary>
   /// Writes beside the file and flushes to disk, then renames over it in one step. A failure
   /// leaves the stored file as it was, and neither the temporary file nor the copy just kept
   /// aside behind.
   /// </summary>
   private void Replace(byte[] content, FileStream? stored, string? aside)
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
         if(stored is null)
         {
            // No overwrite: a file that appeared since the read is not this instance's to replace.
            File.Move(temporary, Path);
         }
         else
         {
            bool renamed = false;
            Retry(() => renamed = _renameOver(temporary, Path));
            if(!renamed)
            {
               // This volume cannot rename over an open file, so the file is closed first.
               stored.Dispose();
               Retry(() => File.Move(temporary, Path, overwrite: true));
            }
         }
         replaced = true;
      }
      finally
      {
         if(!replaced)
         {
            TryDelete(temporary);
            if(aside is not null)
            {
               TryDelete(aside);
            }
         }
      }
   }



   /// <summary>A writer that died before its rename left a temporary file with this store's own name pattern; under the lock every such file is abandoned.</summary>
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



   /// <summary>A sharing violation: another handle on the file that is expected to go away. Access denied is not transient.</summary>
   private static bool IsTransient(IOException exception) => exception.HResult == SharingViolation;



   /// <summary>Removes a file if it is there and can be removed; a folder with that name, or a file in use, is left.</summary>
   private static void TryDelete(string path)
   {
      try
      {
         File.Delete(path);
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
      {
         // Cleanup only: the operation this was part of has already succeeded or failed.
      }
   }
}
