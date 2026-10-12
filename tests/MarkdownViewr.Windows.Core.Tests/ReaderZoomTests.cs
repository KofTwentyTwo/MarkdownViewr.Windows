/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using System.Text.Json;
using MarkdownViewr.Windows.Core.Navigation;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Verifies window-local bounded state and real bundled theme font scaling without changing persisted source themes.</summary>
public sealed class ReaderZoomTests
{
   /// <summary>Repeated commands stay bounded, actual size resets, and another window retains its independent state.</summary>
   [Fact]
   public void Apply_RepeatedAndIndependentCommands_RetainsBoundsAndActualSize()
   {
      var zoom = new ReaderZoom();
      var other = new ReaderZoom();
      Assert.Equal(1, zoom.Scale);
      zoom.Apply(ReaderCommand.None);
      Assert.Equal(1, zoom.Scale);
      zoom.Apply(ReaderCommand.ZoomIn);
      Assert.Equal(1.1, zoom.Scale);
      zoom.Apply(ReaderCommand.ZoomOut);
      Assert.Equal(1, zoom.Scale, precision: 12);
      for(int index = 0; index < 100; index++)
      {
         zoom.Apply(ReaderCommand.ZoomIn);
      }
      Assert.Equal(5, zoom.Scale);
      for(int index = 0; index < 100; index++)
      {
         zoom.Apply(ReaderCommand.ZoomOut);
      }
      Assert.Equal(0.3, zoom.Scale);
      zoom.Apply(ReaderCommand.ZoomReset);
      Assert.Equal(1, zoom.Scale);
      Assert.Equal(1, other.Scale);
   }



   /// <summary>Every bundled theme scales all eight font sizes but retains line height, original JSON and actual-size CSS.</summary>
   [Theory]
   [InlineData(0.3)]
   [InlineData(1.1)]
   [InlineData(5)]
   public void GetStyles_BundledThemes_ScalesFontsWithInvariantNumbers(double scale)
   {
      CultureInfo previous = CultureInfo.CurrentCulture;
      try
      {
         CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
         foreach(ReaderTheme theme in ThemeCatalog.BuiltIn)
         {
            string original = theme.Json;
            using var json = JsonDocument.Parse(original);
            JsonElement sizes = json.RootElement.GetProperty("sizes");
            string styles = theme.GetStyles(scale);
            foreach(string field in new[] { "baseFontSize", "h1Size", "h2Size", "h3Size", "h4Size", "h5Size", "h6Size", "codeFontSize" })
            {
               string number = (sizes.GetProperty(field).GetDouble() * scale).ToString("R", CultureInfo.InvariantCulture);
               Assert.Contains("--" + field + ":" + number + "px;", styles, StringComparison.Ordinal);
            }
            Assert.Contains("--lineHeight:" + sizes.GetProperty("lineHeight").GetDouble().ToString("R", CultureInfo.InvariantCulture) + ";", styles, StringComparison.Ordinal);
            Assert.Equal(theme.Styles, theme.GetStyles(1));
            Assert.Equal(original, theme.Json);
         }
      }
      finally
      {
         CultureInfo.CurrentCulture = previous;
      }
   }



   /// <summary>Nonfinite and out-of-range scales cannot become generated browser CSS.</summary>
   [Theory]
   [InlineData(double.NaN)]
   [InlineData(double.PositiveInfinity)]
   [InlineData(double.NegativeInfinity)]
   [InlineData(0)]
   [InlineData(0.299)]
   [InlineData(5.001)]
   public void GetStyles_InvalidScale_RejectsInput(double scale)
      => Assert.Throws<ArgumentOutOfRangeException>(() => ThemeCatalog.Find(null).GetStyles(scale));



   /// <summary>Ctrl zoom commands own only reading focus and their allowed shift combinations.</summary>
   [Theory]
   [InlineData('+', false, ReaderCommand.ZoomIn)]
   [InlineData('+', true, ReaderCommand.ZoomIn)]
   [InlineData('=', false, ReaderCommand.ZoomIn)]
   [InlineData('=', true, ReaderCommand.None)]
   [InlineData('-', false, ReaderCommand.ZoomOut)]
   [InlineData('-', true, ReaderCommand.None)]
   [InlineData('0', false, ReaderCommand.ZoomReset)]
   [InlineData('0', true, ReaderCommand.None)]
   public void Resolve_ZoomModifiersAndFocus_PreservesNativeTextInput(char character, bool shift, ReaderCommand expected)
   {
      var key = new ReaderKey(character, Control: true, Shift: shift);
      Assert.Equal(expected, new ReaderKeyboard().Resolve(key, readingFocused: true, 0));
      Assert.Equal(ReaderCommand.None, new ReaderKeyboard().Resolve(key, readingFocused: false, 0));
      Assert.Equal(ReaderCommand.None, new ReaderKeyboard().Resolve(key with { Alt = true }, readingFocused: true, 0));
      Assert.Equal(ReaderCommand.None, new ReaderKeyboard().Resolve(key with { Windows = true }, readingFocused: true, 0));
   }
}
