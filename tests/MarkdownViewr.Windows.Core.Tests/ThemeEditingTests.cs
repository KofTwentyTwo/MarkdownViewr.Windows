/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;
using System.Text.Json.Nodes;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Native theme drafts cannot mutate originals, lose schema fields or bypass the shared CSS validation.</summary>
public sealed class ThemeEditingTests
{
   /// <summary>All typography and palette edits persist together while unrelated metadata and the original remain intact.</summary>
   [Fact]
   public void Edit_AllTypographyAndPalette_PreservesSchemaAndOriginal()
   {
      JsonNode source = JsonNode.Parse(ThemeCatalog.Find(null).Json)!;
      source["metadata"] = new JsonObject { ["author"] = "Owned fixture", ["note"] = "Keep this" };
      var original = ReaderTheme.Parse(source.ToJsonString());
      string originalJson = original.Json;
      var fonts = new Dictionary<string, string>(StringComparer.Ordinal)
      {
         ["body"] = " Consolas ",
         ["heading"] = " Segoe UI ",
         ["code"] = " Cascadia Code ",
      };
      var sizes = new Dictionary<string, double>(StringComparer.Ordinal)
      {
         ["baseFontSize"] = 24,
         ["h1Size"] = 56,
         ["h2Size"] = 48,
         ["h3Size"] = 44,
         ["h4Size"] = 40,
         ["h5Size"] = 38,
         ["h6Size"] = 36,
         ["codeFontSize"] = 20,
         ["lineHeight"] = 2,
      };
      var colors = new Dictionary<string, string>(StringComparer.Ordinal) { ["background"] = " #123456 ", ["highlightBackground"] = "#654321", ["highlightText"] = "#eeeeee" };
      ReaderTheme edited = original.Edit(" Complete typography ", fonts, sizes, colors, "p { letter-spacing: 1px; }");
      Assert.Equal(originalJson, original.Json);
      Assert.Equal("Complete typography", edited.Name);
      using var result = JsonDocument.Parse(edited.Json);
      Assert.Equal("Owned fixture", result.RootElement.GetProperty("metadata").GetProperty("author").GetString());
      foreach((string key, string value) in fonts)
      {
         Assert.Equal(value.Trim(), result.RootElement.GetProperty("fonts").GetProperty(key).GetString());
      }
      foreach((string key, double value) in sizes)
      {
         Assert.Equal(value, result.RootElement.GetProperty("sizes").GetProperty(key).GetDouble());
         Assert.Contains("--" + key + ":" + value + (string.Equals(key, "lineHeight", StringComparison.Ordinal) ? ";" : "px;"), edited.Styles, StringComparison.Ordinal);
      }
      foreach((string key, string value) in colors)
      {
         Assert.Equal(value.Trim(), result.RootElement.GetProperty("colors").GetProperty(key).GetString());
      }
      Assert.Contains("p { letter-spacing: 1px; }", edited.Styles, StringComparison.Ordinal);
   }



   /// <summary>Partial drafts preserve every unedited typography and palette field rather than replacing schema groups.</summary>
   [Fact]
   public void Edit_PartialDraft_PreservesUneditedFields()
   {
      ReaderTheme original = ThemeCatalog.Find("Nord");
      ReaderTheme edited = original.Edit(original.Name, new Dictionary<string, string>(StringComparer.Ordinal) { ["body"] = "Consolas" }, new Dictionary<string, double>(StringComparer.Ordinal), new Dictionary<string, string>(StringComparer.Ordinal), "");
      using var before = JsonDocument.Parse(original.Json);
      using var after = JsonDocument.Parse(edited.Json);
      Assert.Equal(before.RootElement.GetProperty("fonts").GetProperty("heading").GetString(), after.RootElement.GetProperty("fonts").GetProperty("heading").GetString());
      Assert.True(JsonElement.DeepEquals(before.RootElement.GetProperty("sizes"), after.RootElement.GetProperty("sizes")));
      Assert.True(JsonElement.DeepEquals(before.RootElement.GetProperty("colors"), after.RootElement.GetProperty("colors")));
   }



