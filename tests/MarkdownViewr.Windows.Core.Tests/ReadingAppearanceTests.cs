/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using MarkdownViewr.Windows.Core.Printing;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Reading layout/CSS preserve reference precedence, bounds, HTML safety and print independence.</summary>
public sealed class ReadingAppearanceTests
{
   /// <summary>The reference default is enabled1000px; corrupt saved widths are bounded without losing the enable flag.</summary>
   [Theory]
   [InlineData(double.NaN, 1000)]
   [InlineData(double.PositiveInfinity, 1000)]
   [InlineData(double.NegativeInfinity, 1000)]
   [InlineData(-1, 1000)]
   [InlineData(0, 1000)]
   [InlineData(199, 200)]
   [InlineData(320, 320)]
   [InlineData(4001, 4000)]
   public void Sanitize_SavedWidths_RepairsCorruption(double width, double expected)
   {
      var settings = new AppSettings { ContentWidthEnabled = false, ContentWidthPx = width };
      settings.Sanitize();
      Assert.Equal(new ReadingAppearance(false, expected), settings.ReadingAppearance);
      Assert.Equal(new ReadingAppearance(), new AppSettings().ReadingAppearance);
   }



   /// <summary>Oversized/null saved CSS is discarded; invalid live drafts are rejected rather than repaired silently.</summary>
   [Fact]
   public void Sanitize_InvalidCss_RepairsOnlySavedInput()
   {
      foreach(string? css in new[] { null, new string('x', ReadingAppearance.MaximumCssCharacters + 1) })
      {
         var options = new ReadingAppearance(GlobalCss: css!);
         Assert.Equal("", options.Sanitize().GlobalCss);
         Assert.Throws<InvalidOperationException>(options.Validate);
         var settings = new AppSettings { GlobalCss = css! };
         settings.Sanitize();
         Assert.Equal("", settings.GlobalCss);
      }
      var valid = new ReadingAppearance(GlobalCss: new string(' ', ReadingAppearance.MaximumCssCharacters));
      valid.Validate();
      Assert.Equal(valid, valid.Sanitize());
   }



   /// <summary>Nonfinite/out-of-range native drafts fail before generating CSS or saving preferences.</summary>
   [Theory]
   [InlineData(double.NaN)]
   [InlineData(double.PositiveInfinity)]
   [InlineData(199)]
   [InlineData(4001)]
   public void Validate_InvalidNativeWidth_RejectsDraft(double width)
   {
      var options = new ReadingAppearance(WidthPixels: width);
      Assert.Throws<InvalidOperationException>(options.Validate);
      Assert.Throws<InvalidOperationException>(() => ThemeCatalog.Find(null).GetReadingStyles(options, 1));
   }



   /// <summary>Global CSS comes before per-theme CSS, then enabled width applies last and scales with reading fonts.</summary>
   [Fact]
   public void GetReadingStyles_ReferenceCascade_PreservesThemeAndWidthPriority()
   {
      JsonNode root = JsonNode.Parse(ThemeCatalog.Find(null).Json)!;
      root["customCSS"] = "body { background: #222222; }";
      var theme = ReaderTheme.Parse(root.ToJsonString());
      string original = theme.Json;
      string css = theme.GetReadingStyles(new ReadingAppearance(WidthPixels: 320, GlobalCss: "body { background: #111111; }"), 1.5);
      int baseRules = css.IndexOf("#content-inner {", StringComparison.Ordinal);
      int global = css.IndexOf("body { background: #111111; }", StringComparison.Ordinal);
      int perTheme = css.IndexOf("body { background: #222222; }", StringComparison.Ordinal);
      int width = css.IndexOf("#content-inner{max-width:calc(320px * var(--zoom));margin:auto;}", StringComparison.Ordinal);
      Assert.True(baseRules >= 0 && global > baseRules && perTheme > global && width > perTheme);
      Assert.Equal(original, theme.Json);
      string unbounded = theme.GetReadingStyles(new ReadingAppearance(false, 320, "body { max-width: 450px; }"), 1.5);
      Assert.DoesNotContain("#content-inner{max-width:calc(", unbounded, StringComparison.Ordinal);
      Assert.Contains("body { max-width: 450px; }", unbounded, StringComparison.Ordinal);
      Assert.DoesNotContain("#content-inner{max-width:calc(320px", unbounded, StringComparison.Ordinal);
   }



