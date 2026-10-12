/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Actual JSON bytes, exact conflict choices and copied editor state cannot bypass the existing schema/style boundary.</summary>
public sealed class ThemeTransferTests
{
   /// <summary>Every shipped JSON palette round-trips through the same bounded UTF-8 reader used by the native picker.</summary>
   public static IEnumerable<object[]> BundledJson => ThemeCatalog.BuiltIn.Select(theme => new object[] { theme.Json });



   /// <summary>Exported bytes preserve all original fields and leave the caller-owned stream available.</summary>
   [Theory]
   [MemberData(nameof(BundledJson))]
   public async Task ReadAsync_ExportedBundledJson_PreservesExactSchema(string json)
   {
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
      ReaderTheme read = await ThemeJson.ReadAsync(stream);
      Assert.Equal(json, read.Json);
      Assert.Equal(1, read.SchemaVersion);
      Assert.True(stream.CanRead);
      Assert.EndsWith(".json", ThemeJson.SuggestedFileName(read), StringComparison.Ordinal);
   }



   /// <summary>Common Windows JSON BOM encodings preserve Unicode text and are exported later as valid UTF-8.</summary>
   [Theory]
   [InlineData(false)]
   [InlineData(true)]
   public async Task ReadAsync_BomUtf8OrUtf16_PreservesUnicode(bool utf16)
   {
      string json = ThemeCatalog.Find(null).Rename("Owned café").Json;
      Encoding encoding = utf16 ? Encoding.Unicode : Encoding.UTF8;
      using var stream = new MemoryStream(encoding.GetPreamble().Concat(encoding.GetBytes(json)).ToArray());
      Assert.Equal(json, (await ThemeJson.ReadAsync(stream)).Json);
   }




   /// <summary>The exact maximum is accepted, but a larger stream is rejected without reading the whole selected file.</summary>
   [Fact]
   public async Task ReadAsync_ExactBoundAndOversized_AllocatesOnlyBoundedText()
   {
      string json = ThemeCatalog.Find(null).Json;
      using var exact = new MemoryStream(Encoding.UTF8.GetBytes(json.PadRight(ReaderTheme.MaximumCharacters)));
      Assert.Equal(ReaderTheme.MaximumCharacters, (await ThemeJson.ReadAsync(exact)).Json.Length);
      using var oversized = new MemoryStream(Encoding.UTF8.GetBytes(json.PadRight(2 * 1024 * 1024)));
      await Assert.ThrowsAsync<JsonException>(() => ThemeJson.ReadAsync(oversized));
      Assert.InRange(oversized.Position, ReaderTheme.MaximumCharacters + 1, ReaderTheme.MaximumCharacters + 8192);
      Assert.True(oversized.CanRead);
   }



