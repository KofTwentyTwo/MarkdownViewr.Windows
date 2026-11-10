/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>
/// The settings file is shared by windows and processes: these tests use real files, real
/// exclusive handles and concurrent store instances, and count attempts instead of time.
/// </summary>
public class AppSettingsStoreTests
{
   private const string KeptPath = @"C:\docs\keep-me.md";



   /// <summary>A first run has no file: defaults apply and reading creates nothing.</summary>
   [Fact]
   public void Load_MissingFile_ReturnsDefaultsWithoutCreatingAnything()
   {
      using var temp = new TempDirectory();
      var faults = new List<SettingsFault>();
      var store = new AppSettingsStore(Path.Combine(temp.Path, "profile", "settings.json"), (fault, _) => faults.Add(fault));
      Assert.Equal("Catppuccin Mocha", store.Load().DocumentTheme);
      Assert.Empty(faults);
      Assert.False(Directory.Exists(Path.Combine(temp.Path, "profile")));
   }



   /// <summary>The first save creates the folder and file, and returns the sanitized settings that were written.</summary>
   [Fact]
   public void Update_MissingFolder_CreatesTheFileAndReturnsSanitizedSettings()
   {
      using var temp = new TempDirectory();
      var store = new AppSettingsStore(Path.Combine(temp.Path, "profile", "settings.json"));
      AppSettings? saved = store.Update(settings =>
      {
         settings.Theme = "Dark";
         settings.OutlineDepth = 99;
         return settings;
      });
      Assert.NotNull(saved);
      Assert.Equal(6, saved.OutlineDepth);
      AppSettings loaded = store.Load();
      Assert.Equal("Dark", loaded.Theme);
      Assert.Equal(6, loaded.OutlineDepth);
      Assert.Equal(["settings.json", "settings.json.lock"], FileNames(Path.Combine(temp.Path, "profile")));
   }



   /// <summary>A change is applied to what is stored now, not to what this instance read earlier.</summary>
   [Fact]
   public void Update_AnotherInstanceSavedFirst_ChangesTheLatestSettings()
   {
      using var temp = new TempDirectory();
      var first = new AppSettingsStore(temp.File("settings.json"));
      var second = new AppSettingsStore(temp.File("settings.json"));
      _ = first.Load();
      Assert.NotNull(second.Update(settings =>
      {
         settings.GlobalCss = "body { color: red }";
         return settings;
      }));
      Assert.NotNull(first.Update(settings =>
      {
         settings.Theme = "Light";
         return settings;
      }));
      AppSettings loaded = second.Load();
      Assert.Equal("body { color: red }", loaded.GlobalCss);
      Assert.Equal("Light", loaded.Theme);
   }



