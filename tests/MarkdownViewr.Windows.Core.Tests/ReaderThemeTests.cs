/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Exercises the original themes, schema errors and CSS/HTML boundary with hostile theme input.</summary>
public sealed class ReaderThemeTests
{
   /// <summary>The complete bundled collection is usable and retains Darin's default.</summary>
   [Fact]
   public void BuiltIn_OriginalResources_LoadsTwentyThemes()
   {
      Assert.Equal(20, ThemeCatalog.BuiltIn.Count);
      Assert.Equal("Catppuccin Mocha", ThemeCatalog.Find(null).Name);
      Assert.Equal("Catppuccin Mocha", ThemeCatalog.Find("removed theme").Name);
      Assert.Equal("GitHub Light", ThemeCatalog.Find("GitHub Light").Name);
      Assert.Equal(ThemeCatalog.BuiltIn.Select(theme => theme.Name).Order(StringComparer.Ordinal), ThemeCatalog.BuiltIn.Select(theme => theme.Name), StringComparer.Ordinal);
      Assert.All(ThemeCatalog.BuiltIn, theme => Assert.Contains("var(--background)", theme.Styles, StringComparison.Ordinal));
   }



   /// <summary>Color, font and size values become CSS variables using invariant numbers.</summary>
   [Fact]
   public void Parse_OriginalTheme_PreservesPaletteAndMapsFonts()
   {
      CultureInfo previous = CultureInfo.CurrentCulture;
      try
      {
         CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
         var theme = ReaderTheme.Parse(OriginalJson());
         Assert.Equal("Catppuccin Mocha", theme.Name);
         Assert.Contains("--background:#1e1e2e;", theme.Styles, StringComparison.Ordinal);
         Assert.Contains("--bodyFont:system-ui, sans-serif;", theme.Styles, StringComparison.Ordinal);
         Assert.Contains("--codeFont:Consolas, 'Cascadia Code', monospace;", theme.Styles, StringComparison.Ordinal);
         Assert.Contains("--baseFontSize:14px;", theme.Styles, StringComparison.Ordinal);
         Assert.Contains("--lineHeight:1.7;", theme.Styles, StringComparison.Ordinal);
      }
      finally
      {
         CultureInfo.CurrentCulture = previous;
      }
   }



   /// <summary>Custom font and CSS values cannot escape the containing style element or inject font declarations.</summary>
   [Fact]
   public void Parse_CustomStrings_EscapesHtmlAndFontDelimiters()
   {
      JsonNode root = JsonNode.Parse(OriginalJson())!;
      root["schemaVersion"] = 1;
      root["fonts"]!["body"] = "A';} <script>";
      root["customCSS"] = "body { color: red; } </style><script>alert(1)</script>";
      string styles = ReaderTheme.Parse(root.ToJsonString()).Styles;
      Assert.DoesNotContain("<", styles, StringComparison.Ordinal);
      Assert.Contains("--bodyFont:'\\41 \\27 \\3B \\7D \\20 \\3C \\73 \\63 \\72 \\69 \\70 \\74 \\3E ';", styles, StringComparison.Ordinal);
      Assert.Contains("\\3c /style>", styles, StringComparison.Ordinal);
   }



   /// <summary>Optional custom CSS is not required by older theme files.</summary>
   [Fact]
   public void Parse_NoCustomCss_LoadsTheme()
   {
      JsonNode root = JsonNode.Parse(OriginalJson())!;
      root.AsObject().Remove("customCSS");
      Assert.Equal("Catppuccin Mocha", ReaderTheme.Parse(root.ToJsonString()).Name);
   }



   /// <summary>Malformed JSON, oversized files and non-object roots fail before generating CSS.</summary>
   [Theory]
   [InlineData("null")]
   [InlineData("[]")]
   [InlineData("{")]
   public void Parse_InvalidRoot_RejectsInput(string json) => Assert.ThrowsAny<JsonException>(() => ReaderTheme.Parse(json));



   /// <summary>Null and oversized input are rejected before parser work.</summary>
   [Fact]
   public void Parse_OutsideSizeContract_RejectsInput()
   {
      Assert.Throws<ArgumentNullException>(() => ReaderTheme.Parse(null!));
      Assert.Throws<JsonException>(() => ReaderTheme.Parse(new string(' ', 128 * 1024 + 1)));
   }



   /// <summary>Required objects, strings, version, numeric bounds and colors cannot carry invalid schema values.</summary>
   [Theory]
   [InlineData("schemaVersion", "2")]
   [InlineData("schemaVersion", "1.5")]
   [InlineData("schemaVersion", "\"1\"")]
   [InlineData("name", "\"\"")]
   [InlineData("name", "\"\\n\"")]
   [InlineData("name", "null")]
   [InlineData("colors", "null")]
   [InlineData("colors.background", "\"red\"")]
   [InlineData("colors.background", "\"#zzzzzz\"")]
   [InlineData("colors.background", "\";color:red\"")]
   [InlineData("fonts.body", "\"\"")]
   [InlineData("fonts.code", "\"\\n\"")]
   [InlineData("sizes.baseFontSize", "5")]
   [InlineData("sizes.h1Size", "97")]
   [InlineData("sizes.h2Size", "1e400")]
   [InlineData("sizes.h3Size", "\"20\"")]
   [InlineData("sizes.lineHeight", "0.4")]
   [InlineData("sizes.lineHeight", "4.1")]
   [InlineData("customCSS", "false")]
   public void Parse_InvalidProperty_RejectsInput(string path, string json)
   {
      JsonNode root = JsonNode.Parse(OriginalJson())!;
      string[] parts = path.Split('.');
      JsonNode parent = parts.Length == 1 ? root : root[parts[0]]!;
      parent[parts[^1]] = JsonNode.Parse(json);
      Assert.Throws<JsonException>(() => ReaderTheme.Parse(root.ToJsonString()));
   }



   /// <summary>Missing fields never reach unchecked JSON property access.</summary>
   [Theory]
   [InlineData("name")]
   [InlineData("colors")]
   [InlineData("fonts")]
   [InlineData("sizes")]
   [InlineData("colors.text")]
   [InlineData("sizes.h4Size")]
   public void Parse_MissingProperty_RejectsInput(string path)
   {
      JsonNode root = JsonNode.Parse(OriginalJson())!;
      string[] parts = path.Split('.');
      JsonNode parent = parts.Length == 1 ? root : root[parts[0]]!;
      parent.AsObject().Remove(parts[^1]);
      Assert.Throws<JsonException>(() => ReaderTheme.Parse(root.ToJsonString()));
   }



   /// <summary>Names and font families cannot produce unbounded CSS or inaccessible labels.</summary>
   [Theory]
   [InlineData("name")]
   [InlineData("fonts.body")]
   public void Parse_OverlongLabel_RejectsInput(string path)
   {
      JsonNode root = JsonNode.Parse(OriginalJson())!;
      string[] parts = path.Split('.');
      JsonNode parent = parts.Length == 1 ? root : root[parts[0]]!;
      parent[parts[^1]] = new string('A', 81);
      Assert.Throws<JsonException>(() => ReaderTheme.Parse(root.ToJsonString()));
   }



   /// <summary>Loads the exact bundled reference instead of maintaining a duplicate test schema.</summary>
   private static string OriginalJson()
   {
      using Stream stream = typeof(ReaderTheme).Assembly.GetManifestResourceStream("MarkdownViewr.Windows.Core.Themes.BuiltIn.catppuccin-mocha.json")!;
      using var reader = new StreamReader(stream);
      return reader.ReadToEnd();
   }
}
