/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using System.Text.Json;
using MarkdownViewr.Windows.Core.Printing;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Locks reference print defaults, bounded saved choices and independent reading/print theme generation.</summary>
public sealed class PrintStylesTests
{
   /// <summary>Default output saves ink, retains images and uses the reference's clean fonts without screen zoom.</summary>
   [Fact]
   public void Build_DefaultPrint_UsesCleanPaletteAndReferenceSizes()
   {
      var options = new PrintOptions();
      ReaderTheme active = ThemeCatalog.Find("Catppuccin Mocha");
      string before = active.Json;
      string css = PrintStyles.Build(options, active, ThemeCatalog.BuiltIn, 5);
      Assert.Contains("--background:#ffffff;", css, StringComparison.Ordinal);
      Assert.Contains("--h1Size:28px;", css, StringComparison.Ordinal);
      Assert.Contains("--codeFontSize:13px;", css, StringComparison.Ordinal);
      Assert.Contains("body{padding:0px!important;}", css, StringComparison.Ordinal);
      Assert.Contains("max-width:none!important", css, StringComparison.Ordinal);
      Assert.Contains("color:#000000!important", css, StringComparison.Ordinal);
      Assert.Contains("[data-mv-body][hidden]{display:block!important;}", css, StringComparison.Ordinal);
      Assert.DoesNotContain("img{display:none", css, StringComparison.Ordinal);
      Assert.DoesNotContain("pre,blockquote,code,details{border:", css, StringComparison.Ordinal);
      Assert.Equal(before, active.Json);
      Assert.Equal(new PrintOptions(), options);
   }



   /// <summary>Active/named themes use their validated colors and fonts; absent choices use the clean fallback.</summary>
   [Theory]
   [InlineData("Active Theme", "#1e1e2e")]
   [InlineData("Catppuccin Mocha", "#1e1e2e")]
   [InlineData("Missing saved theme", "#ffffff")]
   [InlineData("Clean Printing", "#ffffff")]
   public void Build_SelectedTheme_UsesExpectedPrintPalette(string choice, string background)
   {
      ReaderTheme active = ThemeCatalog.Find("Catppuccin Mocha");
      string css = PrintStyles.Build(new PrintOptions(Theme: choice, Backgrounds: true, MatchScreenZoom: true, FullPage: false, Images: false, Borders: true, PaddingPixels: 16.5), active, ThemeCatalog.BuiltIn, 1.1);
      Assert.Contains("--background:" + background + ";", css, StringComparison.Ordinal);
      Assert.DoesNotContain("#content-inner{max-width:none!important", css, StringComparison.Ordinal);
      Assert.Contains("#content-inner{max-width:900px;margin:0 auto;}", css, StringComparison.Ordinal);
      Assert.DoesNotContain("background:#ffffff!important", css, StringComparison.Ordinal);
      Assert.Contains("img{display:none!important;}", css, StringComparison.Ordinal);
      Assert.Contains("pre,blockquote,code,details{border:1px solid currentColor!important;}", css, StringComparison.Ordinal);
      Assert.Contains("body{padding:16.5px!important;}", css, StringComparison.Ordinal);
      Assert.Contains("--h1Size:", css, StringComparison.Ordinal);
   }



   /// <summary>Plain HTML cannot inherit reading-theme/custom CSS and culture never produces a CSS decimal comma.</summary>
   [Fact]
   public void Build_PlainHtml_DoesNotUseReadingTheme()
   {
      CultureInfo previous = CultureInfo.CurrentCulture;
      try
      {
         CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
         string css = PrintStyles.Build(new PrintOptions(Theme: "Plain HTML", PaddingPixels: 8.5), ThemeCatalog.Find(null), [], 0.3);
         Assert.DoesNotContain("--baseFontSize", css, StringComparison.Ordinal);
         Assert.Contains("padding:8.5px!important", css, StringComparison.Ordinal);
      }
      finally
      {
         CultureInfo.CurrentCulture = previous;
      }
   }



   /// <summary>Matching screen zoom scales print fonts while the original reading theme and its data remain unchanged.</summary>
   [Fact]
   public void Build_CustomThemeAndMatchingZoom_RetainsImmutableTheme()
   {
      ReaderTheme original = ThemeCatalog.Find("Catppuccin Mocha");
      var custom = ReaderTheme.Parse(original.Json.Replace("Catppuccin Mocha", "Print custom", StringComparison.Ordinal));
      string expected = custom.GetStyles(1.1);
      string css = PrintStyles.Build(new PrintOptions(Theme: "Print custom", MatchScreenZoom: true), original, [custom], 1.1);
      Assert.Contains(expected, css, StringComparison.Ordinal);
      Assert.True(css.IndexOf("#content-inner{max-width:900px;", StringComparison.Ordinal) < css.IndexOf(expected, StringComparison.Ordinal));
      Assert.Equal(original.GetStyles(1), original.Styles);
      Assert.Equal(ReaderTheme.Parse(custom.Json).Styles, custom.Styles);
   }