   /// <summary>A file saved by hand with a byte order mark, in UTF-8 or UTF-16, is still the user's settings.</summary>
   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public void Load_ByteOrderMark_ReadsTheStoredValues(bool utf16)
   {
      using var temp = new TempDirectory();
      Encoding marked = utf16 ? Encoding.Unicode : new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
      File.WriteAllText(temp.File("settings.json"), """{"Theme":"Dark","DocumentTheme":"Nord"}""", marked);
      var faults = new List<SettingsFault>();
      AppSettings loaded = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault)).Load();
      Assert.Equal("Dark", loaded.Theme);
      Assert.Equal("Nord", loaded.DocumentTheme);
      Assert.Empty(faults);
   }



   /// <summary>Another handle holding the file for a moment (a scanner, a backup) is waited out, not read as an empty profile.</summary>
   [Fact]
   public void Load_FileLockedBriefly_RetriesAndReadsTheStoredValues()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      FileStream? exclusive = new(temp.File("settings.json"), FileMode.Open, FileAccess.Read, FileShare.None);
      int waits = 0;
      var faults = new List<SettingsFault>();
      var store = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault), _ =>
      {
         if(++waits == 3)
         {
            exclusive.Dispose();
         }
      });
      AppSettings loaded = store.Load();
      Assert.Equal(3, waits);
      Assert.Equal("Nord", loaded.DocumentTheme);
      Assert.Equal([KeptPath], loaded.RecentFiles);
      Assert.Empty(faults);
   }



   /// <summary>A file that stays unreadable yields defaults for display and is reported; the attempts are bounded.</summary>
   [Fact]
   public void Load_FileLockedThroughout_ReturnsDefaultsAndReportsUnreadable()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      int waits = 0;
      var faults = new List<(SettingsFault Fault, Exception? Exception)>();
      var store = new AppSettingsStore(temp.File("settings.json"), (fault, exception) => faults.Add((fault, exception)), _ => waits++);
      using(new FileStream(temp.File("settings.json"), FileMode.Open, FileAccess.Read, FileShare.None))
      {
         Assert.Equal("Catppuccin Mocha", store.Load().DocumentTheme);
      }
      Assert.Equal(AppSettingsStore.MaximumAttempts - 1, waits);
      (SettingsFault fault, Exception? exception) = Assert.Single(faults);
      Assert.Equal(SettingsFault.Unreadable, fault);
      Assert.IsType<IOException>(exception);
   }



   /// <summary>
   /// The data-loss regression: a read failure is not an empty profile. Nothing is written over
   /// a file whose content is unknown, so the stored theme, CSS and history survive.
   /// </summary>
   [Fact]
   public void Update_FileLockedThroughout_WritesNothingOverIt()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      byte[] before = File.ReadAllBytes(temp.File("settings.json"));
      var faults = new List<SettingsFault>();
      var store = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault), _ => { });
      bool changed = false;
      using(new FileStream(temp.File("settings.json"), FileMode.Open, FileAccess.Read, FileShare.None))
      {
         Assert.Null(store.Update(settings =>
         {
            changed = true;
            return settings;
         }));
      }
      Assert.False(changed);
      Assert.Equal([SettingsFault.Unreadable], faults);
      Assert.Equal(before, File.ReadAllBytes(temp.File("settings.json")));
      Assert.Equal(["settings.json", "settings.json.lock"], FileNames(temp.Path));
      AppSettings loaded = store.Load();
      Assert.Equal("Nord", loaded.DocumentTheme);
      Assert.Equal("body { color: red }", loaded.GlobalCss);
      Assert.Equal([KeptPath], loaded.RecentFiles);
   }



   /// <summary>Only invalid content is corruption: reading it yields defaults, reports it, and leaves the file where it is.</summary>
   [Theory]
   [InlineData("")]
   [InlineData("{")]
   [InlineData("null")]
   [InlineData("[1]")]
   [InlineData("{\"OutlineDepth\":\"deep\"}")]
   public void Load_InvalidContent_ReturnsDefaultsAndLeavesTheFile(string content)
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("settings.json"), content);
      var faults = new List<SettingsFault>();
      AppSettings loaded = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault)).Load();
      Assert.Equal("Catppuccin Mocha", loaded.DocumentTheme);
      Assert.Equal([SettingsFault.Corrupt], faults);
      Assert.Equal(content, File.ReadAllText(temp.File("settings.json")));
      Assert.Equal(["settings.json"], FileNames(temp.Path));
   }



   /// <summary>Saving over invalid content keeps that content aside, so a hand edit gone wrong can still be recovered.</summary>
   [Fact]
   public void Update_InvalidContent_KeepsItAsideAndSavesTheChange()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("settings.json"), "{ \"Theme\": \"Dark\", ");
      File.WriteAllText(temp.File("settings.json.corrupt"), "an older invalid file");
      var store = new AppSettingsStore(temp.File("settings.json"));
      Assert.NotNull(store.Update(settings =>
      {
         settings.DocumentTheme = "Nord";
         return settings;
      }));
      Assert.Equal("{ \"Theme\": \"Dark\", ", File.ReadAllText(temp.File("settings.json.corrupt")));
      Assert.Equal("Nord", store.Load().DocumentTheme);
      Assert.Equal("System", store.Load().Theme);
   }



   /// <summary>A file far larger than any settings is not loaded into memory, even when it would parse; it reads as invalid content.</summary>
   [Fact]
   public void Load_OversizedFile_ReadsAsInvalidContent()
   {
      using var temp = new TempDirectory();
      byte[] valid = Encoding.UTF8.GetBytes("""{"DocumentTheme":"Nord"}""");
      byte[] padded = new byte[AppSettingsStore.MaximumBytes + 1];
      Array.Fill(padded, (byte)' ');
      valid.CopyTo(padded, 0);
      File.WriteAllBytes(temp.File("settings.json"), padded);
      var faults = new List<SettingsFault>();
      Assert.Equal("Catppuccin Mocha", new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault)).Load().DocumentTheme);
      Assert.Equal([SettingsFault.Corrupt], faults);
   }



   /// <summary>While another instance holds the lock for every attempt, the change is refused and the file is unchanged.</summary>
   [Fact]
   public void Update_LockHeldByAnotherInstance_ReportsBusyAndWritesNothing()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      byte[] before = File.ReadAllBytes(temp.File("settings.json"));
      int waits = 0;
      var faults = new List<SettingsFault>();
      var store = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault), _ => waits++);
      using(new FileStream(temp.File("settings.json.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
      {
         Assert.Null(store.Update(settings => settings));
      }
      Assert.Equal(AppSettingsStore.MaximumAttempts - 1, waits);
      Assert.Equal([SettingsFault.Busy], faults);
      Assert.Equal(before, File.ReadAllBytes(temp.File("settings.json")));
   }



   /// <summary>A lock released while this instance waits lets the change through.</summary>
   [Fact]
   public void Update_LockReleasedDuringWait_SavesTheChange()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      FileStream other = new(temp.File("settings.json.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
      var store = new AppSettingsStore(temp.File("settings.json"), wait: _ => other.Dispose());
      AppSettings? saved = store.Update(settings =>
      {
         settings.Theme = "Light";
         return settings;
      });
      Assert.NotNull(saved);
      Assert.Equal("Light", store.Load().Theme);
      Assert.Equal([KeptPath], store.Load().RecentFiles);
   }



   /// <summary>A handle that forbids replacing the file fails the save cleanly: the previous file stays and no temporary file is left.</summary>
   [Fact]
   public void Update_ReplaceBlocked_LeavesPreviousFileAndNoTemporaryFile()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      byte[] before = File.ReadAllBytes(temp.File("settings.json"));
      int waits = 0;
      var faults = new List<SettingsFault>();
      var store = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault), _ => waits++);
      using(new FileStream(temp.File("settings.json"), FileMode.Open, FileAccess.Read, FileShare.Read))
      {
         Assert.Null(store.Update(settings =>
         {
            settings.Theme = "Light";
            return settings;
         }));
      }
      Assert.Equal(AppSettingsStore.MaximumAttempts - 1, waits);
      Assert.Equal([SettingsFault.WriteFailed], faults);
      Assert.Equal(before, File.ReadAllBytes(temp.File("settings.json")));
      Assert.Equal(["settings.json", "settings.json.lock"], FileNames(temp.Path));
   }



   /// <summary>A location that cannot hold a settings folder reports a failed write instead of throwing into the caller.</summary>
   [Fact]
   public void Update_FolderCannotBeCreated_ReportsWriteFailed()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("blocker"), "a file where the folder should be");
      var faults = new List<SettingsFault>();
      var store = new AppSettingsStore(Path.Combine(temp.File("blocker"), "settings.json"), (fault, _) => faults.Add(fault), _ => { });
      Assert.Null(store.Update(settings => settings));
      Assert.Equal([SettingsFault.WriteFailed], faults);
      Assert.Equal("Catppuccin Mocha", store.Load().DocumentTheme);
   }



   /// <summary>Temporary files left by a writer that died before its rename are removed by the next save; one still in use is left alone.</summary>
   [Fact]
   public void Update_AbandonedTemporaryFiles_AreRemovedUnlessInUse()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      File.WriteAllText(temp.File("settings.json.tmp"), "left by the previous store");
      File.WriteAllText(temp.File("settings.json.0123.tmp"), "left by a writer that died");
      File.WriteAllText(temp.File("settings.json.busy.tmp"), "still open elsewhere");
      File.WriteAllText(temp.File("notes.tmp"), "not ours");
      var store = new AppSettingsStore(temp.File("settings.json"));
      using(new FileStream(temp.File("settings.json.busy.tmp"), FileMode.Open, FileAccess.Read, FileShare.None))
      {
         Assert.NotNull(store.Update(settings => settings));
      }
      Assert.Equal(["notes.tmp", "settings.json", "settings.json.busy.tmp", "settings.json.lock"], FileNames(temp.Path));
   }



   /// <summary>
   /// The lost-update regression: instances that each record their own documents at the same
   /// time keep every one. A refused save is retried, as a user would; a save that reported
   /// success must never be erased by another instance.
   /// </summary>
   [Fact]
   public void Update_ConcurrentInstances_KeepEveryChange()
   {
      using var temp = new TempDirectory();
      const int Instances = 4;
      const int Each = 5;
      using var start = new ManualResetEventSlim();
      var threads = new List<Thread>();
      for(int instance = 0; instance < Instances; instance++)
      {
         int id = instance;
         var thread = new Thread(() =>
         {
            var store = new AppSettingsStore(temp.File("settings.json"));
            start.Wait();
            for(int index = 0; index < Each; index++)
            {
               string path = $@"C:\docs\instance{id}-{index}.md";
               while(store.Update(settings =>
               {
                  settings.RecentFiles = Documents.RecentDocuments.Record(settings.RecentFiles, path);
                  return settings;
               }) is null)
               {
                  Thread.Yield();
               }
            }
         });
         thread.Start();
         threads.Add(thread);
      }
      start.Set();
      threads.ForEach(thread => thread.Join());
      string[] recorded = new AppSettingsStore(temp.File("settings.json")).Load().RecentFiles;
      Assert.Equal(Instances * Each, recorded.Length);
      Assert.Equal(Instances * Each, recorded.Distinct(StringComparer.Ordinal).Count());
      Assert.Equal(["settings.json", "settings.json.lock"], FileNames(temp.Path));
   }



   /// <summary>A profile with non-default values in several unrelated fields, so a wipe is visible.</summary>
   private static void Seed(TempDirectory temp)
   {
      Assert.NotNull(new AppSettingsStore(temp.File("settings.json")).Update(settings =>
      {
         settings.Theme = "Dark";
         settings.DocumentTheme = "Nord";
         settings.GlobalCss = "body { color: red }";
         settings.RecentFiles = [KeptPath];
         return settings;
      }));
   }



   /// <summary>The folder's file names in ordinal order.</summary>
   private static string[] FileNames(string folder)
      => Directory.GetFiles(folder).Select(file => Path.GetFileName(file)!).Order(StringComparer.Ordinal).ToArray();
}
