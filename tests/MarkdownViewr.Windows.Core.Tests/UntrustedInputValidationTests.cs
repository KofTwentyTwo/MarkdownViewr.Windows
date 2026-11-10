/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;
using System.Text.Json.Nodes;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using MarkdownViewr.Windows.Core.Documents;
using MarkdownViewr.Windows.Core.Editors;
using MarkdownViewr.Windows.Core.Printing;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>
/// Validation of text that comes from a document, a settings file, an imported theme or the
/// browser surface: each check is pinned by an input that only that check rejects.
/// </summary>
public sealed class UntrustedInputValidationTests
{
   /// <summary>Metadata keys and values are display text: markup in either is escaped, never parsed.</summary>
   [Fact]
   public void RenderMetadata_MarkupInKeyAndValue_IsEscapedText()
   {
      const string Key = "<b>key</b>";
      const string Value = "<img src=x onerror=run()> & \"quoted\"";
      using IHtmlDocument parsed = new HtmlParser().ParseDocument(DocumentFrontmatter.RenderMetadata(Key + ": " + Value));
      Assert.Equal(Key, Assert.Single(parsed.QuerySelectorAll("th.fm-key")).TextContent);
      Assert.Equal(Value, Assert.Single(parsed.QuerySelectorAll("td.fm-value")).TextContent);
      Assert.Empty(parsed.QuerySelectorAll("b,img"));
   }



   /// <summary>A line that starts with its colon still has a key cell, so the table keeps two columns.</summary>
   [Fact]
   public void RenderMetadata_LeadingColon_KeepsAnEmptyKeyCell()
   {
      using IHtmlDocument parsed = new HtmlParser().ParseDocument(DocumentFrontmatter.RenderMetadata(": value"));
      Assert.Equal("", Assert.Single(parsed.QuerySelectorAll("th.fm-key")).TextContent);
      IElement value = Assert.Single(parsed.QuerySelectorAll("td.fm-value"));
      Assert.Equal("value", value.TextContent);
      Assert.False(value.HasAttribute("colspan"));
   }



   /// <summary>The private source marker is new for every render, so an authored attribute cannot predict it.</summary>
   [Fact]
   public void AttributeName_EachSourceMap_IsUnpredictable()
   {
      string first = new DocumentSourceMap("# Title").AttributeName;
      string second = new DocumentSourceMap("# Title").AttributeName;
      Assert.Matches("^data-mv-source-[0-9a-f]{32}$", first);
      Assert.Matches("^data-mv-source-[0-9a-f]{32}$", second);
      Assert.NotEqual(first, second, StringComparer.Ordinal);
   }



   /// <summary>Raw HTML headings map to their source at every level, including the sixth.</summary>
   [Theory]
   [InlineData(1)]
   [InlineData(5)]
   [InlineData(6)]
   public void SourceHeadings_RawHtmlHeadingLevel_MapsItsSource(int level)
   {
      string heading = $"<h{level}>Raw</h{level}>";
      string source = "Intro\n\n" + heading + "\n\nAfter\n";
      var outline = new DocumentOutline(source);
      Assert.Equal(level, Assert.Single(outline.Headings).Level);
      DocumentSourceHeading mapped = Assert.Single(outline.SourceHeadings);
      Assert.Equal(heading, source[mapped.Start..mapped.End]);
   }



   /// <summary>A level the HTML standard does not define is not a heading and gets no source mapping.</summary>
   [Fact]
   public void SourceHeadings_RawHtmlLevelSeven_IsNotAHeading()
   {
      var outline = new DocumentOutline("<h7>Not a heading</h7>\n");
      Assert.Empty(outline.Headings);
      Assert.Empty(outline.SourceHeadings);
   }



   /// <summary>Loading repairs every stored list and object, not only the ones the window happens to read first.</summary>
   [Fact]
   public void Sanitize_CorruptPrintEditorsAndThemeLists_RepairsEachProperty()
   {
      var valid = new EditorConfiguration(Guid.NewGuid(), "Editor", @"C:\Editor\editor.exe");
      var settings = new AppSettings
      {
         Print = new PrintOptions(Theme: "Bad\u0001Theme", PaddingPixels: 5000),
         ConfiguredEditors = [valid, new EditorConfiguration(Guid.NewGuid(), "Script", @"C:\Editor\run.cmd"), new EditorConfiguration(Guid.Empty, "Empty", @"C:\Editor\other.exe")],
         DisabledThemes = ["Nord", "Nord", "Bad\u0001Name", ""],
         DeletedBuiltInThemes = ["Dracula", "Dracula", "Bad\u0002Name"],
         ThemeOrder = ["Nord", "Nord", "Bad\u0003Name"],
         DocumentTheme = "Bad\u0001Theme",
      };
      settings.Sanitize();
      Assert.Equal("Clean Printing", settings.Print.Theme);
      Assert.Equal(96, settings.Print.PaddingPixels);
      Assert.Equal([valid], settings.ConfiguredEditors);
      Assert.Equal(valid.Executable, settings.ExternalEditor);
      Assert.Equal(["Nord"], settings.DisabledThemes);
      Assert.Equal(["Dracula"], settings.DeletedBuiltInThemes);
      Assert.Equal(["Nord"], settings.ThemeOrder);
      Assert.Equal("Catppuccin Mocha", settings.DocumentTheme);
   }