   /// <summary>Malformed bytes/JSON, cancellation and null sources never produce an importable theme.</summary>
   [Fact]
   public async Task ReadAsync_InvalidBytesJsonOrCancellation_RejectsInput()
   {
      using var malformed = new MemoryStream([0xFF, 0x80, 0xFF]);
      await Assert.ThrowsAsync<DecoderFallbackException>(() => ThemeJson.ReadAsync(malformed));
      using var json = new MemoryStream(Encoding.UTF8.GetBytes("{invalid"));
      await Assert.ThrowsAnyAsync<JsonException>(() => ThemeJson.ReadAsync(json));
      using var cancelled = new MemoryStream(Encoding.UTF8.GetBytes(ThemeCatalog.Find(null).Json));
      await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ThemeJson.ReadAsync(cancelled, new CancellationToken(canceled: true)));
      await Assert.ThrowsAsync<ArgumentNullException>(() => ThemeJson.ReadAsync(null!));
   }



   /// <summary>Positive newer schemas preserve metadata and require explicit consent before any copied import is saved.</summary>
   [Fact]
   public async Task Import_NewerCompatibleSchema_RequiresApprovalAndRetainsVersion()
   {
      JsonNode source = JsonNode.Parse(ThemeCatalog.Find(null).Json)!;
      source["name"] = "Newer owned theme";
      source["schemaVersion"] = 2;
      source["metadata"] = new JsonObject { ["futureField"] = "retained" };
      string json = source.ToJsonString();
      Assert.Throws<JsonException>(() => ReaderTheme.Parse(json));
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
      ReaderTheme theme = await ThemeJson.ReadAsync(stream);
      Assert.Equal(2, theme.SchemaVersion);
      var settings = new AppSettings();
      Assert.Throws<InvalidOperationException>(() => ThemeManagement.Import(settings, theme));
      Assert.Empty(settings.UserThemes);
      AppSettings imported = ThemeManagement.Import(settings, theme, approveNewerSchema: true);
      Assert.Equal(json, Assert.Single(imported.UserThemes));
      imported.Sanitize();
      Assert.Equal(json, Assert.Single(imported.UserThemes));
      ReaderTheme saved = ThemeManagement.All(imported).Single(item => string.Equals(item.Name, theme.Name, StringComparison.Ordinal));
      ReaderTheme renamed = saved.Rename("Renamed future");
      Assert.Equal(2, renamed.SchemaVersion);
      Assert.Equal(2, renamed.Edit(renamed.Name, new Dictionary<string, string>(StringComparer.Ordinal), new Dictionary<string, double>(StringComparer.Ordinal), new Dictionary<string, string>(StringComparer.Ordinal), "").SchemaVersion);
      using var fields = JsonDocument.Parse(renamed.Json);
      Assert.Equal("retained", fields.RootElement.GetProperty("metadata").GetProperty("futureField").GetString());
      AppSettings edited = ThemeManagement.SaveEdited(imported, saved, renamed);
      Assert.Equal("Renamed future", edited.DocumentTheme);
      Assert.Equal(2, ThemeManagement.Active(edited).SchemaVersion);
   }



   /// <summary>Newer version support never accepts invalid versions or malicious known fields.</summary>
   [Theory]
   [InlineData("schemaVersion", "0")]
   [InlineData("schemaVersion", "-1")]
   [InlineData("schemaVersion", "1.5")]
   [InlineData("schemaVersion", "\"2\"")]
   [InlineData("background", "\"url(https://attacker.invalid)\"")]
   [InlineData("baseFontSize", "1000")]
   public void ParseCompatible_InvalidVersionOrStyle_RejectsEveryKnownBoundary(string field, string value)
   {
      JsonNode source = JsonNode.Parse(ThemeCatalog.Find(null).Json)!;
      source["schemaVersion"] = 2;
      JsonNode target = string.Equals(field, "background", StringComparison.Ordinal) ? source["colors"]! : string.Equals(field, "baseFontSize", StringComparison.Ordinal) ? source["sizes"]! : source;
      target[field] = JsonNode.Parse(value);
      Assert.Throws<JsonException>(() => ReaderTheme.ParseCompatible(source.ToJsonString()));
   }



   /// <summary>Overwrite/keep-both/skip have distinct copied results and never mutate the bundled palette or original preferences.</summary>
   [Theory]
   [InlineData(ThemeImportResolution.Overwrite, "Nord", 1)]
   [InlineData(ThemeImportResolution.KeepBoth, "Nord (2)", 1)]
   [InlineData(ThemeImportResolution.Skip, "", 0)]
   public void Import_BuiltInConflict_PreservesExactChoice(ThemeImportResolution resolution, string name, int count)
   {
      ReaderTheme original = ThemeCatalog.Find("Nord");
      ReaderTheme incoming = original.Edit(original.Name, new Dictionary<string, string>(StringComparer.Ordinal), new Dictionary<string, double>(StringComparer.Ordinal), new Dictionary<string, string>(StringComparer.Ordinal) { ["background"] = "#112233" }, "p { color: #123456; }");
      var settings = new AppSettings { DisabledThemes = ["Nord"], ThemeOrder = ["Nord"], GlobalCss = "retained" };
      AppSettings draft = ThemeManagement.Import(settings, incoming, resolution);
      Assert.Empty(settings.UserThemes);
      Assert.Equal(count, draft.UserThemes.Length);
      Assert.Equal(settings.DocumentTheme, draft.DocumentTheme);
      Assert.Equal(settings.DisabledThemes, draft.DisabledThemes);
      Assert.Equal(settings.ThemeOrder, draft.ThemeOrder);
      Assert.Equal(settings.GlobalCss, draft.GlobalCss);
      if(count > 0)
      {
         var imported = ReaderTheme.Parse(Assert.Single(draft.UserThemes));
         Assert.Equal(name, imported.Name);
         Assert.Contains("--background:#112233;", imported.Styles, StringComparison.Ordinal);
         Assert.Contains("p { color: #123456; }", imported.Styles, StringComparison.Ordinal);
      }
      Assert.DoesNotContain("#112233", original.Json, StringComparison.Ordinal);
   }



   /// <summary>Batch imports reserve prior names, overwrite only matching JSON and can revive a deliberately deleted bundled choice.</summary>
   [Fact]
   public void Import_BatchCollisionsOverwriteAndDeleted_PreservesUnrelatedThemes()
   {
      ReaderTheme nord = ThemeCatalog.Find("Nord");
      ReaderTheme custom = nord.Rename("Owned theme");
      var settings = new AppSettings { UserThemes = [custom.Json], DeletedBuiltInThemes = ["Dracula"] };
      AppSettings first = ThemeManagement.Import(settings, nord);
      AppSettings second = ThemeManagement.Import(first, nord);
      Assert.Equal(new[] { "Owned theme", "Nord (2)", "Nord (3)" }, second.UserThemes.Select(json => ReaderTheme.Parse(json).Name), StringComparer.Ordinal);
      ReaderTheme changed = custom.Rename(custom.Name).Edit(custom.Name, new Dictionary<string, string>(StringComparer.Ordinal), new Dictionary<string, double>(StringComparer.Ordinal), new Dictionary<string, string>(StringComparer.Ordinal), "p { color: #123456; }");
      AppSettings replaced = ThemeManagement.Import(second, changed, ThemeImportResolution.Overwrite);
      Assert.Equal(3, replaced.UserThemes.Length);
      Assert.Equal(changed.Json, ThemeManagement.All(replaced).Single(theme => string.Equals(theme.Name, custom.Name, StringComparison.Ordinal)).Json);
      AppSettings revived = ThemeManagement.Import(replaced, ThemeCatalog.Find("Dracula"));
      Assert.Empty(revived.DeletedBuiltInThemes);
      Assert.Contains(ThemeManagement.All(revived), theme => string.Equals(theme.Name, "Dracula", StringComparison.Ordinal));
      Assert.Throws<ArgumentOutOfRangeException>(() => ThemeManagement.Import(settings, nord, (ThemeImportResolution)99));
      Assert.Throws<ArgumentNullException>(() => ThemeManagement.Import(settings, null!));
   }



   /// <summary>New/copy editor drafts use unique bounded names; cancelled copies retain disabled state and original metadata.</summary>
   [Fact]
   public void NewAndCopy_ExistingOrLongNames_ProduceIndependentValidatedDrafts()
   {
      var settings = new AppSettings();
      ReaderTheme created = ThemeManagement.NewTheme(settings);
      Assert.Equal("New theme", created.Name);
      AppSettings first = ThemeManagement.Import(settings, created);
      Assert.Equal("New theme (2)", ThemeManagement.NewTheme(first).Name);
      ReaderTheme original = ThemeCatalog.Find("Nord");
      ReaderTheme copy = ThemeManagement.CopyTheme(first, original);
      Assert.Equal("Nord Copy", copy.Name);
      AppSettings next = ThemeManagement.Import(first, copy);
      Assert.Equal("Nord Copy (2)", ThemeManagement.CopyTheme(next, original).Name);
      ReaderTheme longName = original.Rename(new string('x', 80));
      Assert.Equal(80, ThemeManagement.CopyTheme(settings, longName).Name.Length);
      Assert.Empty(settings.UserThemes);
      Assert.Throws<ArgumentNullException>(() => ThemeManagement.CopyTheme(settings, null!));
   }



   /// <summary>Native editor renaming cannot silently overwrite another palette, while in-place edits select/enable a copied draft.</summary>
   [Fact]
   public void SaveEdited_NameConflictAndDisabledChoice_PreservesOriginal()
   {
      ReaderTheme original = ThemeCatalog.Find("Nord");
      var settings = new AppSettings { DisabledThemes = ["Nord"] };
      Assert.Throws<JsonException>(() => ThemeManagement.SaveEdited(settings, original, original.Rename("Dracula")));
      AppSettings saved = ThemeManagement.SaveEdited(settings, original, original);
      Assert.Equal("Nord", saved.DocumentTheme);
      Assert.Empty(saved.DisabledThemes);
      Assert.Equal(original.Json, Assert.Single(saved.UserThemes));
      Assert.Empty(settings.UserThemes);
      Assert.Contains("Nord", settings.DisabledThemes, StringComparer.Ordinal);
      Assert.Throws<ArgumentNullException>(() => ThemeManagement.SaveEdited(settings, null!, original));
      Assert.Throws<ArgumentNullException>(() => ThemeManagement.SaveEdited(settings, original, null!));
      Assert.Throws<ArgumentNullException>(() => ThemeManagement.ValidateEditName("Nord", "Nord", null!));
   }



   /// <summary>Import/export suggestions cannot turn a theme name into a Windows path or reserved device target.</summary>
   [Theory]
   [InlineData("GitHub Light", "github-light.json")]
   [InlineData("Owned < > : \" / \\ | ? *", "owned-_-_-_-_-_-_-_-_-_.json")]
   [InlineData("CON", "theme-con.json")]
   [InlineData("con.extra", "theme-con.extra.json")]
   [InlineData("LPT1", "theme-lpt1.json")]
   [InlineData("COM9", "theme-com9.json")]
   [InlineData("com10", "com10.json")]
   [InlineData("...", "theme.json")]
   [InlineData("COM¹", "theme-com¹.json")]
   [InlineData("COM²", "theme-com².json")]
   [InlineData("COM³", "theme-com³.json")]
   [InlineData("LPT¹", "theme-lpt¹.json")]
   [InlineData("LPT²", "theme-lpt².json")]
   [InlineData("LPT³", "theme-lpt³.json")]
   public void SuggestedFileName_UnsafeOrReserved_SuggestsSafeJson(string name, string expected)
      => Assert.Equal(expected, ThemeJson.SuggestedFileName(ThemeCatalog.Find(null).Rename(name)));



   /// <summary>Null filename input is a contract error rather than a native picker target.</summary>
   [Fact]
   public void SuggestedFileName_Null_RejectsInput() => Assert.Throws<ArgumentNullException>(() => ThemeJson.SuggestedFileName(null!));
}
