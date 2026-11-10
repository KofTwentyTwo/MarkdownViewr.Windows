/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>
/// Deeply nested markup must be refused as an ordinary error. The renders here run on a thread
/// with a quarter of the default stack, so a recursive walk that the bound failed to stop would
/// end the test run instead of passing by luck.
/// </summary>
public sealed class HtmlNestingTests
{
   private const int SmallStackBytes = 256 * 1024;



   /// <summary>The deepest markup the bound allows renders completely, with stack to spare.</summary>
   [Fact]
   public void Render_NestingAtTheLimit_RendersOnASmallStack()
   {
      int levels = HtmlNesting.MaximumDepth - 1;
      string markup = Repeat("<div>", levels) + "deepest" + Repeat("</div>", levels);
      string? html = null;
      Assert.Null(OnSmallStack(() => html = new MarkdownRenderer().Render(markup)));
      using IHtmlDocument parsed = new HtmlParser().ParseDocument(html!);
      Assert.Equal(levels, parsed.QuerySelectorAll("div").Length);
      Assert.Contains("deepest", html, StringComparison.Ordinal);
      Assert.Equal(256, HtmlNesting.MaximumDepth);
   }



   /// <summary>One level past the limit is an argument error, the failure the reader already reports for an unsupported document.</summary>
   [Fact]
   public void Render_NestingOnePastTheLimit_IsRejected()
   {
      string markup = Repeat("<div>", HtmlNesting.MaximumDepth) + "too deep";
      Assert.IsType<ArgumentException>(OnSmallStack(() => new MarkdownRenderer().Render(markup)));
   }



   /// <summary>
   /// A few kilobytes of nested elements, in every shape that nests, are refused before any
   /// library walks the tree recursively.
   /// </summary>
   [Theory]
   [InlineData("<div>", 3000)]
   [InlineData("<b>", 3000)]
   [InlineData("<span><i>", 1500)]
   [InlineData("<table><tr><td>", 3000)]
   [InlineData("<blockquote>", 2000)]
   [InlineData("<ul><li>", 2000)]
   [InlineData("<details><summary>s</summary>", 1500)]
   [InlineData("<div></section>", 3000)]
   public void Render_DeeplyNestedMarkup_IsRejectedBeforeAnyRecursiveWalk(string unit, int count)
   {
      string markup = Repeat(unit, count) + "x";
      Assert.IsType<ArgumentException>(OnSmallStack(() => new MarkdownRenderer().Render(markup)));
      Assert.IsType<ArgumentException>(OnSmallStack(() => _ = new DocumentOutline(markup)));
   }



   /// <summary>
   /// Nesting far past the limit is stopped by the count kept while tokens arrive, not after the
   /// whole input has been parsed: parsing it to the end takes time that grows with its square.
   /// </summary>
   [Fact]
   public void Render_RunawayNesting_IsStoppedWhileParsing()
   {
      string block = Repeat("<div>", 100000) + "x";
      ArgumentException stoppedInBlock = Assert.IsType<ArgumentException>(OnSmallStack(() => new MarkdownRenderer().Render(block)));
      Assert.Equal("token", stoppedInBlock.ParamName);
      string inline = "Paragraph " + Repeat("<b>", 100000) + "x";
      ArgumentException stoppedInline = Assert.IsType<ArgumentException>(OnSmallStack(() => new MarkdownRenderer().Render(inline)));
      Assert.Equal("token", stoppedInline.ParamName);
      ArgumentException measured = Assert.IsType<ArgumentException>(OnSmallStack(() => new MarkdownRenderer().Render(Repeat("<div>", 300) + "x")));
      Assert.Equal("root", measured.ParamName);
   }



   /// <summary>Nesting assembled from inline tags in a paragraph, or from many separate blocks, is measured as a whole.</summary>
   [Theory]
   [InlineData("Paragraph ", "<b>", "")]
   [InlineData("", "<div>", "\n\n")]
   [InlineData("- item ", "<span>", "")]
   [InlineData("> quoted ", "<i>", "")]
   public void Render_NestingAcrossInlineTagsAndBlocks_IsRejected(string prefix, string unit, string separator)
   {
      string markup = prefix + string.Concat(Enumerable.Repeat(unit + separator, 400)) + "x";
      Assert.IsType<ArgumentException>(OnSmallStack(() => new MarkdownRenderer().Render(markup)));
   }



   /// <summary>Nesting hidden in frontmatter rendered as Markdown is bounded like the body.</summary>
   [Fact]
   public void Render_DeepNestingInMarkdownFrontmatter_IsRejected()
   {
      string markup = "---\n" + Repeat("<div>", 3000) + "\n---\nBody";
      Assert.IsType<ArgumentException>(OnSmallStack(() => new MarkdownRenderer().Render(markup, FrontmatterMode.Markdown)));
      Assert.Null(OnSmallStack(() => new MarkdownRenderer().Render(markup, FrontmatterMode.Hide)));
   }