   /// <summary>A missing print object, as an older file or a hand edit leaves it, becomes the defaults.</summary>
   [Fact]
   public void Sanitize_MissingPrintObject_UsesDefaults()
   {
      var settings = new AppSettings { Print = null! };
      settings.Sanitize();
      Assert.Equal(new PrintOptions(), settings.Print);
   }



   /// <summary>Editing a theme cannot silently raise it to a newer schema; that needs the import confirmation.</summary>
   [Fact]
   public void SaveEdited_SchemaVersionRaised_RequiresConfirmation()
   {
      ReaderTheme original = ThemeCatalog.Find(null);
      JsonNode raised = JsonNode.Parse(original.Json)!;
      raised["schemaVersion"] = 2;
      var edited = ReaderTheme.ParseCompatible(raised.ToJsonString());
      Assert.Equal(1, original.SchemaVersion);
      Assert.Equal(2, edited.SchemaVersion);
      Assert.Throws<InvalidOperationException>(() => ThemeManagement.SaveEdited(new AppSettings(), original, edited));
   }



   /// <summary>An already confirmed newer-schema theme stays editable at its own version.</summary>
   [Fact]
   public void SaveEdited_SameNewerSchema_IsSaved()
   {
      JsonNode newer = JsonNode.Parse(ThemeCatalog.Find(null).Json)!;
      newer["schemaVersion"] = 2;
      newer["name"] = "Owned future";
      var theme = ReaderTheme.ParseCompatible(newer.ToJsonString());
      AppSettings imported = ThemeManagement.Import(new AppSettings(), theme, approveNewerSchema: true);
      AppSettings saved = ThemeManagement.SaveEdited(imported, theme, theme);
      Assert.Equal("Owned future", saved.DocumentTheme);
      Assert.Equal(2, ThemeManagement.Active(saved).SchemaVersion);
   }



   /// <summary>A copy of a theme whose name is already at the limit still has a name the theme parser accepts.</summary>
   [Fact]
   public void CopyTheme_LongestNameTakenTwice_StaysWithinTheNameLimit()
   {
      ReaderTheme longest = ThemeCatalog.Find(null).Rename(new string('N', 80));
      AppSettings settings = ThemeManagement.Import(new AppSettings(), longest);
      ReaderTheme firstCopy = ThemeManagement.CopyTheme(settings, longest);
      settings = ThemeManagement.Import(settings, firstCopy);
      ReaderTheme secondCopy = ThemeManagement.CopyTheme(settings, longest);
      Assert.Equal(80, firstCopy.Name.Length);
      Assert.Equal(80, secondCopy.Name.Length);
      Assert.EndsWith(" (2)", secondCopy.Name, StringComparison.Ordinal);
      Assert.Equal(secondCopy.Name, ReaderTheme.ParseCompatible(secondCopy.Json).Name);
   }



   /// <summary>Editor and document paths are accepted up to the Windows limit and refused one character past it.</summary>
   [Fact]
   public void IsExecutablePath_PathAtAndPastTheWindowsLimit_IsBoundedExactly()
   {
      string atLimit = @"C:\" + new string('e', 32767 - 7) + ".exe";
      string pastLimit = @"C:\" + new string('e', 32768 - 7) + ".exe";
      Assert.Equal(32767, atLimit.Length);
      Assert.True(EditorLaunch.IsExecutablePath(atLimit));
      Assert.False(EditorLaunch.IsExecutablePath(pastLimit));
      string document = @"C:\" + new string('d', 32767 - 6) + ".md";
      Assert.Equal(32767, document.Length);
      Assert.Equal(document, Assert.Single(EditorLaunch.Create(@"C:\Editor\editor.exe", document).ArgumentList));
      Assert.Throws<ArgumentException>(() => EditorLaunch.Create(@"C:\Editor\editor.exe", document + "x"));
   }



