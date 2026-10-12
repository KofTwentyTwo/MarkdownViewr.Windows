/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;
using AngleSharp.Html.Parser;
using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Verifies independent reference syntax, passive content, original offsets and saved window preferences.</summary>
public sealed class MarkdownExtensionsTests
{
   /// <summary>Every preference combination preserves GFM and renders only the explicitly enabled extension tags.</summary>
   [Theory]
   [InlineData(0)]
   [InlineData(1)]
   [InlineData(2)]
   [InlineData(3)]
   [InlineData(4)]
   [InlineData(5)]
   [InlineData(6)]
   [InlineData(7)]
   [InlineData(8)]
   [InlineData(9)]
   [InlineData(10)]
   [InlineData(11)]
   [InlineData(12)]
   [InlineData(13)]
   [InlineData(14)]
   [InlineData(15)]
   public void Render_IndependentOptions_RetainsOnlyEnabledSyntax(int mask)
   {
      var options = new MarkdownExtensions((mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0, (mask & 8) != 0);
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer(options).Render("==Highlighted text== x^Power^ H~2~O ++Underlined text++ ~~Removed text~~"));
      foreach((bool enabled, string tag, string value, string literal) in new[]
      {
         (options.Highlight, "mark", "Highlighted text", "==Highlighted text=="),
         (options.Superscript, "sup", "Power", "^Power^"),
         (options.Subscript, "sub", "2", "~2~"),
         (options.Underline, "ins", "Underlined text", "++Underlined text++"),
      })
      {
         if(enabled)
         {
            Assert.Equal(value, Assert.Single(document.QuerySelectorAll(tag)).TextContent);
            Assert.DoesNotContain(literal, document.Body!.TextContent, StringComparison.Ordinal);
         }
         else
         {
            Assert.Empty(document.QuerySelectorAll(tag));
            Assert.Contains(literal, document.Body!.TextContent, StringComparison.Ordinal);
         }
      }
      Assert.Equal("Removed text", Assert.Single(document.QuerySelectorAll("del")).TextContent);
   }



   /// <summary>Raw block text follows the reference options while attributes and authored code remain unchanged.</summary>
   [Theory]
   [InlineData(0)]
   [InlineData(1)]
   [InlineData(2)]
   [InlineData(3)]
   [InlineData(4)]
   [InlineData(5)]
   [InlineData(6)]
   [InlineData(7)]
   [InlineData(8)]
   [InlineData(9)]
   [InlineData(10)]
   [InlineData(11)]
   [InlineData(12)]
   [InlineData(13)]
   [InlineData(14)]
   [InlineData(15)]
   public void Render_RawHtmlBlockOptions_PreservesAttributesAndCode(int mask)
   {
      const string Syntax = "==Highlight== ^Power^ H~2~O ++Underline++";
      const string Markdown = "<div title='==attribute== ^attribute^ ~attribute~ ++attribute++'>\n" + Syntax + "\n<code>" + Syntax + "</code>\n<pre>" + Syntax + "</pre>\n</div>";
      var options = new MarkdownExtensions((mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0, (mask & 8) != 0);
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer(options).Render(Markdown));
      Assert.Equal("==attribute== ^attribute^ ~attribute~ ++attribute++", document.QuerySelector("div")!.GetAttribute("title"));
      Assert.Equal(Syntax, document.QuerySelector("code")!.TextContent);
      Assert.Equal(Syntax, document.QuerySelector("pre")!.TextContent);
      Assert.Equal(options.Highlight ? 1 : 0, document.QuerySelectorAll("mark").Length);
      Assert.Equal(options.Superscript ? 1 : 0, document.QuerySelectorAll("sup").Length);
      Assert.Equal(options.Subscript ? 1 : 0, document.QuerySelectorAll("sub").Length);
      Assert.Equal(options.Underline ? 1 : 0, document.QuerySelectorAll("ins").Length);
   }