   /// <summary>Blank, control-containing and oversized theme names cannot become persisted selector/CSS inputs.</summary>
   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData("Bad\nname")]
   public void Sanitize_InvalidThemeName_UsesCleanDefault(string? name)
   {
      Assert.Equal("Clean Printing", new PrintOptions(Theme: name!).Sanitize().Theme);
      Assert.Equal("Clean Printing", new PrintOptions(Theme: new string('x', 81)).Sanitize().Theme);
   }



   /// <summary>Untrusted saved page padding stays finite and within the native UI's supported 0–96 pixel range.</summary>
   [Theory]
   [InlineData(double.NaN, 0)]
   [InlineData(double.PositiveInfinity, 0)]
   [InlineData(double.NegativeInfinity, 0)]
   [InlineData(-1, 0)]
   [InlineData(0, 0)]
   [InlineData(24, 24)]
   [InlineData(97, 96)]
   public void Sanitize_SavedPadding_ClampsOrRepairs(double value, double expected)
   {
      Assert.Equal(expected, new PrintOptions(PaddingPixels: value).Sanitize().PaddingPixels);
   }



   /// <summary>Invalid host zoom values cannot enter print CSS, even for plain/unscaled printing.</summary>
   [Theory]
   [InlineData(double.NaN)]
   [InlineData(double.PositiveInfinity)]
   [InlineData(double.NegativeInfinity)]
   [InlineData(0.29)]
   [InlineData(5.01)]
   public void Build_InvalidReadingZoom_RejectsValue(double value)
   {
      Assert.Throws<ArgumentOutOfRangeException>(() => PrintStyles.Build(new PrintOptions(Theme: "Plain HTML"), ThemeCatalog.Find(null), [], value));
   }



   /// <summary>Missing host dependencies fail directly instead of silently omitting print validation.</summary>
   [Fact]
   public void Build_NullInputs_RejectsMissingData()
   {
      Assert.Throws<ArgumentNullException>(() => PrintStyles.Build(null!, ThemeCatalog.Find(null), [], 1));
      Assert.Throws<ArgumentNullException>(() => PrintStyles.Build(new PrintOptions(), null!, [], 1));
      Assert.Throws<ArgumentNullException>(() => PrintStyles.Build(new PrintOptions(), ThemeCatalog.Find(null), null!, 1));
   }



   /// <summary>Ctrl+P belongs to ready reading surfaces; text entry and other modifiers retain normal native behavior.</summary>
   [Theory]
   [InlineData('p', true, false, false, true)]
   [InlineData('P', true, false, false, true)]
   [InlineData('p', false, false, false, false)]
   [InlineData('p', true, true, false, false)]
   [InlineData('p', true, false, true, false)]
   public void Resolve_PrintShortcut_RespectsFocusAndModifiers(char character, bool reading, bool shift, bool alt, bool expected)
   {
      var keyboard = new Navigation.ReaderKeyboard();
      Navigation.ReaderCommand command = keyboard.Resolve(new Navigation.ReaderKey(character, Control: true, Shift: shift, Alt: alt), reading, 1);
      Assert.Equal(expected ? Navigation.ReaderCommand.Print : Navigation.ReaderCommand.None, command);
   }



   /// <summary>Older/missing nested settings retain the reference's true image/full-page defaults.</summary>
   [Theory]
   [InlineData("{}")]
   [InlineData("{\"Print\":{}}")]
   [InlineData("{\"Print\":null}")]
   public void Deserialize_OlderPrintSettings_RetainsReferenceDefaults(string json)
   {
      AppSettings settings = JsonSerializer.Deserialize(json, AppSettingsContext.Default.AppSettings)!;
      settings.Sanitize();
      Assert.Equal(new PrintOptions(), settings.Print);
   }



   /// <summary>AppKit's generated settings JSON preserves every print choice and null input cannot block startup.</summary>
   [Fact]
   public void Sanitize_PrintPreferences_RoundTripsWithoutChangingReadingPreferences()
   {
      var settings = new AppSettings { DocumentTheme = "GitHub Light", ShowSource = true, Print = new PrintOptions("Active Theme", true, false, true, false, true, 24) };
      string json = JsonSerializer.Serialize(settings, AppSettingsContext.Default.AppSettings);
      AppSettings loaded = JsonSerializer.Deserialize(json, AppSettingsContext.Default.AppSettings)!;
      loaded.Sanitize();
      Assert.Equal(settings.Print, loaded.Print);
      Assert.Equal("GitHub Light", loaded.DocumentTheme);
      Assert.True(loaded.ShowSource);
      loaded.Print = null!;
      loaded.Sanitize();
      Assert.Equal(new PrintOptions(), loaded.Print);
   }
}
