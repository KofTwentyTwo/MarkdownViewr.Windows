/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using MarkdownViewr.Windows.Core.Documents;
using MarkdownViewr.Windows.Core.Printing;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Checks the pinned Mac document CSS/DOM contract independently of native layout and preserves passive markup/print isolation.</summary>
public sealed class DocumentStylesTests
{
   /// <summary>Original CSS variable names remain available in every original palette while earlier Windows aliases stay compatible.</summary>
   [Fact]
   public void ReadingStyles_AllPalettes_ExportsOriginalCssContract()
   {
      var aliases = new Dictionary<string, string>(StringComparer.Ordinal)
      {
         ["bg"] = "background",
         ["h1"] = "heading1",
         ["h2"] = "heading2",
         ["h3"] = "heading3",
         ["h4"] = "heading4",
         ["h5"] = "heading5",
         ["h6"] = "heading6",
         ["code-bg"] = "codeBackground",
         ["code-text"] = "codeText",
         ["blockquote-bg"] = "blockquoteBackground",
         ["blockquote-border"] = "blockquoteBorder",
         ["highlight-bg"] = "highlightBackground",
         ["highlight-text"] = "highlightText",
         ["body-font"] = "bodyFont",
         ["heading-font"] = "headingFont",
         ["code-font"] = "codeFont",
         ["base-font-size"] = "baseFontSize",
         ["h1-size"] = "h1Size",
         ["h2-size"] = "h2Size",
         ["h3-size"] = "h3Size",
         ["h4-size"] = "h4Size",
         ["h5-size"] = "h5Size",
         ["h6-size"] = "h6Size",
         ["code-font-size"] = "codeFontSize",
         ["line-height"] = "lineHeight",
      };
      Assert.Equal(20, ThemeCatalog.BuiltIn.Count);
      foreach(ReaderTheme theme in ThemeCatalog.BuiltIn)
      {
         ICssStyleDeclaration root = Rules(theme.GetReadingStyles(new ReadingAppearance(), 1.5)).Single(rule => string.Equals(rule.SelectorText, ":root", StringComparison.Ordinal)).Style;
         foreach((string name, string target) in aliases)
         {
            Assert.Equal("var(--" + target + ")", root.GetPropertyValue("--" + name));
            Assert.NotEmpty(root.GetPropertyValue("--" + target));
         }
         Assert.Equal("1.5", root.GetPropertyValue("--zoom"));
         Assert.NotEmpty(root.GetPropertyValue("--text"));
         Assert.NotEmpty(root.GetPropertyValue("--link"));
      }
   }



   /// <summary>Reference spacing for the inner body, code, lists, nested quotes and tables is parsed as actual CSS declarations.</summary>
   [Fact]
   public void ReadingStyles_DocumentRules_MatchPinnedReference()
   {
      ICssStyleRule[] rules = Rules(ThemeCatalog.Find(null).GetReadingStyles(new ReadingAppearance(), 1));
      Assert.Equal("32px 48px", Property(rules, "#content-inner", "padding"));
      Assert.Equal("auto", Property(rules, "#content", "overflow-y"));
      Assert.Equal("100vh", Property(rules, "#content", "height"));
      Assert.Equal("8px", Property(rules, "pre", "border-radius"));
      Assert.Equal("0.9em", Property(rules, "code", "font-size"));
      Assert.Equal("var(--code-font-size)", Property(rules, "pre code", "font-size"));
      Assert.Equal("24px", Property(rules, "ul, ol", "padding-left"));
      Assert.Equal("4px", Property(rules, "li", "margin-bottom"));
      Assert.Equal("100%", Property(rules, "table", "width"));
      Assert.Equal("8px 12px", Property(rules, "th, td", "padding"));
      Assert.Equal("0.6", Property(rules, "del", "opacity"));
      Assert.Equal("var(--blockquote-bg)", Property(rules, "blockquote blockquote blockquote", "background"));
      Assert.Equal("none", Property(rules, "li.task-list-item", "list-style"));
      Assert.Equal("12px", Property(rules, "::-webkit-scrollbar", "width"));
   }



   /// <summary>A document has one private scroll host and original inner selector; metadata, GFM and generated collapse bodies survive.</summary>
   [Fact]
   public void Page_RenderedDocument_PreservesPrivateContainersAndSafeContent()
   {
      const string Markdown = "---\ntitle: Example\n---\n# Heading\n\n- [x] Complete\n\n```csharp\nvar x = 1;\n```\n\n<div id='content' class='unknown' onclick='evil()'>Literal</div>";
      var outline = new DocumentOutline(Markdown, FrontmatterMode.Metadata);
      string page = DocumentPage.Build(outline.Html, "<title> & café </title><script>evil()</script>", ThemeCatalog.Find(null), new ReadingAppearance(), 1);
      using IHtmlDocument document = new HtmlParser().ParseDocument(page);
      Assert.Equal("<title> & café </title><script>evil()</script>", document.Title);
      Assert.Single(document.QuerySelectorAll("#content"));
      Assert.Single(document.QuerySelectorAll("#content-inner"));
      Assert.NotNull(document.QuerySelector("body > #content > #content-inner"));
      Assert.Equal("Heading", document.QuerySelector("#content-inner #mv-heading-0")!.TextContent);
      Assert.NotNull(document.QuerySelector("#content-inner [data-mv-body='0']"));
      Assert.Equal("title", document.QuerySelector("#content-inner .frontmatter .fm-key")!.TextContent);
      Assert.Equal("Example", document.QuerySelector("#content-inner .frontmatter .fm-value")!.TextContent);
      Assert.Equal("var x = 1;\n", document.QuerySelector("pre code.language-csharp")!.TextContent);
      Assert.NotNull(document.QuerySelector("input[disabled][checked]"));
      Assert.Empty(document.QuerySelectorAll("script, [onclick], .unknown, #toc, #raw-source"));
      Assert.Equal("screen", document.QuerySelector("style")!.GetAttribute("media"));
   }