   /// <summary>Wide documents are not deep: thousands of siblings, unclosed paragraphs, list items and cells render normally.</summary>
   [Theory]
   [InlineData("<div>x</div>")]
   [InlineData("<p>x")]
   [InlineData("x<br>")]
   [InlineData("<span>x</span><b>y</b>")]
   public void Render_ManySiblings_AreNotMistakenForNesting(string unit)
   {
      string markup = "<div>" + Repeat(unit, HtmlNesting.MaximumOpenTags + 500) + "</div>";
      string? html = null;
      Assert.Null(OnSmallStack(() => html = new MarkdownRenderer().Render(markup)));
      Assert.Contains("x", html, StringComparison.Ordinal);
   }



   /// <summary>List items and table cells left unclosed are siblings, however many there are.</summary>
   [Fact]
   public void Render_ManyUnclosedItemsAndCells_AreNotMistakenForNesting()
   {
      string markup = "<ul>" + Repeat("<li>item", 5000) + "</ul>\n\n<table>" + Repeat("<tr><td>a<td>b", 5000) + "</table>";
      string? html = null;
      Assert.Null(OnSmallStack(() => html = new MarkdownRenderer().Render(markup)));
      using IHtmlDocument parsed = new HtmlParser().ParseDocument(html!);
      Assert.Equal(5000, parsed.QuerySelectorAll("li").Length);
      Assert.Equal(10000, parsed.QuerySelectorAll("td").Length);
   }



   /// <summary>The early count allows exactly its bound of open tags and stops at the next one.</summary>
   [Theory]
   [InlineData(HtmlNesting.MaximumOpenTags, false)]
   [InlineData(HtmlNesting.MaximumOpenTags + 1, true)]
   public void Observe_OpenTagsAtAndPastTheBound_StopsExactlyPastIt(int count, bool stopped)
   {
      Assert.Equal(stopped, Record.Exception(() => Tokenize(Repeat("<div>", count))) is ArgumentException);
      Assert.Equal(4096, HtmlNesting.MaximumOpenTags);
   }



   /// <summary>
   /// The early count never falls below the real depth: an end tag closes only the tag on top,
   /// so an end tag the parser would ignore cannot be used to hide nesting from the count.
   /// </summary>
   [Theory]
   [InlineData("<div></span>")]
   [InlineData("<div><table><tr><td></div>")]
   [InlineData("<section><p></article>")]
   public void Observe_EndTagThatDoesNotCloseTheOpenTag_DoesNotReduceTheCount(string unit)
      => Assert.IsType<ArgumentException>(Record.Exception(() => Tokenize(Repeat(unit, HtmlNesting.MaximumOpenTags + 1))));



   /// <summary>Properly closed elements, void elements and self-closing siblings never accumulate.</summary>
   [Theory]
   [InlineData("<div><span>x</span></div>")]
   [InlineData("<img src=x><br><hr><input>")]
   [InlineData("<p>x<li>y<td>z<option>o")]
   public void Observe_ClosedVoidAndImplicitlyClosedTags_DoNotAccumulate(string unit)
      => Assert.Null(Record.Exception(() => Tokenize(Repeat(unit, HtmlNesting.MaximumOpenTags + 1))));



   /// <summary>The exact measurement walks every branch and counts from the root it is given.</summary>
   [Fact]
   public void ThrowIfTooDeep_ShallowWideAndBranchingTrees_AreAccepted()
   {
      using IHtmlDocument empty = new HtmlParser().ParseDocument("");
      HtmlNesting.ThrowIfTooDeep(empty.Body!);
      using IHtmlDocument branching = new HtmlParser().ParseDocument(Repeat("<div>", 200) + "a" + Repeat("</div>", 200) + "<p>b</p>" + Repeat("<span>c</span>", 2000) + Repeat("<div>", 254) + "d");
      HtmlNesting.ThrowIfTooDeep(branching.Body!);
      using IHtmlDocument late = new HtmlParser().ParseDocument("<p>first</p><p>second</p>" + Repeat("<div>", 256) + "deep");
      Assert.Throws<ArgumentException>(() => HtmlNesting.ThrowIfTooDeep(late.Body!));
   }



   /// <summary>Feeds markup through a parser whose tokens go to a fresh count, as the renderer does.</summary>
   private static void Tokenize(string html)
   {
      var nesting = new HtmlNesting();
      using IHtmlDocument document = new HtmlParser(new HtmlParserOptions { OnToken = (token, _) => nesting.Observe(token) }).ParseDocument(html);
   }



   /// <summary>Runs on a thread with a quarter of the default stack and returns what it threw, if anything.</summary>
   private static Exception? OnSmallStack(Action action)
   {
      Exception? failure = null;
      var thread = new Thread(() => failure = Record.Exception(action), SmallStackBytes);
      thread.Start();
      thread.Join();
      return failure;
   }



   /// <summary>The unit repeated without separators.</summary>
   private static string Repeat(string unit, int count) => string.Concat(Enumerable.Repeat(unit, count));
}