   /// <summary>Reference metadata receives post-render extensions but keeps the body-only subscript preprocessing boundary.</summary>
   [Theory]
   [InlineData(0)]
   [InlineData(1)]
   [InlineData(2)]
   [InlineData(3)]
   [InlineData(4)]
   [InlineData(5)]
   [InlineData(6)]
   [InlineData(7)]
   [InlineData(8)]
   [InlineData(9)]
   [InlineData(10)]
   [InlineData(11)]
   [InlineData(12)]
   [InlineData(13)]
   [InlineData(14)]
   [InlineData(15)]
   public void Render_MetadataExtensions_PreservesReferencePreprocessingBoundary(int mask)
   {
      var options = new MarkdownExtensions((mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0, (mask & 8) != 0);
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer(options).Render("---\ntitle: ==Highlight== ^Power^ H~2~O ++Underline++\n---\n\nBody", FrontmatterMode.Metadata));
      Assert.Equal(options.Highlight ? 1 : 0, document.QuerySelectorAll("mark").Length);
      Assert.Equal(options.Superscript ? 1 : 0, document.QuerySelectorAll("sup").Length);
      Assert.Equal(options.Underline ? 1 : 0, document.QuerySelectorAll("ins").Length);
      Assert.Empty(document.QuerySelectorAll("sub"));
      Assert.Contains("H~2~O", document.QuerySelector(".fm-value")!.TextContent, StringComparison.Ordinal);
   }



