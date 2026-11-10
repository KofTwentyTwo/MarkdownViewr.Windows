/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Malformed and very repetitive documents render, with work that grows in step with their size.</summary>
public sealed class RendererRobustnessTests
{
   /// <summary>An unterminated script followed by a comment opener reports a text run past the end of its block; the document still opens and stays passive.</summary>
   [Theory]
   [InlineData("<script>x<!--")]
   [InlineData("<script><!--")]
   [InlineData("<script>==a==\n<!--")]
   [InlineData("<script>*<!--")]
   [InlineData("<div>==kept==</div><script>\n<!--\n")]
   public void Render_UnterminatedScriptAndComment_OpensAsPassiveMarkup(string markdown)
   {
      string html = new MarkdownRenderer().Render(markdown + "\n\nAfter the block.");
      Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
      var outline = new DocumentOutline("# Title\n\n" + markdown);
      Assert.Equal("Title", Assert.Single(outline.Headings).Title);
      Assert.DoesNotContain("<script", outline.Html, StringComparison.OrdinalIgnoreCase);
   }



   /// <summary>
   /// Markup is parsed with scripting on, as the sanitizer library parses it: the content of a
   /// noscript element is text that ends at the first closing tag, wherever that tag appears.
   /// </summary>
   [Fact]
   public void Render_NoscriptContent_IsParsedAsTextEndingAtItsFirstClosingTag()
   {
      const string Markdown = "<noscript><p title=\"</noscript><img src=x onerror=execute() alt=after>\"></noscript>";
      using IHtmlDocument parsed = new HtmlParser().ParseDocument(new MarkdownRenderer().Render(Markdown));
      AngleSharp.Dom.IElement image = Assert.Single(parsed.QuerySelectorAll("img"));
      Assert.Equal(["alt", "src"], image.Attributes.Select(attribute => attribute.Name).Order(StringComparer.Ordinal), StringComparer.Ordinal);
      Assert.Empty(parsed.QuerySelectorAll("noscript,p[title]"));
   }



   /// <summary>Markup broken so that one text run starts inside another is left as authored there, formatted elsewhere, and still sanitized.</summary>
   [Fact]
   public void Render_OverlappingTextRuns_RendersWithoutRewritingTheOverlap()
   {
      const string Markdown = "<iframe srcdoc='<script>execute()</scr<pre><code>==literal== ^literal^ ++literal++</code></pre>\nipt>'></iframe></style><script>execute()</script>";
      string html = new MarkdownRenderer().Render(Markdown);
      using IHtmlDocument parsed = new HtmlParser().ParseDocument(html);
      Assert.Empty(parsed.QuerySelectorAll("script,iframe,style"));
      Assert.DoesNotContain("execute()</script>", html, StringComparison.Ordinal);
   }



   /// <summary>Every text run in a long raw block is formatted once, in place, in document order.</summary>
   [Fact]
   public void Render_ManyFormattedRunsInOneBlock_FormatsEachInOrder()
   {
      const int Runs = 3000;
      string block = "<div>" + string.Concat(Enumerable.Range(0, Runs).Select(index => $"<span>==m{index}== and ^s{index}^</span>")) + "</div>";
      using IHtmlDocument parsed = new HtmlParser().ParseDocument(new MarkdownRenderer().Render(block));
      Assert.Equal(Enumerable.Range(0, Runs).Select(index => "m" + index.ToString(System.Globalization.CultureInfo.InvariantCulture)), parsed.QuerySelectorAll("mark").Select(mark => mark.TextContent), StringComparer.Ordinal);
      Assert.Equal(Enumerable.Range(0, Runs).Select(index => "s" + index.ToString(System.Globalization.CultureInfo.InvariantCulture)), parsed.QuerySelectorAll("sup").Select(sup => sup.TextContent), StringComparer.Ordinal);
      Assert.Equal(Runs, parsed.QuerySelectorAll("span").Length);
   }



   /// <summary>A raw block whose text needs no formatting is passed through unchanged between its neighbours.</summary>
   [Fact]
   public void Render_FormattedRunsBetweenUnchangedText_KeepsTheTextAroundThem()
   {
      using IHtmlDocument parsed = new HtmlParser().ParseDocument(new MarkdownRenderer().Render("<div>before <b>==first==</b> middle <i>plain</i> ++last++ after</div>"));
      Assert.Equal("before first middle plain last after", parsed.Body!.TextContent.Trim());
      Assert.Equal("first", Assert.Single(parsed.QuerySelectorAll("mark")).TextContent);
      Assert.Equal("last", Assert.Single(parsed.QuerySelectorAll("ins")).TextContent);
   }



   /// <summary>Thousands of headings with one title get distinct anchors, and a link to any of them finds its heading.</summary>
   [Fact]
   public void Outline_ManyHeadingsWithTheSameTitle_AnchorEachOne()
   {
      const int Count = 4000;
      string markdown = string.Concat(Enumerable.Repeat("# Same\n\n", Count)) + "[first](#same) [second](#same-1) [last](#same-3999) [none](#same-4000)\n";
      var outline = new DocumentOutline(markdown);
      Assert.Equal(Count, outline.Headings.Count);
      using IHtmlDocument parsed = new HtmlParser().ParseDocument(outline.Html);
      Assert.Equal(["#mv-heading-0", "#mv-heading-1", "#mv-heading-3999", "#same-4000"], parsed.QuerySelectorAll("a").Select(link => link.GetAttribute("href")!), StringComparer.Ordinal);
   }



   /// <summary>A heading that is literally named like a generated anchor keeps its own anchor; repeats step past it.</summary>
   [Theory]
   [InlineData("# Same\n\n# Same-1\n\n# Same\n\n# Same\n\n", new[] { "#same", "#same-1", "#same-2", "#same-3" })]
   [InlineData("# Same-1\n\n# Same\n\n# Same\n\n# Same-1\n\n", new[] { "#same-1", "#same", "#same-2", "#same-1-1" })]
   [InlineData("# A\n\n# B\n\n# A\n\n# B\n\n", new[] { "#a", "#b", "#a-1", "#b-1" })]
   public void Outline_RepeatedAndLiteralSuffixTitles_AnchorInDocumentOrder(string headings, string[] anchors)
   {
      string markdown = headings + string.Concat(anchors.Select(anchor => $"[link]({anchor}) "));
      using IHtmlDocument parsed = new HtmlParser().ParseDocument(new DocumentOutline(markdown).Html);
      Assert.Equal(["#mv-heading-0", "#mv-heading-1", "#mv-heading-2", "#mv-heading-3"], parsed.QuerySelectorAll("a").Select(link => link.GetAttribute("href")!), StringComparer.Ordinal);
   }



   /// <summary>A load that is no longer wanted stops instead of building its outline.</summary>
   [Fact]
   public void Outline_CancelledBeforeItStarts_StopsWithoutBuilding()
   {
      using var cancellation = new CancellationTokenSource();
      cancellation.Cancel();
      Assert.Throws<OperationCanceledException>(() => new DocumentOutline("# Title\n\nBody", cancellation: cancellation.Token));
      Assert.Equal("Title", Assert.Single(new DocumentOutline("# Title\n\nBody", cancellation: CancellationToken.None).Headings).Title);
   }
}
