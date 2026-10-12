/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using AngleSharp.Html.Parser;
using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Proves section boundaries and labels without giving authored markup native identities.</summary>
public sealed class DocumentOutlineTests
{
   /// <summary>Peer headings close their predecessor; ancestors close nested descendants.</summary>
   [Fact]
   public void Create_NestedAndDuplicateHeadings_GroupsCorrectContent()
   {
      var outline = new DocumentOutline("Intro\n\n# Parent\n\nBody one\n\n### Child\n\nNested body\n\n## Peer\n\nPeer body\n\n# Parent\n\nSecond parent");
      Assert.Equal(new[] { new DocumentHeading(0, 1, "Parent"), new DocumentHeading(1, 3, "Child"), new DocumentHeading(2, 2, "Peer"), new DocumentHeading(3, 1, "Parent") }, outline.Headings);
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser().ParseDocument(outline.Html);
      Assert.Contains("Intro", document.Body!.FirstElementChild!.TextContent, StringComparison.Ordinal);
      Assert.Contains("Nested body", document.QuerySelector("[data-mv-body='0'] [data-mv-body='1']")!.TextContent, StringComparison.Ordinal);
      Assert.NotNull(document.QuerySelector("[data-mv-body='0'] [data-mv-body='2']"));
      Assert.DoesNotContain("Second parent", document.QuerySelector("[data-mv-body='0']")!.TextContent, StringComparison.Ordinal);
      Assert.Contains("Second parent", document.QuerySelector("[data-mv-body='3']")!.TextContent, StringComparison.Ordinal);
      Assert.Equal(4, document.QuerySelectorAll("[id^='mv-heading-']").Length);
   }



   /// <summary>All heading levels and nested raw containers retain semantic heading text.</summary>
   [Fact]
   public void Create_AuthoredContainers_UsesSanitizedHostIdentities()
   {
      var outline = new DocumentOutline("<div id='mv-heading-0'><h4 onclick='evil()'>A &amp; <em>B</em></h4><p data-mv-body='0'>Nested</p><h5>Five</h5><h6>Six</h6></div>\n\n<h2 id='native'>Two</h2>\n\n<script>evil()</script>");
      Assert.Equal(new[] { 4, 5, 6, 2 }, outline.Headings.Select(heading => heading.Level));
      Assert.Equal("A & B", outline.Headings[0].Title);
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser().ParseDocument(outline.Html);
      Assert.Single(document.QuerySelectorAll("#mv-heading-0"));
      Assert.Null(document.QuerySelector("script,[onclick],#native"));
      Assert.Contains("Nested", document.QuerySelector("[data-mv-body='0']")!.TextContent, StringComparison.Ordinal);
      Assert.Null(document.QuerySelector("p[data-mv-body]"));
   }



   /// <summary>Ordinary text, empty documents and hidden metadata do not manufacture headings.</summary>
   [Theory]
   [InlineData("")]
   [InlineData("Text **only**")]
   [InlineData("---\n# Metadata\n---\nBody")]
   public void Create_NoHeadings_RetainsBody(string markdown)
   {
      var outline = new DocumentOutline(markdown);
      Assert.Empty(outline.Headings);
      Assert.DoesNotContain("data-mv-body", outline.Html, StringComparison.Ordinal);
   }



   /// <summary>Rendering frontmatter as Markdown participates in the same sanitized outline.</summary>
   [Fact]
   public void Create_MarkdownMetadata_IncludesVisibleHeadings()
   {
      var outline = new DocumentOutline("---\n# Metadata\n---\n# Body", FrontmatterMode.Markdown);
      Assert.Equal(new[] { "Metadata", "Body" }, outline.Headings.Select(heading => heading.Title), StringComparer.Ordinal);
   }



   /// <summary>Corrupt saved sizes and depth cannot create an unusable native sidebar.</summary>
   [Theory]
   [InlineData(-1, double.NaN, 1, 220)]
   [InlineData(99, double.PositiveInfinity, 6, 220)]
   [InlineData(3, 0, 3, 140)]
   [InlineData(4, 900, 4, 500)]
   [InlineData(2, 240, 2, 240)]
   public void Sanitize_OutlinePreferences_ClampsSupportedRange(int depth, double width, int expectedDepth, double expectedWidth)
   {
      var settings = new AppSettings { ShowOutline = true, OutlineDepth = depth, OutlineWidth = width };
      settings.Sanitize();
      Assert.True(settings.ShowOutline);
      Assert.Equal(expectedDepth, settings.OutlineDepth);
      Assert.Equal(expectedWidth, settings.OutlineWidth);
   }



   /// <summary>GFM fragments resolve duplicate and Unicode headings without retaining authored identifiers.</summary>
   [Fact]
   public void Create_FragmentLinks_ResolveGeneratedTargets()
   {
      var outline = new DocumentOutline("# Hello **world**!\n\n# Hello world!\n\n# Café\n\n[first](#hello-world) [duplicate](#hello-world-1) [unicode](#caf%C3%A9) [unknown](#unknown) [web](https://example.com)");
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser().ParseDocument(outline.Html);
      Assert.Equal(new[] { "#mv-heading-0", "#mv-heading-1", "#mv-heading-2", "#unknown", "https://example.com" }, document.QuerySelectorAll("a").Select(link => link.GetAttribute("href")), StringComparer.Ordinal);
   }
}