   /// <summary>Zoom width uses invariant finite numbers; custom CSS cannot close the containing screen style element.</summary>
   [Fact]
   public void GetReadingStyles_HostileCssAndCulture_PreservesHtmlBoundary()
   {
      CultureInfo previous = CultureInfo.CurrentCulture;
      try
      {
         CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
         string css = ThemeCatalog.Find(null).GetReadingStyles(new ReadingAppearance(WidthPixels: 321.5, GlobalCss: "} @media print {body{color:red}} </style><script>evil()</script>"), 1.5);
         Assert.Contains("--zoom:1.5;", css, StringComparison.Ordinal);
         Assert.Contains("max-width:calc(321.5px * var(--zoom));", css, StringComparison.Ordinal);
         Assert.DoesNotContain("<", css, StringComparison.Ordinal);
         Assert.Contains("\\3c /style>", css, StringComparison.Ordinal);
      }
      finally
      {
         CultureInfo.CurrentCulture = previous;
      }
   }



   /// <summary>Reading overrides never alter PrintStyles or the saved print theme/layout.</summary>
   [Fact]
   public void GetReadingStyles_GlobalOverrides_LeavesPrintIndependent()
   {
      ReaderTheme theme = ThemeCatalog.Find(null);
      var print = new PrintOptions("Active Theme", Backgrounds: true, FullPage: false);
      string before = PrintStyles.Build(print, theme, ThemeCatalog.BuiltIn, 1);
      string reading = theme.GetReadingStyles(new ReadingAppearance(WidthPixels: 320, GlobalCss: "html { background: #112233; }"), 1);
      Assert.Contains("#112233", reading, StringComparison.Ordinal);
      Assert.DoesNotContain("#112233", before, StringComparison.Ordinal);
      Assert.DoesNotContain("max-width:320px", before, StringComparison.Ordinal);
      Assert.Equal(before, PrintStyles.Build(print, theme, ThemeCatalog.BuiltIn, 1));
   }



   /// <summary>Old JSON retains the reference defaults; new disabled width/global CSS preferences survive source-generated persistence.</summary>
   [Fact]
   public void Serialize_AppearancePreferences_PreservesOldDefaultsAndNewChoices()
   {
      AppSettings old = JsonSerializer.Deserialize("{}", AppSettingsContext.Default.AppSettings)!;
      Assert.Equal(new ReadingAppearance(), old.ReadingAppearance);
      var settings = new AppSettings { ContentWidthEnabled = false, ContentWidthPx = 320, GlobalCss = "p { color: #123456; }" };
      string json = JsonSerializer.Serialize(settings, AppSettingsContext.Default.AppSettings);
      Assert.DoesNotContain("ReadingAppearance", json, StringComparison.Ordinal);
      Assert.Equal(settings.ReadingAppearance, JsonSerializer.Deserialize(json, AppSettingsContext.Default.AppSettings)!.ReadingAppearance);
   }



   /// <summary>A width edit in one reader preserves newer global CSS in another reader's saved preferences.</summary>
   [Fact]
   public void Merge_IndependentAppearanceFields_PreservesSiblingCss()
   {
      var before = new AppSettings();
      AppSettings edited = SettingsChanges.Copy(before);
      edited.ContentWidthPx = 320;
      var current = new AppSettings { GlobalCss = "body { color: #abcdef; }", ContentWidthEnabled = false };
      Assert.Equal(new ReadingAppearance(false, 320, current.GlobalCss), SettingsChanges.Merge(current, before, edited).ReadingAppearance);
      Assert.Equal(1000, current.ContentWidthPx);
   }



   /// <summary>Missing appearance or invalid zoom reports an explicit contract failure before producing style output.</summary>
   [Theory]
   [InlineData(double.NaN)]
   [InlineData(double.PositiveInfinity)]
   [InlineData(0.29)]
   [InlineData(5.01)]
   public void GetReadingStyles_InvalidArguments_RejectsInput(double zoom)
   {
      ReaderTheme theme = ThemeCatalog.Find(null);
      Assert.Throws<ArgumentOutOfRangeException>(() => theme.GetReadingStyles(new ReadingAppearance(), zoom));
      Assert.Throws<ArgumentNullException>(() => theme.GetReadingStyles(null!, 1));
   }
}
