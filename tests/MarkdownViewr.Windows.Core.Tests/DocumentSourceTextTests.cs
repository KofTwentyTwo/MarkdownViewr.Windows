/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Proves literal nested folding and original-to-native selection offsets without mutating document data.</summary>
public sealed class DocumentSourceTextTests
{
   /// <summary>Setext and fenced pseudo-headings retain correct independent body boundaries across native line endings.</summary>
   [Theory]
   [InlineData("\n")]
   [InlineData("\r\n")]
   [InlineData("\r")]
   public void SetCollapsed_NestedSections_PreservesLiteralSyntaxAndChoices(string newline)
   {
      string original = string.Join(newline, "😀 Intro", "", "Parent", "======", "Parent body", "", "## Child", "Child body", "```md", "# Fake", "```", "", "> ## Peer", "> Peer body", "", "# Last", "Tail");
      var source = new DocumentSourceText(original, new DocumentOutline(original));
      Assert.Equal(original.ReplaceLineEndings("\r\n"), source.Text);
      Assert.Equal(4, source.Headings.Count);
      Assert.True(source.SetCollapsed([1]));
      Assert.Contains("Parent body", source.Text, StringComparison.Ordinal);
      Assert.Contains("## Child\r\n⋯\r\n> ## Peer", source.Text, StringComparison.Ordinal);
      Assert.DoesNotContain("# Fake", source.Text, StringComparison.Ordinal);
      Assert.Null(source.GetDisplayRange(original.IndexOf("Child body", StringComparison.Ordinal), 5));
      Assert.True(source.SetCollapsed([0, 1]));
      Assert.Contains("Parent\r\n======\r\n⋯\r\n# Last", source.Text, StringComparison.Ordinal);
      Assert.DoesNotContain("Child", source.Text, StringComparison.Ordinal);
      Assert.Equal(new[] { 0, 3 }, source.Headings.Select(heading => heading.Index));
      Assert.True(source.SetCollapsed([1]));
      Assert.Contains("> ## Peer", source.Text, StringComparison.Ordinal);
      Assert.DoesNotContain("Child body", source.Text, StringComparison.Ordinal);
      Assert.False(source.SetCollapsed([1, 1]));
      Assert.True(source.SetCollapsed([]));
      Assert.Equal(original.ReplaceLineEndings("\r\n"), source.Text);
   }



   /// <summary>Selections and heading anchors remain exact after omitted bodies and UTF-16/native newline conversion.</summary>
   [Fact]
   public void GetDisplayRange_MixedNewlinesAndFold_MapsLiteralOccurrence()
   {
      const string Original = "😀 Intro\r\n# One\nHidden\n# Two\rVisible\r\n# Three";
      var source = new DocumentSourceText(Original, new DocumentOutline(Original));
      source.SetCollapsed([0]);
      int start = Original.IndexOf("Visible", StringComparison.Ordinal);
      (int Start, int Length) range = source.GetDisplayRange(start, "Visible".Length)!.Value;
      Assert.Equal("Visible", source.Text.Substring(range.Start, range.Length));
      Assert.Equal(source.Text.IndexOf("# Two", StringComparison.Ordinal), source.Headings.Single(heading => heading.Index == 1).Start);
      Assert.Equal("# Two", source.Text[source.Headings[1].Start..source.Headings[1].End]);
      Assert.Null(source.GetDisplayRange(Original.IndexOf("Hidden", StringComparison.Ordinal) - 1, 8));
      Assert.Equal((source.Text.Length, 0), source.GetDisplayRange(Original.Length, 0));
      Assert.Equal((0, 2), source.GetDisplayRange(0, 2));
      Assert.Throws<ArgumentOutOfRangeException>(() => source.GetDisplayRange(-1, 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => source.GetDisplayRange(0, -1));
      Assert.Throws<ArgumentOutOfRangeException>(() => source.GetDisplayRange(Original.Length + 1, 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => source.GetDisplayRange(0, Original.Length + 1));
   }



   /// <summary>Empty bodies, ordinary content and invalid collapse indices cannot hide another section or manufacture headings.</summary>
   [Theory]
   [InlineData("")]
   [InlineData("ordinary literal <script> text")]
   [InlineData("# Empty\n# Next")]
   public void SetCollapsed_NoBodyOrInvalidIndices_RetainsText(string original)
   {
      var source = new DocumentSourceText(original, new DocumentOutline(original));
      source.SetCollapsed([-1, 0, 99]);
      Assert.Equal(original.ReplaceLineEndings("\r\n"), source.Text);
      Assert.Throws<ArgumentNullException>(() => source.SetCollapsed(null!));
   }



   /// <summary>Raw tags on one line are retained literally; same-line peer headings cannot produce reversed body ranges.</summary>
   [Fact]
   public void SetCollapsed_RawSameLinePeers_RetainsHeadingLine()
   {
      const string Original = "<h2>One</h2><h2>Two</h2>\nBody\n\n# Last\nTail";
      var source = new DocumentSourceText(Original, new DocumentOutline(Original));
      source.SetCollapsed([0, 1]);
      Assert.Contains("<h2>One</h2><h2>Two</h2>", source.Text, StringComparison.Ordinal);
      Assert.DoesNotContain("Body", source.Text, StringComparison.Ordinal);
      Assert.Contains("# Last\r\nTail", source.Text, StringComparison.Ordinal);
   }



   /// <summary>Folding accepts only bounded canonical snapshots with matching source anchors.</summary>
   [Fact]
   public void Create_InvalidSnapshot_RejectsInput()
   {
      var outline = new DocumentOutline("# Heading");
      Assert.Throws<ArgumentNullException>(() => new DocumentSourceText(null!, outline));
      Assert.Throws<ArgumentNullException>(() => new DocumentSourceText("", null!));
      Assert.Throws<ArgumentException>(() => new DocumentSourceText(new string('x', MarkdownRenderer.MaximumCharacters + 1), outline));
      Assert.Throws<ArgumentException>(() => new DocumentSourceText("", outline));
   }
}
