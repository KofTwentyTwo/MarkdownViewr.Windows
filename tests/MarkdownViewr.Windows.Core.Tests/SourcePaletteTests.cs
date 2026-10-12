/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json.Nodes;
using MarkdownViewr.Windows.Core.Documents;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Native source colors follow shell mode and document accents without unreadable contrast or theme mutation.</summary>
public sealed class SourcePaletteTests
{
   /// <summary>Every shipped document palette remains readable against both native source backgrounds.</summary>
   [Theory]
   [InlineData(false, "#FFFFFF", "#1F1F1F")]
   [InlineData(true, "#1E1E1E", "#D4D4D4")]
   public void Create_AllBuiltIns_ContrastsWithShellMode(bool dark, string background, string foreground)
   {
      foreach(ReaderTheme theme in ThemeCatalog.BuiltIn)
      {
         string original = theme.Json;
         var palette = new SourcePalette(theme, dark);
         Assert.Equal(background, palette.Background);
         Assert.Equal(foreground, palette.Foreground);
         foreach(SourceTokenKind kind in Enum.GetValues<SourceTokenKind>())
         {
            Assert.True(SourcePalette.Contrast(palette.Color(kind), palette.Background) >= 4.5);
         }
         Assert.Equal(original, theme.Json);
      }
   }



   /// <summary>Readable custom accents are preserved; matching-background accents blend toward the readable foreground.</summary>
   [Theory]
   [InlineData(false, "#000000", "#FFFFFF")]
   [InlineData(true, "#FFFFFF", "#1E1E1E")]
   public void Create_CustomAccents_PreservesOrAdjustsColor(bool dark, string readable, string unreadable)
   {
      JsonNode json = JsonNode.Parse(ThemeCatalog.Find(null).Json)!;
      json["colors"]!["heading1"] = readable;
      json["colors"]!["link"] = unreadable;
      var palette = new SourcePalette(ReaderTheme.Parse(json.ToJsonString()), dark);
      Assert.Equal(readable, palette.Color(SourceTokenKind.Heading));
      Assert.NotEqual(unreadable, palette.Color(SourceTokenKind.Link), StringComparer.Ordinal);
      Assert.True(SourcePalette.Contrast(palette.Color(SourceTokenKind.Link), palette.Background) >= 4.5);
      Assert.Throws<KeyNotFoundException>(() => palette.Color((SourceTokenKind)999));
      Assert.Throws<ArgumentNullException>(() => new SourcePalette(null!, dark));
   }



   /// <summary>Contrast uses the standard sRGB luminance extremes and is independent of color ordering.</summary>
   [Fact]
   public void Contrast_ReferenceColors_MatchesKnownRatios()
   {
      Assert.Equal(21, SourcePalette.Contrast("#000000", "#FFFFFF"), 8);
      Assert.Equal(21, SourcePalette.Contrast("#FFFFFF", "#000000"), 8);
      Assert.Equal(1, SourcePalette.Contrast("#123456", "#123456"), 8);
   }
}
