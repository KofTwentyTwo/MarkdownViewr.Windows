/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text;
using System.Text.Json.Nodes;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>
/// The settings file is shared by windows and processes: these tests use real files, real
/// handles held the way other programs hold them, and concurrent store instances. They count
/// attempts instead of measuring time.
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



   /// <summary>The store addresses one file whatever the working folder becomes later.</summary>
   [Fact]
   public void Constructor_RelativePath_IsMadeAbsolute()
   {
      var store = new AppSettingsStore(Path.Combine("relative-profile", "settings.json"));
      Assert.True(Path.IsPathFullyQualified(store.Path));
      Assert.Equal(Path.GetFullPath(Path.Combine("relative-profile", "settings.json")), store.Path);
      Assert.Throws<ArgumentException>(() => new AppSettingsStore(" "));
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



   /// <summary>Callers that merge return a new object: that object is what is saved and what comes back.</summary>
   [Fact]
   public void Update_ChangeReturnsANewInstance_SavesAndReturnsThatInstance()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      var store = new AppSettingsStore(temp.File("settings.json"));
      var replacement = new AppSettings { DocumentTheme = "Dracula", OutlineWidth = 9000 };
      AppSettings? saved = store.Update(_ => replacement);
      Assert.Same(replacement, saved);
      Assert.Equal(500, replacement.OutlineWidth);
      AppSettings loaded = store.Load();
      Assert.Equal("Dracula", loaded.DocumentTheme);
      Assert.Equal("System", loaded.Theme);
      Assert.Empty(loaded.RecentFiles);
   }



   /// <summary>Stored values outside their range are repaired on every read, not only on save.</summary>
   [Fact]
   public void Load_StoredValuesOutOfRange_AreSanitized()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("settings.json"), """{"OutlineDepth":99,"Theme":"Purple","OutlineWidth":-4}""");
      AppSettings loaded = new AppSettingsStore(temp.File("settings.json")).Load();
      Assert.Equal(6, loaded.OutlineDepth);
      Assert.Equal("System", loaded.Theme);
      Assert.Equal(140, loaded.OutlineWidth);
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



   /// <summary>Without a byte order mark the file is UTF-8, so text outside ASCII reads as written.</summary>
   [Fact]
   public void Load_Utf8WithoutByteOrderMark_ReadsTextOutsideAscii()
   {
      using var temp = new TempDirectory();
      File.WriteAllBytes(temp.File("settings.json"), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes("""{"GlobalCss":"/* café ☕ */"}"""));
      Assert.Equal("/* café ☕ */", new AppSettingsStore(temp.File("settings.json")).Load().GlobalCss);
   }



   /// <summary>What a hand edit commonly leaves behind is still valid settings: a trailing comma, a comment, a quoted number.</summary>
   [Theory]
   [InlineData("""{"Theme":"Dark","OutlineDepth":4,}""")]
   [InlineData("""{ /* my settings */ "Theme":"Dark", "OutlineDepth":4 }""")]
   [InlineData("""{"Theme":"Dark","OutlineDepth":"4"}""")]
   public void Load_HandEditedSyntax_ReadsWithoutAFault(string content)
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("settings.json"), content);
      var faults = new List<SettingsFault>();
      AppSettings loaded = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault)).Load();
      Assert.Equal("Dark", loaded.Theme);
      Assert.Equal(4, loaded.OutlineDepth);
      Assert.Empty(faults);
   }



   /// <summary>The file on disk stays what earlier versions wrote and can read: indented UTF-8 JSON with no byte order mark.</summary>
   [Fact]
   public void Update_WrittenFile_IsIndentedUtf8WithoutByteOrderMarkAndReadableByThePreviousStore()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      byte[] bytes = File.ReadAllBytes(temp.File("settings.json"));
      Assert.Equal((byte)'{', bytes[0]);
      Assert.Contains(Environment.NewLine + "  \"Theme\": \"Dark\"", Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
      var previous = new KofTwentyTwo.AppKit.Settings.SettingsStore<AppSettings>(temp.File("settings.json"), AppSettingsContext.Default.AppSettings);
      AppSettings read = previous.Load();
      Assert.Equal("Nord", read.DocumentTheme);
      Assert.Equal("body { color: red }", read.GlobalCss);
      Assert.Equal([KeptPath], read.RecentFiles);
      read.Theme = "Light";
      Assert.True(previous.Save(read));
      AppSettings loaded = new AppSettingsStore(temp.File("settings.json")).Load();
      Assert.Equal("Light", loaded.Theme);
      Assert.Equal("Nord", loaded.DocumentTheme);
   }



   /// <summary>Another handle holding the file for a moment (a scanner, a backup) is waited out, not read as an empty profile.</summary>
   [Fact]
   public void Load_FileLockedBriefly_RetriesAndReadsTheStoredValues()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      using var exclusive = new FileStream(temp.File("settings.json"), FileMode.Open, FileAccess.Read, FileShare.None);
      var delays = new List<TimeSpan>();
      var faults = new List<SettingsFault>();
      var store = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault), delay =>
      {
         delays.Add(delay);
         if(delays.Count == 3)
         {
            exclusive.Dispose();
         }
      });
      AppSettings loaded = store.Load();
      Assert.Equal([AppSettingsStore.RetryDelay, AppSettingsStore.RetryDelay, AppSettingsStore.RetryDelay], delays);
      Assert.Equal(TimeSpan.FromMilliseconds(50), AppSettingsStore.RetryDelay);
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
   /// A read failure is not an empty profile: nothing is written over a file whose content is
   /// unknown, so the stored theme, CSS and history survive.
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



   /// <summary>
   /// Access denied is permanent, not a moment to wait out: it is reported at once, defaults are
   /// shown, and nothing is written. A folder where the file should be answers exactly that way.
   /// </summary>
   [Fact]
   public void LoadAndUpdate_AccessDenied_ReportUnreadableWithoutWaitingOrWriting()
   {
      using var temp = new TempDirectory();
      Directory.CreateDirectory(temp.File("settings.json"));
      int waits = 0;
      var faults = new List<(SettingsFault Fault, Exception? Exception)>();
      var store = new AppSettingsStore(temp.File("settings.json"), (fault, exception) => faults.Add((fault, exception)), _ => waits++);
      Assert.Equal("Catppuccin Mocha", store.Load().DocumentTheme);
      bool changed = false;
      Assert.Null(store.Update(settings =>
      {
         changed = true;
         return settings;
      }));
      Assert.False(changed);
      Assert.Equal(0, waits);
      Assert.Equal([SettingsFault.Unreadable, SettingsFault.Unreadable], faults.Select(entry => entry.Fault));
      Assert.All(faults, entry => Assert.IsType<UnauthorizedAccessException>(entry.Exception));
      Assert.True(Directory.Exists(temp.File("settings.json")));
      Assert.Equal(["settings.json.lock"], FileNames(temp.Path));
   }



   /// <summary>
   /// Another program rewriting the file in place has truncated it and not finished. That is a
   /// busy file, not invalid content: it is not set aside, and once the writer finishes the
   /// change is applied to what the writer wrote.
   /// </summary>
   [Fact]
   public void Update_FileBeingRewrittenInPlace_WaitsForTheWriterAndKeepsItsContent()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      using var writer = new FileStream(temp.File("settings.json"), FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
      int waits = 0;
      var faults = new List<SettingsFault>();
      var store = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault), _ =>
      {
         if(++waits == 2)
         {
            writer.Write("""{"Theme":"Light","DocumentTheme":"Dracula","GlobalCss":"p { margin: 0 }"}"""u8);
            writer.Dispose();
         }
      });
      AppSettings? saved = store.Update(settings =>
      {
         settings.OutlineDepth = 5;
         return settings;
      });
      Assert.NotNull(saved);
      Assert.Equal(2, waits);
      Assert.Empty(faults);
      AppSettings loaded = store.Load();
      Assert.Equal("Light", loaded.Theme);
      Assert.Equal("Dracula", loaded.DocumentTheme);
      Assert.Equal("p { margin: 0 }", loaded.GlobalCss);
      Assert.Equal(5, loaded.OutlineDepth);
      Assert.Equal(["settings.json", "settings.json.lock"], FileNames(temp.Path));
   }



   /// <summary>A writer that never finishes leaves the save refused and the file exactly as the writer has it.</summary>
   [Fact]
   public void Update_FileStillBeingRewritten_IsRefusedAndNothingIsSetAside()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      var faults = new List<SettingsFault>();
      var store = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault), _ => { });
      using(new FileStream(temp.File("settings.json"), FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
      {
         Assert.Null(store.Update(settings => settings));
         Assert.Equal("Catppuccin Mocha", store.Load().DocumentTheme);
      }
      Assert.Equal([SettingsFault.Unreadable, SettingsFault.Unreadable], faults);
      Assert.Equal(["settings.json", "settings.json.lock"], FileNames(temp.Path));
      Assert.Equal(0, new FileInfo(temp.File("settings.json")).Length);
   }



   /// <summary>Content that is not settings at all yields defaults, is reported, and stays where it is until something is saved.</summary>
   [Theory]
   [InlineData("")]
   [InlineData("{")]
   [InlineData("null")]
   [InlineData("[1]")]
   [InlineData("\"text\"")]
   [InlineData("not json")]
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



   /// <summary>An invalid value in a file that also has hand-edited syntax still costs only that value.</summary>
   [Fact]
   public void Load_InvalidValueBesideHandEditedSyntax_KeepsEveryOtherValue()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("settings.json"), """{ /* mine */ "Theme":"Dark", "OutlineDepth":"deep", "DocumentTheme":"Nord", }""");
      var faults = new List<SettingsFault>();
      AppSettings loaded = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault)).Load();
      Assert.Equal([SettingsFault.Repaired], faults);
      Assert.Equal("Dark", loaded.Theme);
      Assert.Equal("Nord", loaded.DocumentTheme);
      Assert.Equal(3, loaded.OutlineDepth);
   }



   /// <summary>One invalid value costs only that value: every other stored preference is kept.</summary>
   [Fact]
   public void Load_OneInvalidValue_KeepsEveryOtherValue()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("settings.json"), """{"Theme":"Dark","OutlineDepth":5.5,"DocumentTheme":"Nord","RecentFiles":["C:\\docs\\a.md",5],"GlobalCss":"body { color: red }","FutureSetting":{"a":1}}""");
      var faults = new List<SettingsFault>();
      AppSettings loaded = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault)).Load();
      Assert.Equal([SettingsFault.Repaired], faults);
      Assert.Equal("Dark", loaded.Theme);
      Assert.Equal("Nord", loaded.DocumentTheme);
      Assert.Equal("body { color: red }", loaded.GlobalCss);
      Assert.Equal(3, loaded.OutlineDepth);
      Assert.Empty(loaded.RecentFiles);
   }



   /// <summary>A repeated property beside an invalid value is still read without throwing into startup.</summary>
   [Fact]
   public void Load_RepeatedPropertyAndInvalidValue_DoesNotThrow()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("settings.json"), """{"Theme":"Dark","Theme":"Light","OutlineDepth":"deep"}""");
      var faults = new List<SettingsFault>();
      AppSettings loaded = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault)).Load();
      Assert.Contains(Assert.Single(faults), new[] { SettingsFault.Corrupt, SettingsFault.Repaired });
      Assert.Equal(3, loaded.OutlineDepth);
   }



   /// <summary>A saved theme whose text the JSON reader cannot decode is discarded like any other invalid theme; it cannot stop the application starting.</summary>
   [Fact]
   public void Load_SavedThemeWithUnpairedSurrogate_DiscardsThatThemeOnly()
   {
      using var temp = new TempDirectory();
      string valid = Themes.ThemeCatalog.Find("Nord").Json;
      string undecodable = valid.Replace("\"Nord\"", "\"\\uD800\"", StringComparison.Ordinal);
      Assert.NotEqual(valid, undecodable, StringComparer.Ordinal);
      var stored = new JsonObject { ["Theme"] = "Dark", ["UserThemes"] = new JsonArray(undecodable, valid) };
      File.WriteAllText(temp.File("settings.json"), stored.ToJsonString());
      AppSettings loaded = new AppSettingsStore(temp.File("settings.json")).Load();
      Assert.Equal("Dark", loaded.Theme);
      Assert.Equal([valid], loaded.UserThemes);
   }



   /// <summary>Saving over invalid content keeps that content under a free name; an earlier kept file is not overwritten.</summary>
   [Fact]
   public void Update_InvalidContent_KeepsItAsideUnderAFreeNameAndSavesTheChange()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("settings.json"), "{ \"Theme\": \"Dark\", ");
      File.WriteAllText(temp.File("settings.json.corrupt"), "an earlier invalid file");
      var store = new AppSettingsStore(temp.File("settings.json"));
      Assert.NotNull(store.Update(settings =>
      {
         settings.DocumentTheme = "Nord";
         return settings;
      }));
      Assert.Equal("an earlier invalid file", File.ReadAllText(temp.File("settings.json.corrupt")));
      Assert.Equal("{ \"Theme\": \"Dark\", ", File.ReadAllText(temp.File("settings.json.corrupt-2")));
      Assert.Equal("Nord", store.Load().DocumentTheme);
      Assert.Equal("System", store.Load().Theme);
      Assert.Equal(["settings.json", "settings.json.corrupt", "settings.json.corrupt-2", "settings.json.lock"], FileNames(temp.Path));
   }



   /// <summary>A repaired file is saved with its valid values and the change, and the original is kept for the value that was dropped.</summary>
   [Fact]
   public void Update_RepairedContent_SavesTheValidValuesAndKeepsTheOriginalAside()
   {
      using var temp = new TempDirectory();
      const string Original = """{"Theme":"Dark","OutlineDepth":"deep","DocumentTheme":"Nord"}""";
      File.WriteAllText(temp.File("settings.json"), Original);
      var store = new AppSettingsStore(temp.File("settings.json"));
      Assert.NotNull(store.Update(settings =>
      {
         settings.GlobalCss = "p { margin: 0 }";
         return settings;
      }));
      Assert.Equal(Original, File.ReadAllText(temp.File("settings.json.corrupt")));
      var faults = new List<SettingsFault>();
      AppSettings loaded = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault)).Load();
      Assert.Empty(faults);
      Assert.Equal("Dark", loaded.Theme);
      Assert.Equal("Nord", loaded.DocumentTheme);
      Assert.Equal("p { margin: 0 }", loaded.GlobalCss);
   }



   /// <summary>Kept files are bounded: once every slot is used the newest slot is reused and the older ones stay.</summary>
   [Fact]
   public void Update_EveryKeptSlotUsed_ReusesTheNewestSlot()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("settings.json.corrupt"), "kept 1");
      for(int slot = 2; slot <= AppSettingsStore.MaximumKeptAside; slot++)
      {
         string number = slot.ToString(System.Globalization.CultureInfo.InvariantCulture);
         File.WriteAllText(temp.File("settings.json.corrupt-" + number), "kept " + number);
      }
      File.WriteAllText(temp.File("settings.json"), "newest invalid");
      Assert.NotNull(new AppSettingsStore(temp.File("settings.json")).Update(settings => settings));
      Assert.Equal("kept 1", File.ReadAllText(temp.File("settings.json.corrupt")));
      Assert.Equal("kept 8", File.ReadAllText(temp.File("settings.json.corrupt-8")));
      Assert.Equal("newest invalid", File.ReadAllText(temp.File("settings.json.corrupt-9")));
      Assert.False(File.Exists(temp.File("settings.json.corrupt-10")));
      Assert.Equal(9, AppSettingsStore.MaximumKeptAside);
   }



   /// <summary>When the save over invalid content fails, the invalid file is still in place: it is never moved away first.</summary>
   [Fact]
   public void Update_InvalidContentAndFailedSave_LeavesTheInvalidFileInPlace()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("settings.json"), "not json");
      var faults = new List<SettingsFault>();
      var store = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault), _ => { });
      using(new FileStream(temp.File("settings.json"), FileMode.Open, FileAccess.Read, FileShare.Read))
      {
         Assert.Null(store.Update(settings => settings));
      }
      Assert.Equal([SettingsFault.Corrupt, SettingsFault.WriteFailed], faults);
      Assert.Equal("not json", File.ReadAllText(temp.File("settings.json")));
      Assert.Equal(["settings.json", "settings.json.lock"], FileNames(temp.Path));
   }



   /// <summary>A change that throws leaves the file, the lock and the folder as they were; the next save works.</summary>
   [Fact]
   public void Update_ChangeThrows_LeavesTheFileAndReleasesTheLock()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("settings.json"), "not json");
      var store = new AppSettingsStore(temp.File("settings.json"));
      Assert.Throws<InvalidOperationException>(() => store.Update(_ => throw new InvalidOperationException("caller bug")));
      Assert.Equal("not json", File.ReadAllText(temp.File("settings.json")));
      Assert.Equal(["settings.json", "settings.json.lock"], FileNames(temp.Path));
      Assert.Throws<ArgumentNullException>(() => store.Update(null!));
      Assert.NotNull(store.Update(settings => settings));
   }



   /// <summary>Reading for display holds nothing afterwards: another program can replace or rewrite the file at once.</summary>
   [Fact]
   public void Load_AfterReading_ReleasesTheFile()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      var store = new AppSettingsStore(temp.File("settings.json"));
      Assert.Equal("Nord", store.Load().DocumentTheme);
      using(new FileStream(temp.File("settings.json"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
      {
         Assert.Equal("Catppuccin Mocha", new AppSettingsStore(temp.File("settings.json"), wait: _ => { }).Load().DocumentTheme);
      }
      Assert.Equal("Nord", store.Load().DocumentTheme);
   }



   /// <summary>
   /// Between judging the stored content and swapping in the new file, no other program can
   /// rewrite it in place: the content that was read is the content that is replaced.
   /// </summary>
   [Fact]
   public void Update_WhileTheChangeRuns_TheStoredFileCannotBeRewrittenInPlace()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      var store = new AppSettingsStore(temp.File("settings.json"));
      bool refused = false;
      Assert.NotNull(store.Update(settings =>
      {
         refused = Record.Exception(() => new FileStream(temp.File("settings.json"), FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete).Dispose()) is IOException;
         settings.Theme = "Light";
         return settings;
      }));
      Assert.True(refused);
      Assert.Equal("Light", store.Load().Theme);
      Assert.Equal("Nord", store.Load().DocumentTheme);
   }



   /// <summary>A file that appears after this instance found none was written by someone else and is not overwritten.</summary>
   [Fact]
   public void Update_FileAppearsAfterTheRead_IsNotOverwritten()
   {
      using var temp = new TempDirectory();
      var faults = new List<SettingsFault>();
      var store = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault));
      Assert.Null(store.Update(settings =>
      {
         File.WriteAllText(temp.File("settings.json"), """{"DocumentTheme":"Dracula"}""");
         settings.Theme = "Light";
         return settings;
      }));
      Assert.Equal([SettingsFault.WriteFailed], faults);
      Assert.Equal("""{"DocumentTheme":"Dracula"}""", File.ReadAllText(temp.File("settings.json")));
      Assert.Equal(["settings.json", "settings.json.lock"], FileNames(temp.Path));
      AppSettings? saved = store.Update(settings =>
      {
         settings.Theme = "Light";
         return settings;
      });
      Assert.NotNull(saved);
      Assert.Equal("Dracula", saved.DocumentTheme);
   }



   /// <summary>A file is read up to the size bound and not loaded past it, even when it would parse.</summary>
   [Theory]
   [InlineData(0, true)]
   [InlineData(1, false)]
   public void Load_FileAtAndPastTheSizeBound_IsBoundedExactly(int extraBytes, bool loaded)
   {
      using var temp = new TempDirectory();
      byte[] padded = new byte[AppSettingsStore.MaximumBytes + extraBytes];
      Array.Fill(padded, (byte)' ');
      """{"DocumentTheme":"Nord"}"""u8.CopyTo(padded);
      File.WriteAllBytes(temp.File("settings.json"), padded);
      var faults = new List<SettingsFault>();
      Assert.Equal(loaded ? "Nord" : "Catppuccin Mocha", new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault)).Load().DocumentTheme);
      Assert.Equal(loaded ? [] : [SettingsFault.Corrupt], faults);
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



   /// <summary>
   /// Mutual exclusion, shown without threads: while one instance is inside its change, a second
   /// instance's save is refused for every attempt, and the first instance's change is what ends
   /// up stored.
   /// </summary>
   [Fact]
   public void Update_WhileAnotherInstanceIsInsideItsChange_IsRefusedAndTheFirstChangeIsSaved()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      int waits = 0;
      var faults = new List<SettingsFault>();
      var first = new AppSettingsStore(temp.File("settings.json"));
      var second = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault), _ => waits++);
      bool secondChanged = false;
      AppSettings? saved = first.Update(settings =>
      {
         Assert.Null(second.Update(other =>
         {
            secondChanged = true;
            other.Theme = "Light";
            return other;
         }));
         settings.DocumentTheme = "Dracula";
         return settings;
      });
      Assert.NotNull(saved);
      Assert.False(secondChanged);
      Assert.Equal(AppSettingsStore.MaximumAttempts - 1, waits);
      Assert.Equal([SettingsFault.Busy], faults);
      AppSettings loaded = second.Load();
      Assert.Equal("Dracula", loaded.DocumentTheme);
      Assert.Equal("Dark", loaded.Theme);
      Assert.NotNull(second.Update(other => other));
   }



   /// <summary>A lock released while this instance waits lets the change through.</summary>
   [Fact]
   public void Update_LockReleasedDuringWait_SavesTheChange()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      using var other = new FileStream(temp.File("settings.json.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
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



   /// <summary>A reader that allows the file to be replaced, as this store's own reads do, does not stop a save.</summary>
   [Fact]
   public void Update_FileOpenByAReaderThatAllowsReplacement_StillSaves()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      int waits = 0;
      var store = new AppSettingsStore(temp.File("settings.json"), wait: _ => waits++);
      using(new FileStream(temp.File("settings.json"), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
      {
         Assert.NotNull(store.Update(settings =>
         {
            settings.Theme = "Light";
            return settings;
         }));
      }
      Assert.Equal(0, waits);
      Assert.Equal("Light", store.Load().Theme);
      Assert.Equal(["settings.json", "settings.json.lock"], FileNames(temp.Path));
   }



   /// <summary>A handle that forbids replacing the file fails the save cleanly: the stored file stays and no temporary file is left.</summary>
   [Fact]
   public void Update_ReplaceBlocked_LeavesTheStoredFileAndNoTemporaryFile()
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



   /// <summary>A read-only settings file fails the save at once instead of stalling the window for every attempt.</summary>
   [Fact]
   public void Update_ReadOnlyFile_FailsWithoutWaiting()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      byte[] before = File.ReadAllBytes(temp.File("settings.json"));
      int waits = 0;
      var faults = new List<(SettingsFault Fault, Exception? Exception)>();
      var store = new AppSettingsStore(temp.File("settings.json"), (fault, exception) => faults.Add((fault, exception)), _ => waits++);
      File.SetAttributes(temp.File("settings.json"), FileAttributes.ReadOnly);
      try
      {
         Assert.Null(store.Update(settings =>
         {
            settings.Theme = "Light";
            return settings;
         }));
         Assert.Equal("Nord", store.Load().DocumentTheme);
      }
      finally
      {
         File.SetAttributes(temp.File("settings.json"), FileAttributes.Normal);
      }
      Assert.Equal(0, waits);
      (SettingsFault fault, Exception? exception) = Assert.Single(faults);
      Assert.Equal(SettingsFault.WriteFailed, fault);
      Assert.IsType<UnauthorizedAccessException>(exception);
      Assert.Equal(before, File.ReadAllBytes(temp.File("settings.json")));
      Assert.Equal(["settings.json", "settings.json.lock"], FileNames(temp.Path));
   }



   /// <summary>A lock that can never be taken, here a folder with the lock's name, is a failed write reported at once, not a busy lock.</summary>
   [Fact]
   public void Update_LockCannotBeOpened_FailsWithoutWaiting()
   {
      using var temp = new TempDirectory();
      Directory.CreateDirectory(temp.File("settings.json.lock"));
      int waits = 0;
      var faults = new List<SettingsFault>();
      var store = new AppSettingsStore(temp.File("settings.json"), (fault, _) => faults.Add(fault), _ => waits++);
      Assert.Null(store.Update(settings => settings));
      Assert.Equal(0, waits);
      Assert.Equal([SettingsFault.WriteFailed], faults);
      Assert.Empty(FileNames(temp.Path));
   }



   /// <summary>A location that cannot hold a settings folder reports a failed write instead of throwing into the caller.</summary>
   [Fact]
   public void Update_FolderCannotBeCreated_ReportsWriteFailed()
   {
      using var temp = new TempDirectory();
      File.WriteAllText(temp.File("blocker"), "a file where the folder should be");
      int waits = 0;
      var faults = new List<SettingsFault>();
      var store = new AppSettingsStore(Path.Combine(temp.File("blocker"), "settings.json"), (fault, _) => faults.Add(fault), _ => waits++);
      Assert.Null(store.Update(settings => settings));
      Assert.Equal([SettingsFault.WriteFailed], faults);
      Assert.Equal(0, waits);
      Assert.Equal("Catppuccin Mocha", store.Load().DocumentTheme);
   }



   /// <summary>
   /// Only this store's own abandoned temporary files are removed by the next save. One still in
   /// use, the fixed-name temporary file an older release writes, and anything else ending in
   /// .tmp are left alone.
   /// </summary>
   [Fact]
   public void Update_AbandonedTemporaryFiles_RemovesOnlyItsOwn()
   {
      using var temp = new TempDirectory();
      Seed(temp);
      string own = "settings.json." + Guid.NewGuid().ToString("N") + ".tmp";
      string inUse = "settings.json." + Guid.NewGuid().ToString("N") + ".tmp";
      string notHex = "settings.json." + new string('z', 32) + ".tmp";
      foreach(string name in new[] { own, inUse, notHex, "settings.json.tmp", "settings.json.bak.tmp", "settings.json.0123.tmp", "notes.tmp" })
      {
         File.WriteAllText(temp.File(name), "left behind");
      }
      var store = new AppSettingsStore(temp.File("settings.json"));
      using(new FileStream(temp.File(inUse), FileMode.Open, FileAccess.Read, FileShare.None))
      {
         Assert.NotNull(store.Update(settings => settings));
      }
      string[] expected = [inUse, notHex, "notes.tmp", "settings.json", "settings.json.0123.tmp", "settings.json.bak.tmp", "settings.json.lock", "settings.json.tmp"];
      Assert.Equal(expected.Order(StringComparer.Ordinal), FileNames(temp.Path), StringComparer.Ordinal);
   }



   /// <summary>
   /// Instances that each record their own documents at the same time keep every one. A refused
   /// save is retried a bounded number of times, as a user would; a save that reported success
   /// must never be erased by another instance.
   /// </summary>
   [Fact]
   public void Update_ConcurrentInstances_KeepEveryChange()
   {
      using var temp = new TempDirectory();
      const int Instances = 4;
      const int Each = 5;
      using var start = new ManualResetEventSlim();
      var failures = new System.Collections.Concurrent.ConcurrentQueue<string>();
      var threads = new List<Thread>();
      for(int instance = 0; instance < Instances; instance++)
      {
         int id = instance;
         var thread = new Thread(() => RecordDocuments(temp.File("settings.json"), id, Each, start, failures));
         thread.Start();
         threads.Add(thread);
      }
      start.Set();
      threads.ForEach(thread => thread.Join());
      Assert.Empty(failures);
      string[] recorded = new AppSettingsStore(temp.File("settings.json")).Load().RecentFiles;
      Assert.Equal(Instances * Each, recorded.Length);
      Assert.Equal(Instances * Each, recorded.Distinct(StringComparer.Ordinal).Count());
      Assert.Equal(["settings.json", "settings.json.lock"], FileNames(temp.Path));
   }



   /// <summary>One instance's work: a failure is recorded for the test to assert, never thrown on a bare thread.</summary>
   private static void RecordDocuments(string file, int id, int count, ManualResetEventSlim start, System.Collections.Concurrent.ConcurrentQueue<string> failures)
   {
      try
      {
         var store = new AppSettingsStore(file);
         start.Wait();
         for(int index = 0; index < count; index++)
         {
            string path = $@"C:\docs\instance{id}-{index}.md";
            int refusals = 0;
            while(store.Update(settings =>
            {
               settings.RecentFiles = Documents.RecentDocuments.Record(settings.RecentFiles, path);
               return settings;
            }) is null)
            {
               if(++refusals == 50)
               {
                  failures.Enqueue(path + " was refused 50 times");
                  return;
               }
            }
         }
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or InvalidOperationException)
      {
         failures.Enqueue(exception.ToString());
      }
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