   /// <summary>Custom CSS may use the original variables/selectors without closing the screen style or adding native/browser capabilities.</summary>
   [Fact]
   public void Page_HostileThemeCss_PreservesStyleAndTitleBoundaries()
   {
      var appearance = new ReadingAppearance(GlobalCss: "#content-inner { padding: calc(2px * var(--zoom)); } </style><script>evil()</script>");
      string page = DocumentPage.Build("<p>Safe</p>", "</title><script>evil()</script>", ThemeCatalog.Find(null), appearance, 1.5);
      using IHtmlDocument document = new HtmlParser().ParseDocument(page);
      Assert.Single(document.QuerySelectorAll("style"));
      Assert.Empty(document.QuerySelectorAll("script"));
      Assert.Equal("Safe", document.QuerySelector("#content-inner p")!.TextContent);
      Assert.Contains("var(--zoom)", document.QuerySelector("style")!.TextContent, StringComparison.Ordinal);
   }



   /// <summary>Missing required native state never yields a partially configured document page.</summary>
   [Theory]
   [InlineData(0)]
   [InlineData(1)]
   [InlineData(2)]
   [InlineData(3)]
   public void Page_MissingRequiredState_RejectsInput(int missing)
   {
      Assert.Throws<ArgumentNullException>(() => DocumentPage.Build(missing == 0 ? null! : "<p>Safe</p>", missing == 1 ? null! : "Title", missing == 2 ? null! : ThemeCatalog.Find(null), missing == 3 ? null! : new ReadingAppearance(), 1));
   }



   /// <summary>Native default/limited print layouts release the reader scroll host and avoid applying inner padding a second time.</summary>
   [Fact]
   public void PrintStyles_OriginalContainers_ExpandAndApplyPaddingOnce()
   {
      ReaderTheme theme = ThemeCatalog.Find(null);
      string full = PrintStyles.Build(new PrintOptions(PaddingPixels: 16), theme, ThemeCatalog.BuiltIn, 1);
      string limited = PrintStyles.Build(new PrintOptions(FullPage: false, PaddingPixels: 16), theme, ThemeCatalog.BuiltIn, 1);
      Assert.Contains("html,body,#content{height:auto!important;overflow:visible!important;display:block!important;}", full, StringComparison.Ordinal);
      Assert.Contains("#content-inner{padding:0!important;}", full, StringComparison.Ordinal);
      Assert.Contains("body{padding:16px!important;}", full, StringComparison.Ordinal);
      Assert.Contains("#content-inner{width:auto!important;max-width:none!important;margin:0!important;}", full, StringComparison.Ordinal);
      Assert.DoesNotContain("#content-inner{max-width:none!important", limited, StringComparison.Ordinal);
      Assert.Contains("#content-inner{max-width:900px;margin:0 auto;}", limited, StringComparison.Ordinal);
   }



   /// <summary>Only fixed passive reference classes or a bounded code language survive, without IDs, styles, event handlers or host attributes.</summary>
   [Fact]
   public void Render_StylingClasses_AllowOnlyPassiveReferenceNames()
   {
      const string Markdown = "<div class='frontmatter unknown fm-key fm-value frontmatter' id='content' style='display:none' data-mv-body='0' onclick='evil()'>Metadata</div>\n\n<code class='language-csharp language-c++ language-c# language-a_b language- language-é arbitrary'>Code</code>\n\n<p class='unknown'>Literal</p>\n\n<ul class='task-list'><li class='task-list-item'><input type='checkbox'></li></ul>";
      using IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer().Render(Markdown));
      Assert.Equal("frontmatter fm-key fm-value", document.QuerySelector("div")!.GetAttribute("class"));
      Assert.Equal("language-csharp language-c++ language-c# language-a_b", document.QuerySelector("code")!.GetAttribute("class"));
      Assert.Null(document.QuerySelector("p")!.GetAttribute("class"));
      Assert.NotNull(document.QuerySelector(".task-list .task-list-item input[disabled]"));
      Assert.Empty(document.QuerySelectorAll("[id], [style], [onclick], [data-mv-body]"));
      using IHtmlDocument oversized = new HtmlParser().ParseDocument(new MarkdownRenderer().Render("<code class='language-" + new string('x', 81) + "'>Code</code>"));
      Assert.Null(oversized.QuerySelector("code")!.GetAttribute("class"));
   }



   /// <summary>Parses CSS using the dependency already present for sanitization; modern variables and WebView vendor pseudo-elements remain inspectable.</summary>
   private static ICssStyleRule[] Rules(string css) => new CssParser(new CssParserOptions { IsIncludingUnknownDeclarations = true, IsToleratingInvalidSelectors = true }).ParseStyleSheet(css).Rules.OfType<ICssStyleRule>().ToArray();



   /// <summary>Each declaration cascades independently; later rules that omit a property do not erase its earlier value.</summary>
   private static string Property(IEnumerable<ICssStyleRule> rules, string selector, string name) => rules.Reverse().Where(rule => string.Equals(rule.SelectorText, selector, StringComparison.Ordinal)).Select(rule => rule.Style.GetPropertyValue(name)).First(value => value.Length > 0);
}
