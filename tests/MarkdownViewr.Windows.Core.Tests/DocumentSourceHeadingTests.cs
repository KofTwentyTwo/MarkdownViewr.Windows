/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using AngleSharp.Html.Parser;
using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Proves original UTF-16 heading provenance through raw HTML, fences, frontmatter and passive sanitation.</summary>
public sealed class DocumentSourceHeadingTests
{
   /// <summary>Parser spans distinguish real Setext/ATX/nested headings from fenced pseudo-headings and repeated labels.</summary>
   [Theory]
   [InlineData("\n")]
   [InlineData("\r\n")]
   public void Create_MarkdownHeadings_MapsOriginalSyntax(string newline)
   {
      string source = string.Join(newline, "😀 Intro", "", "# Same", "", "```md", "# Same", "Fake", "====", "```", "", "Same", "====", "", "> ## Quoted", "", "- ### Listed", "", "Last", "----");
      var outline = new DocumentOutline(source);
      Assert.Equal(new[] { "Same", "Same", "Quoted", "Listed", "Last" }, outline.Headings.Select(heading => heading.Title), StringComparer.Ordinal);
      Assert.Equal(outline.Headings.Select(heading => heading.Index), outline.SourceHeadings.Select(heading => heading.Index));
      Assert.Equal(new[] { "# Same", "Same" + newline + "====", "## Quoted", "### Listed", "Last" + newline + "----" }, outline.SourceHeadings.Select(heading => source[heading.Start..heading.End].TrimEnd('\r')), StringComparer.Ordinal);
      Assert.DoesNotContain("data-mv-source-", outline.Html, StringComparison.OrdinalIgnoreCase);
   }



   /// <summary>HTML token locations account for Markdown quote/list prefixes and multiple tags on one source line.</summary>
   [Theory]
   [InlineData("<div><h2>Same</h2><h3>Same</h3></div>\n\n<H4\n title='name'>Four</H4>", 3)]
   [InlineData("> <div><h2>Same</h2>\n> <h3>Same</h3></div>", 2)]
   [InlineData("- <h2>Listed</h2>\n\n  <h3>Nested</h3>", 2)]
   [InlineData("Text <h2>Inline</h2> followed by <h3/> end", 2)]
   public void Create_RawHtmlHeadings_UsesExactOpeningOffsets(string source, int count)
   {
      var outline = new DocumentOutline(source);
      Assert.Equal(count, outline.Headings.Count);
      Assert.Equal(count, outline.SourceHeadings.Count);
      foreach(DocumentSourceHeading heading in outline.SourceHeadings)
      {
         Assert.Equal("<h" + outline.Headings[heading.Index].Level, source.Substring(heading.Start, 3), ignoreCase: true);
         Assert.InRange(heading.End, heading.Start + 3, source.Length);
      }
      Assert.Equal(outline.SourceHeadings.Count, outline.SourceHeadings.Select(heading => heading.Start).Distinct().Count());
      Assert.DoesNotContain("data-mv-source-", outline.Html, StringComparison.OrdinalIgnoreCase);
   }



   /// <summary>Hidden/metadata frontmatter retains body offsets; Markdown mode also maps metadata with its original delimiter spacing.</summary>
   [Theory]
   [InlineData(FrontmatterMode.Hide, 1)]
   [InlineData(FrontmatterMode.Metadata, 1)]
   [InlineData(FrontmatterMode.Markdown, 2)]
   public void Create_FrontmatterModes_MapsUnmodifiedSnapshot(FrontmatterMode mode, int count)
   {
      const string Source = "  --- \r\n# Metadata\r\n \t---  \r\n\r\n# Body\r\nContent";
      var outline = new DocumentOutline(Source, mode);
      Assert.Equal(count, outline.SourceHeadings.Count);
      Assert.Equal(Source.IndexOf("# Body", StringComparison.Ordinal), outline.SourceHeadings[^1].Start);
      Assert.Equal("# Body", Source[outline.SourceHeadings[^1].Start..outline.SourceHeadings[^1].End].TrimEnd('\r'));
      if(mode == FrontmatterMode.Markdown)
      {
         Assert.Equal(Source.IndexOf("# Metadata", StringComparison.Ordinal), outline.SourceHeadings[0].Start);
         Assert.Equal("# Metadata", Source[outline.SourceHeadings[0].Start..outline.SourceHeadings[0].End].TrimEnd('\r'));
      }
   }



   /// <summary>Removed active containers and authored identity attributes cannot impersonate surviving heading provenance.</summary>
   [Fact]
   public void Create_MaliciousMarkup_RetainsOnlyTrustedSurvivingPositions()
   {
      const string Source = "<script><h2>Same</h2></script>\n\n<iframe><h2>Same</h2></iframe>\n\n<h2 data-mv-source-fixed='0' data-mv-body='99' id='mv-heading-99' onclick='evil()'>Same</h2>\n\n# Same\n\nText <span>inline</span><!-- <h3>comment</h3> -->";
      var outline = new DocumentOutline(Source);
      Assert.Equal(new[] { "Same", "Same" }, outline.Headings.Select(heading => heading.Title), StringComparer.Ordinal);
      Assert.Equal(2, outline.SourceHeadings.Count);
      Assert.Equal(Source.IndexOf("<h2 data", StringComparison.Ordinal), outline.SourceHeadings[0].Start);
      Assert.Equal(Source.IndexOf("# Same", StringComparison.Ordinal), outline.SourceHeadings[1].Start);
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser().ParseDocument(outline.Html);
      Assert.Null(document.QuerySelector("script,iframe,[onclick],[data-mv-source-fixed],#mv-heading-99"));
      Assert.DoesNotContain("data-mv-source-", outline.Html, StringComparison.OrdinalIgnoreCase);
   }



   /// <summary>Empty input and no-heading HTML do not manufacture positions; invalid inputs keep the renderer's validation.</summary>
   [Theory]
   [InlineData("")]
   [InlineData("<div>ordinary body</div>")]
   [InlineData("Text <h22>ordinary</h22> &amp; <hr/> <!-- comment -->")]
   [InlineData("---\n\n---\nBody")]
   public void Create_NoHeadings_ReturnsEmptyPositions(string source)
   {
      Assert.Empty(new DocumentOutline(source).SourceHeadings);
   }



   /// <summary>Many raw headings on one line share one bounded line end without rescanning its entire remaining text per heading.</summary>
   [Fact]
   public void Create_ManyRawHeadingsOnOneLine_PreservesDistinctOffsets()
   {
      string source = string.Concat(Enumerable.Repeat("<h2>Same</h2>", 300));
      var outline = new DocumentOutline(source);
      Assert.Equal(300, outline.SourceHeadings.Count);
      for(int index = 0; index < outline.SourceHeadings.Count; index++)
      {
         Assert.Equal(index * "<h2>Same</h2>".Length, outline.SourceHeadings[index].Start);
         Assert.Equal(source.Length, outline.SourceHeadings[index].End);
      }
      Assert.DoesNotContain("data-mv-source-", outline.Html, StringComparison.OrdinalIgnoreCase);
   }



   /// <summary>A mapped render still rejects null, oversized content and corrupt frontmatter modes before use.</summary>
   [Fact]
   public void Create_InvalidInput_PreservesRenderingLimits()
   {
      Assert.Throws<ArgumentNullException>(() => new DocumentOutline(null!));
      Assert.Throws<ArgumentException>(() => new DocumentOutline(new string('x', MarkdownRenderer.MaximumCharacters + 1)));
      Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentOutline("# Body", (FrontmatterMode)99));
   }
}