   /// <summary>Recent paths are kept up to the Windows limit and dropped one character past it.</summary>
   [Fact]
   public void Sanitize_RecentPathAtAndPastTheWindowsLimit_IsBoundedExactly()
   {
      string atLimit = @"C:\" + new string('d', 32767 - 6) + ".md";
      Assert.Equal([atLimit], RecentDocuments.Sanitize([atLimit, atLimit + "x"]));
   }



   /// <summary>A find term is accepted at its limit and refused one character past it.</summary>
   [Fact]
   public void Constructor_FindTermAtAndPastTheLimit_IsBoundedExactly()
   {
      string atLimit = new('q', DocumentFind.MaximumQueryCharacters);
      Assert.Equal(atLimit, new DocumentFind(atLimit + " tail", atLimit).Query);
      Assert.Throws<ArgumentException>(() => new DocumentFind("source", atLimit + "q"));
   }



   /// <summary>The viewport result is read up to its size cap and ignored past it, even when it would parse.</summary>
   [Fact]
   public void FromBrowserResult_ViewportAtAndPastTheSizeCap_IsBoundedExactly()
   {
      string atCap = "[0.25," + new string(' ', 128 - 11) + "0.75]";
      Assert.Equal(128, atCap.Length);
      Assert.Equal(0.75, DocumentViewport.FromBrowserResult(atCap).VerticalFraction);
      Assert.Equal(default, DocumentViewport.FromBrowserResult(atCap.Replace("[", "[ ", StringComparison.Ordinal)));
   }



   /// <summary>The scroll-state result is read up to its size cap for the heading count and ignored past it.</summary>
   [Theory]
   [InlineData(0)]
   [InlineData(3)]
   public void FromBrowserResult_ScrollStateAtAndPastTheSizeCap_IsBoundedExactly(int headings)
   {
      int cap = headings * 128 + 512;
      string atCap = "[1,2,[],[]" + new string(' ', cap - 11) + "]";
      Assert.Equal(cap, atCap.Length);
      Assert.NotNull(DocumentScrollState.FromBrowserResult(atCap, headings));
      Assert.Null(DocumentScrollState.FromBrowserResult(atCap.Replace("[1,", "[ 1,", StringComparison.Ordinal), headings));
   }



   /// <summary>The scroll-state result has exactly four parts; a longer array is not the application's own query result.</summary>
   [Theory]
   [InlineData("[1,2,[]]")]
   [InlineData("[1,2,[],[],[]]")]
   [InlineData("[1,2,[],[],0]")]
   public void FromBrowserResult_ScrollStateWithWrongNumberOfParts_IsRejected(string json)
      => Assert.Null(DocumentScrollState.FromBrowserResult(json, 2));



   /// <summary>Print styles are built from repaired options, so a corrupt padding value cannot reach the stylesheet.</summary>
   [Theory]
   [InlineData(double.NaN, "padding:0px")]
   [InlineData(double.PositiveInfinity, "padding:0px")]
   [InlineData(5000, "padding:96px")]
   [InlineData(-3, "padding:0px")]
   public void Build_CorruptPadding_UsesTheRepairedValue(double padding, string expected)
   {
      ReaderTheme theme = ThemeCatalog.Find(null);
      string css = PrintStyles.Build(new PrintOptions(PaddingPixels: padding), theme, [theme], 1);
      Assert.Contains(expected, css, StringComparison.Ordinal);
      Assert.DoesNotContain("NaN", css, StringComparison.Ordinal);
      Assert.DoesNotContain("Infinity", css, StringComparison.Ordinal);
      Assert.DoesNotContain("∞", css, StringComparison.Ordinal);
   }



