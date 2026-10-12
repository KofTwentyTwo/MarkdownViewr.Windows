/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Proves paired counts, literal positions and navigation for the real two-surface find controls.</summary>
public sealed class DocumentFindTests
{
   /// <summary>Formatting and case differences retain offsets in the original source rather than rendered text.</summary>
   [Fact]
   public void Create_MixedCaseSource_ReturnsLiteralPositions()
   {
      var find = new DocumentFind("**Alpha**\nALPHA alpha", "alpha");
      Assert.Equal("alpha", find.Query);
      Assert.Equal(new[] { 2, 10, 16 }, find.SourceMatches);
      Assert.Equal(3, find.Total);
      Assert.Equal(1, find.Current);
      Assert.Equal((2, 5), find.SourceSelection);
      find.SetRenderedCount(3);
      Assert.Equal(3, find.Total);
   }



   /// <summary>Markup/script/regex punctuation is ordinary search text and cannot become executable input.</summary>
   [Theory]
   [InlineData("before a.*[x] after", "a.*[x]", 7)]
   [InlineData("before <script>alert('x')</script> after", "<script>alert('x')</script>", 7)]
   [InlineData("🙂 CAFÉ then café", "café", 3)]
   [InlineData("banana", "ana", 1)]
   public void Create_LiteralQuery_FindsNonoverlappingRanges(string source, string query, int offset)
   {
      var find = new DocumentFind(source, query);
      Assert.Equal(offset, find.SourceMatches[0]);
      Assert.Equal((offset, query.Length), find.SourceSelection);
      if(string.Equals(query, "ana", StringComparison.Ordinal))
      {
         Assert.Single(find.SourceMatches);
      }
   }



   /// <summary>Empty/absent/longer-than-source queries leave navigation inert.</summary>
   [Theory]
   [InlineData("text", "")]
   [InlineData("text", "missing")]
   [InlineData("", "query")]
   [InlineData("longer source", "absent")]
   public void Create_NoMatch_LeavesZeroCount(string source, string query)
   {
      var find = new DocumentFind(source, query);
      find.Next();
      find.Previous();
      find.SetRenderedCount(-1);
      Assert.Empty(find.SourceMatches);
      Assert.Equal(0, find.Total);
      Assert.Equal(0, find.Current);
      Assert.Null(find.SourceSelection);
   }



   /// <summary>The larger surface count wins, with wraparound and no false range for a rendered-only occurrence.</summary>
   [Fact]
   public void Navigate_DifferentSurfaceCounts_WrapsPairedOccurrences()
   {
      var find = new DocumentFind("one", "one");
      find.SetRenderedCount(3);
      Assert.Equal(3, find.Total);
      find.Previous();
      Assert.Equal(3, find.Current);
      Assert.Null(find.SourceSelection);
      find.Next();
      Assert.Equal(1, find.Current);
      Assert.Equal((0, 3), find.SourceSelection);
      find.Next();
      Assert.Equal(2, find.Current);
      find.Previous();
      Assert.Equal(1, find.Current);
      find.Previous();
      find.SetRenderedCount(0);
      Assert.Equal(1, find.Current);
      Assert.Equal(1, find.Total);
   }



   /// <summary>A rendered-only query acquires its first occurrence when the native browser reports matches.</summary>
   [Fact]
   public void SetCount_RenderedOnlyMatch_InitializesAndClearsCurrent()
   {
      var find = new DocumentFind("![caption](image.png)", "visible");
      find.SetRenderedCount(2);
      Assert.Equal(1, find.Current);
      Assert.Null(find.SourceSelection);
      find.Next();
      Assert.Equal(2, find.Current);
      find.SetRenderedCount(-1);
      Assert.Equal(0, find.Current);
   }



   /// <summary>Empty terms ignore late counts; native integer limits cannot overflow wraparound arithmetic.</summary>
   [Fact]
   public void Navigate_EmptyAndMaximumCounts_StaysInRange()
   {
      var empty = new DocumentFind("source", "");
      empty.SetRenderedCount(9);
      Assert.Equal(0, empty.Total);
      var maximum = new DocumentFind("source", "missing");
      maximum.SetRenderedCount(int.MaxValue);
      maximum.Previous();
      Assert.Equal(int.MaxValue, maximum.Current);
      maximum.Next();
      Assert.Equal(1, maximum.Current);
   }


   /// <summary>Programmatic callers cannot bypass the same document/query limits used by native controls.</summary>
   [Fact]
   public void Create_InvalidInput_RejectsBeforeSearch()
   {
      Assert.Throws<ArgumentNullException>(() => new DocumentFind(null!, "query"));
      Assert.Throws<ArgumentNullException>(() => new DocumentFind("source", null!));
      Assert.Throws<ArgumentException>(() => new DocumentFind(new string('x', MarkdownRenderer.MaximumCharacters + 1), "query"));
      Assert.Throws<ArgumentException>(() => new DocumentFind("source", new string('x', DocumentFind.MaximumQueryCharacters + 1)));
   }
}