   /// <summary>Invalid numeric drafts fail before saving and leave the bundled original usable for a corrected retry.</summary>
   [Theory]
   [InlineData("baseFontSize", 5)]
   [InlineData("h1Size", 97)]
   [InlineData("lineHeight", 0.4)]
   [InlineData("lineHeight", 4.1)]
   [InlineData("codeFontSize", double.NaN)]
   [InlineData("h2Size", double.PositiveInfinity)]
   [InlineData("h3Size", double.NegativeInfinity)]
   public void Edit_InvalidSize_RejectsWithoutMutatingOriginal(string key, double value)
   {
      ReaderTheme original = ThemeCatalog.Find(null);
      string before = original.Json;
      Assert.Throws<JsonException>(() => original.Edit("Draft", new Dictionary<string, string>(StringComparer.Ordinal), new Dictionary<string, double>(StringComparer.Ordinal) { [key] = value }, new Dictionary<string, string>(StringComparer.Ordinal), ""));
      Assert.Equal(before, original.Json);
      Assert.Equal("Corrected", original.Edit("Corrected", new Dictionary<string, string>(StringComparer.Ordinal), new Dictionary<string, double>(StringComparer.Ordinal), new Dictionary<string, string>(StringComparer.Ordinal), "").Name);
   }



   /// <summary>Unknown or case-confused fields cannot silently bypass the known schema and later disappear at rendering time.</summary>
   [Theory]
   [InlineData("fonts")]
   [InlineData("sizes")]
   [InlineData("colors")]
   public void Edit_UnknownField_RejectsDraft(string group)
   {
      var fonts = new Dictionary<string, string>(StringComparer.Ordinal);
      var sizes = new Dictionary<string, double>(StringComparer.Ordinal);
      var colors = new Dictionary<string, string>(StringComparer.Ordinal);
      if(string.Equals(group, "fonts", StringComparison.Ordinal))
      {
         fonts.Add("Body", "Consolas");
      }
      else if(string.Equals(group, "sizes", StringComparison.Ordinal))
      {
         sizes.Add("BaseFontSize", 18);
      }
      else
      {
         colors.Add("Background", "#123456");
      }
      Assert.Equal(group, Assert.Throws<ArgumentException>(() => ThemeCatalog.Find(null).Edit("Draft", fonts, sizes, colors, "")).ParamName);
   }



   /// <summary>Null draft inputs report explicit argument errors rather than damaging a saved theme or crashing the editor.</summary>
   [Theory]
   [InlineData("name")]
   [InlineData("fonts")]
   [InlineData("sizes")]
   [InlineData("colors")]
   [InlineData("customCss")]
   public void Edit_NullDraft_ReportsExplicitContract(string field)
   {
      var fonts = new Dictionary<string, string>(StringComparer.Ordinal);
      var sizes = new Dictionary<string, double>(StringComparer.Ordinal);
      var colors = new Dictionary<string, string>(StringComparer.Ordinal);
      ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => ThemeCatalog.Find(null).Edit(
         string.Equals(field, "name", StringComparison.Ordinal) ? null! : "Draft",
         string.Equals(field, "fonts", StringComparison.Ordinal) ? null! : fonts,
         string.Equals(field, "sizes", StringComparison.Ordinal) ? null! : sizes,
         string.Equals(field, "colors", StringComparison.Ordinal) ? null! : colors,
         string.Equals(field, "customCss", StringComparison.Ordinal) ? null! : ""));
      Assert.Equal(field, exception.ParamName);
   }



   /// <summary>Edited font/CSS strings pass through the existing HTML-escape boundary; authored color values still require hex notation.</summary>
   [Fact]
   public void Edit_HostileStrings_PreservesStyleBoundary()
   {
      ReaderTheme original = ThemeCatalog.Find(null);
      var fonts = new Dictionary<string, string>(StringComparer.Ordinal) { ["body"] = "Font';} <script>" };
      ReaderTheme edited = original.Edit("Safe", fonts, new Dictionary<string, double>(StringComparer.Ordinal), new Dictionary<string, string>(StringComparer.Ordinal), "</style><script>evil()</script>");
      Assert.DoesNotContain("<", edited.Styles, StringComparison.Ordinal);
      Assert.Contains("\\3c /style>", edited.Styles, StringComparison.Ordinal);
      Assert.Throws<JsonException>(() => original.Edit("Invalid", fonts, new Dictionary<string, double>(StringComparer.Ordinal), new Dictionary<string, string>(StringComparer.Ordinal) { ["background"] = "red" }, ""));
   }
}