   /// <summary>Inline, fenced and indented code remain literal even though all four extensions are enabled.</summary>
   [Fact]
   public void Render_CodeAndEscapes_PreservesLiteralSyntax()
   {
      const string Syntax = "==Highlight== ^Power^ ~Sub~ ++Underline++";
      string markdown = "`" + Syntax + "`\n\n```md\n" + Syntax + "\n```\n\n    " + Syntax + "\n\n\\==escaped== \\^escaped^ \\~escaped~ \\+\\+escaped++";
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer().Render(markdown));
      Assert.Empty(document.QuerySelectorAll("mark,sup,sub,ins"));
      Assert.Equal(3, document.QuerySelectorAll("code").Length);
      Assert.All(document.QuerySelectorAll("code"), code => Assert.Equal(Syntax, code.TextContent.TrimEnd('\n')));
      Assert.Contains("==escaped== ^escaped^ ~escaped~ ++escaped++", document.Body!.TextContent, StringComparison.Ordinal);
   }



   /// <summary>Authored HTML code and delimiters spanning it remain literal, like protected inline/fenced code.</summary>
   [Theory]
   [InlineData("Before <code>==Highlight== x^Power^ H~2~O ++Underline++</code> after.")]
   [InlineData("Before <CODE class='language-md'>==Highlight== x^Power^ H~2~O ++Underline++</CODE> after.")]
   [InlineData("==<code>Highlight</code>==")]
   [InlineData("++<code>Underline</code>++")]
   public void Render_AuthoredCode_PreservesLiteralExtensionBoundaries(string markdown)
   {
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer().Render(markdown));
      Assert.Empty(document.QuerySelectorAll("mark,sup,sub,ins"));
      Assert.NotNull(document.QuerySelector("code"));
      using AngleSharp.Html.Dom.IHtmlDocument literal = new HtmlParser().ParseDocument(markdown);
      Assert.Equal(literal.Body!.TextContent, document.Body!.TextContent.TrimEnd('\n'));
   }



   /// <summary>Actual closing tags across HTML blocks restore formatting without granting code-like tag names protection.</summary>
   [Theory]
   [InlineData("Before <code>==Literal==\n\n==Protected==\n</code>\n\n==Outside==")]
   [InlineData("<pre>==Literal==</pre>\n\n==Outside==")]
   [InlineData("Before </code> ==Outside==")]
   [InlineData("Before <code-other>==Ignored==</code-other> ==Outside==")]
   [InlineData("Before <a title='<code>'>==Outside==</a>")]
   [InlineData("Before <!--<code>-->==Outside==")]
   public void Render_AuthoredCodeClosingAndLookalikes_RestoresOutsideExtensions(string markdown)
   {
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer().Render(markdown));
      Assert.Equal("Outside", Assert.Single(document.QuerySelectorAll("mark")).TextContent);
      Assert.Empty(document.QuerySelectorAll("code mark,pre mark"));
   }



   /// <summary>Reference superscripts/subscripts contain no whitespace; ordinary caret/tilde prose stays literal.</summary>
   [Fact]
   public void Render_WhitespaceSuperscriptAndSubscript_RemainsLiteral()
   {
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer().Render("^two words^ ~two words~ ^line\nbreak^ ~line\nbreak~"));
      Assert.Empty(document.QuerySelectorAll("sup,sub"));
      Assert.Contains("^two words^ ~two words~", document.Body!.TextContent, StringComparison.Ordinal);
   }



   /// <summary>Delimiters outside a code span or across a newline stay literal without changing the protected code.</summary>
   [Theory]
   [InlineData("==`code`==")]
   [InlineData("++`code`++")]
   [InlineData("^`code`^")]
   [InlineData("~`code`~")]
   [InlineData("==line\nbreak==")]
   [InlineData("++line\nbreak++")]
   public void Render_CodeOrNewlineBoundary_RetainsDelimiters(string markdown)
   {
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer().Render(markdown));
      Assert.Empty(document.QuerySelectorAll("mark,sup,sub,ins"));
      Assert.Equal(markdown.Replace("`", "", StringComparison.Ordinal), document.Body!.TextContent.TrimEnd('\n'));
   }



   /// <summary>Formatting composes with emphasis/links without changing destinations, attributes or passive code.</summary>
   [Fact]
   public void Render_NestedFormattingAndAttributes_PreservesPassiveBoundary()
   {
      const string Markdown = "==**Highlighted strong**== ++*Underlined emphasis*++ [==Label==](https://example.com/==literal==)\n\n<mark id='host' style='color:red' onclick='evil()'>Raw mark</mark><ins cite='javascript:evil()' onmouseover='evil()'>Raw insert</ins>";
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer().Render(Markdown));
      Assert.Equal("Highlighted strong", document.QuerySelector("mark strong")!.TextContent);
      Assert.Equal("Underlined emphasis", document.QuerySelector("ins em")!.TextContent);
      Assert.Equal("Label", document.QuerySelector("a mark")!.TextContent);
      Assert.Equal("https://example.com/==literal==", document.QuerySelector("a")!.GetAttribute("href"));
      Assert.All(document.QuerySelectorAll("mark,ins"), element => Assert.Empty(element.Attributes));
   }



   /// <summary>Extensions change heading presentation while all source ranges still point at the original UTF-16 snapshot.</summary>
   [Theory]
   [InlineData(FrontmatterMode.Hide)]
   [InlineData(FrontmatterMode.Metadata)]
   [InlineData(FrontmatterMode.Markdown)]
   public void Create_ExtensionHeadings_PreservesOriginalOffsets(FrontmatterMode mode)
   {
      const string Source = "---\r\n# ==Metadata==\r\n---\r\n\r\n# ==Highlighted== H~2~O\r\n\r\n++Underlined++ ^Power^\r\n---\r\n";
      var outline = new DocumentOutline(Source, mode);
      Assert.Equal(mode == FrontmatterMode.Markdown ? 3 : 2, outline.Headings.Count);
      Assert.Equal("Highlighted H2O", outline.Headings[^2].Title);
      Assert.Equal("Underlined Power", outline.Headings[^1].Title);
      foreach(DocumentSourceHeading heading in outline.SourceHeadings)
      {
         Assert.Contains(heading.Index == outline.Headings.Count - 1 ? "++Underlined++ ^Power^" : "==", Source[heading.Start..heading.End], StringComparison.Ordinal);
      }
      Assert.DoesNotContain("data-mv-source-", outline.Html, StringComparison.Ordinal);
      var disabled = new DocumentOutline(Source, mode, new MarkdownExtensions(false, false, false, false));
      Assert.Equal("==Highlighted== H~2~O", disabled.Headings[^2].Title);
      Assert.Equal("++Underlined++ ^Power^", disabled.Headings[^1].Title);
      Assert.Equal(outline.SourceHeadings, disabled.SourceHeadings);
   }



   /// <summary>Protecting authored heading code does not rewrite the original source identities or ranges.</summary>
   [Fact]
   public void Create_AuthoredCodeHeading_PreservesOriginalOffsets()
   {
      const string Source = "# Root\n\nprefix <h2>Child <code>==Literal== ^Power^</code> ==Marked==</h2>\n\nBody.";
      var outline = new DocumentOutline(Source);
      Assert.Equal("Child ==Literal== ^Power^ Marked", outline.Headings[1].Title);
      DocumentSourceHeading heading = outline.SourceHeadings[1];
      Assert.Equal("<h2>Child <code>==Literal== ^Power^</code> ==Marked==</h2>", Source[heading.Start..heading.End]);
      var disabled = new DocumentOutline(Source, extensions: new MarkdownExtensions(false, false, false, false));
      Assert.Equal("Child ==Literal== ^Power^ ==Marked==", disabled.Headings[1].Title);
      Assert.Equal(outline.SourceHeadings, disabled.SourceHeadings);
   }



   /// <summary>Formatting raw headings changes visible labels while preserving exact CRLF/Unicode source spans and code text.</summary>
   [Fact]
   public void Create_RawBlockExtensionHeading_PreservesOriginalOffsetsAndEntities()
   {
      const string Source = "# Root\r\n\r\n<h2 title='==attribute=='>==Raw &amp; café 😀== <code>++Literal++</code></h2>\r\n\r\nBody.";
      var outline = new DocumentOutline(Source);
      Assert.Equal("Raw & café 😀 ++Literal++", outline.Headings[1].Title);
      DocumentSourceHeading heading = outline.SourceHeadings[1];
      Assert.Equal("<h2 title='==attribute=='>==Raw &amp; café 😀== <code>++Literal++</code></h2>", Source[heading.Start..heading.End]);
      var disabled = new DocumentOutline(Source, extensions: new MarkdownExtensions(false, false, false, false));
      Assert.Equal("==Raw & café 😀== ++Literal++", disabled.Headings[1].Title);
      Assert.Equal(outline.SourceHeadings, disabled.SourceHeadings);
   }



   /// <summary>Old JSON loads all four reference defaults; false choices survive source-generated persistence.</summary>
   [Fact]
   public void Copy_DefaultAndDisabledOptions_PreservesPreferences()
   {
      var settings = new AppSettings();
      Assert.Equal(new MarkdownExtensions(), SettingsChanges.Copy(settings).MarkdownExtensions);
      settings.RenderHighlight = false;
      settings.RenderSuperscript = false;
      settings.RenderSubscript = false;
      settings.RenderUnderline = false;
      Assert.Equal(new MarkdownExtensions(false, false, false, false), SettingsChanges.Copy(settings).MarkdownExtensions);
   }



   /// <summary>Existing settings omit these new fields; deserialization retains all four defaults while explicit false remains false.</summary>
   [Fact]
   public void Deserialize_OldAndSavedSettings_RetainsReferenceDefaults()
   {
      AppSettings original = JsonSerializer.Deserialize("{ \"ShowSplashScreen\": false }", AppSettingsContext.Default.AppSettings)!;
      Assert.Equal(new MarkdownExtensions(), original.MarkdownExtensions);
      original.RenderHighlight = false;
      original.RenderSuperscript = false;
      original.RenderSubscript = false;
      original.RenderUnderline = false;
      string json = JsonSerializer.Serialize(original, AppSettingsContext.Default.AppSettings);
      Assert.DoesNotContain("MarkdownExtensions", json, StringComparison.Ordinal);
      Assert.Equal(original.MarkdownExtensions, JsonSerializer.Deserialize(json, AppSettingsContext.Default.AppSettings)!.MarkdownExtensions);
   }



   /// <summary>Marked/inserted content uses the validated selected palette and underline style, including print theme generation.</summary>
   [Fact]
   public void GetStyles_ExtensionFormatting_UsesThemeColors()
   {
      foreach(Themes.ReaderTheme theme in Themes.ThemeCatalog.BuiltIn)
      {
         Assert.Contains("background-color: var(--highlight-bg);", theme.Styles, StringComparison.Ordinal);
         Assert.Contains("color: var(--highlight-text);", theme.Styles, StringComparison.Ordinal);
         Assert.Contains("--highlight-bg:var(--highlightBackground);", theme.Styles, StringComparison.Ordinal);
         Assert.Contains("--highlight-text:var(--highlightText);", theme.Styles, StringComparison.Ordinal);
         Assert.Contains("ins { text-decoration: underline; }", theme.GetStyles(1.1), StringComparison.Ordinal);
      }
   }



   /// <summary>Separate native toggles merge individually; a stale window cannot overwrite a sibling's other extension choice.</summary>
   [Fact]
   public void Merge_IndependentExtensionChanges_PreservesSiblingChoice()
   {
      var before = new AppSettings();
      AppSettings edited = SettingsChanges.Copy(before);
      edited.RenderHighlight = false;
      var current = new AppSettings { RenderSuperscript = false, RenderSubscript = false, RenderUnderline = false };
      AppSettings result = SettingsChanges.Merge(current, before, edited);
      Assert.Equal(new MarkdownExtensions(false, false, false, false), result.MarkdownExtensions);
      Assert.True(current.RenderHighlight);
   }
}