   /// <summary>
   /// Text the JSON reader cannot decode, an unpaired surrogate escape, makes the theme invalid
   /// wherever it appears: in a value the theme reads, in a property name, or in a value this
   /// version ignores but would later re-serialize. So does a repeated property name.
   /// </summary>
   [Theory]
   [InlineData("\"name\": \"Nord\"", "\"name\": \"\\uD83D\"")]
   [InlineData("\"background\": \"#2e3440\"", "\"background\": \"\\uD800\"")]
   [InlineData("\"body\": \"System\"", "\"body\": \"A\\uDC00B\"")]
   [InlineData("\"name\": \"Nord\"", "\"name\": \"Nord\", \"\\uD83D\": 1")]
   [InlineData("\"name\": \"Nord\"", "\"name\": \"Nord\", \"note\": \"\\uD83D\"")]
   [InlineData("\"name\": \"Nord\"", "\"name\": \"Nord\", \"notes\": [1, [\"ok\", {\"deep\": \"\\uDFFF\"}]]")]
   [InlineData("\"body\": \"System\"", "\"body\": \"System\", \"\\uD800x\": \"y\"")]
   [InlineData("\"name\": \"Nord\"", "\"name\": \"Nord\", \"name\": \"Nord\"")]
   [InlineData("\"body\": \"System\"", "\"body\": \"System\", \"body\": \"Other\"")]
   [InlineData("\"name\": \"Nord\"", "\"name\": \"Nord\", \"extra\": {\"a\": 1, \"a\": 2}")]
   public void Parse_UndecodableTextOrRepeatedProperty_IsRejectedAsAnInvalidTheme(string valid, string invalid)
   {
      string original = ThemeCatalog.Find("Nord").Json;
      Assert.Contains(valid, original, StringComparison.Ordinal);
      string json = original.Replace(valid, invalid, StringComparison.Ordinal);
      Assert.Throws<JsonException>(() => ReaderTheme.Parse(json));
      Assert.Throws<JsonException>(() => ReaderTheme.ParseCompatible(json));
      var settings = new AppSettings { UserThemes = [json, original] };
      settings.Sanitize();
      Assert.Equal([original], settings.UserThemes);
   }



   /// <summary>
   /// A theme that parses can be renamed, copied, edited and imported under a new name, whatever
   /// extra properties it carries: those operations re-serialize the whole document.
   /// </summary>
   [Fact]
   public void Parse_AcceptedThemeWithExtraProperties_SurvivesRenameCopyEditAndImport()
   {
      string json = ThemeCatalog.Find("Nord").Json.Replace("\"name\": \"Nord\"", "\"name\": \"Nord\", \"note\": \"caf\\u00e9 \\ud83d\\ude00\", \"future\": {\"list\": [1, \"two\", null]}", StringComparison.Ordinal);
      var theme = ReaderTheme.Parse(json);
      Assert.Equal("Renamed", theme.Rename("Renamed").Name);
      AppSettings imported = ThemeManagement.Import(new AppSettings(), theme);
      Assert.Contains(ThemeManagement.All(imported), item => item.Name.StartsWith("Nord (", StringComparison.Ordinal));
      Assert.EndsWith(" Copy", ThemeManagement.CopyTheme(imported, theme).Name, StringComparison.Ordinal);
      ReaderTheme edited = theme.Edit("Edited", new Dictionary<string, string>(StringComparer.Ordinal), new Dictionary<string, double>(StringComparer.Ordinal), new Dictionary<string, string>(StringComparer.Ordinal), "");
      Assert.Equal("Edited", edited.Name);
      Assert.Contains("caf", edited.Json, StringComparison.Ordinal);
   }



   /// <summary>Only the exact lower-case channel name selects the development identity.</summary>
   [Theory]
   [InlineData("DEV")]
   [InlineData("Dev")]
   [InlineData("dev ")]
   [InlineData("development")]
   public void ForChannel_OtherSpellingOfDev_KeepsTheStableIdentity(string channel)
      => Assert.Equal("markdownviewr-windows", AppIdentity.ForChannel(channel).Id);



   /// <summary>A colour that is not six hexadecimal digits after a hash, or a label containing a control character, is refused.</summary>
   [Theory]
   [InlineData("colors.background", "\";ffffff\"")]
   [InlineData("colors.background", "\"#fffffff\"")]
   [InlineData("colors.background", "\"#ffffffff\"")]
   [InlineData("colors.background", "\"#fffff\"")]
   [InlineData("colors.background", "\"ffffff#\"")]
   [InlineData("colors.text", "\"#12345g\"")]
   [InlineData("name", "\"Theme\\u0001Name\"")]
   [InlineData("name", "\"Theme\\u007fName\"")]
   [InlineData("fonts.heading", "\"Font\\u0001Name\"")]
   [InlineData("fonts.code", "\"Font\\u009bName\"")]
   public void Parse_MalformedColorOrLabel_IsRejected(string path, string json)
   {
      JsonNode root = JsonNode.Parse(ThemeCatalog.Find(null).Json)!;
      string[] parts = path.Split('.');
      JsonNode parent = parts.Length == 1 ? root : root[parts[0]]!;
      parent[parts[^1]] = JsonNode.Parse(json);
      Assert.Throws<JsonException>(() => ReaderTheme.Parse(root.ToJsonString()));
   }
}
