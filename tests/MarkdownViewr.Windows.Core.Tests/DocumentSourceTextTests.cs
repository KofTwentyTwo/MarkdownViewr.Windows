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
      Assert.Equal(original.ReplaceLineEndings("\r"), source.Text);
      Assert.Equal(4, source.Headings.Count);
      Assert.True(source.SetCollapsed([1]));
      Assert.Contains("Parent body", source.Text, StringComparison.Ordinal);
      Assert.Contains("## Child\r⋯\r> ## Peer", source.Text, StringComparison.Ordinal);
      Assert.DoesNotContain("# Fake", source.Text, StringComparison.Ordinal);
      Assert.Null(source.GetDisplayRange(original.IndexOf("Child body", StringComparison.Ordinal), 5));
      Assert.True(source.SetCollapsed([0, 1]));
      Assert.Contains("Parent\r======\r⋯\r# Last", source.Text, StringComparison.Ordinal);
      Assert.DoesNotContain("Child", source.Text, StringComparison.Ordinal);
      Assert.Equal(new[] { 0, 3 }, source.Headings.Select(heading => heading.Index));
      Assert.True(source.SetCollapsed([1]));
      Assert.Contains("> ## Peer", source.Text, StringComparison.Ordinal);
      Assert.DoesNotContain("Child body", source.Text, StringComparison.Ordinal);
      Assert.False(source.SetCollapsed([1, 1]));
      Assert.True(source.SetCollapsed([]));
      Assert.Equal(original.ReplaceLineEndings("\r"), source.Text);
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
      Assert.Equal(original.ReplaceLineEndings("\r"), source.Text);
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
      Assert.Contains("# Last\rTail", source.Text, StringComparison.Ordinal);
   }



   /// <summary>A folded multiline HTML heading retains all authored attributes, nested title markup and closing syntax.</summary>
   [Theory]
   [InlineData("\n")]
   [InlineData("\r\n")]
   [InlineData("\r")]
   public void SetCollapsed_RawMultilineHeading_PreservesCompleteHeader(string newline)
   {
      string header = string.Join(newline, "<H2", " title='quoted > value'>", "Raw <em>child</em>", "<!-- </h2> is a comment -->", "</H2>");
      string original = "# Root" + newline
         + header + newline + "Hidden body" + newline + "<h3>First peer</h3><h3>Second peer</h3>" + newline + "Peer body" + newline + newline + "# Last" + newline + "Tail";
      var source = new DocumentSourceText(original, new DocumentOutline(original));
      Assert.True(source.SetCollapsed([1]));
      Assert.Contains(header.ReplaceLineEndings("\r"), source.Text, StringComparison.Ordinal);
      Assert.DoesNotContain("Hidden body", source.Text, StringComparison.Ordinal);
      Assert.DoesNotContain("First peer", source.Text, StringComparison.Ordinal);
      Assert.Contains("# Last\rTail", source.Text, StringComparison.Ordinal);
      Assert.Equal(new[] { 0, 1, 4 }, source.Headings.Select(heading => heading.Index));
      Assert.True(source.SetCollapsed([]));
      Assert.Equal(original, source.GetCopyText(0, source.Text.Length));
   }



   /// <summary>Inline raw headings may span Markdown lines; folding preserves their complete authored title and closing tag.</summary>
   [Theory]
   [InlineData("<h2 title='inline'>Raw <em>inline</em>\nheading</h2>")]
   [InlineData("<h2>Raw `</h2><h3>fake</h3>`<!-- </h2> -->\nheading</h2>")]
   public void SetCollapsed_RawInlineMultilineHeading_PreservesCompleteHeader(string header)
   {
      string original = "# Root\n\nIntro " + header + "\n\nHidden body\n\n# Last\nTail";
      var source = new DocumentSourceText(original, new DocumentOutline(original));
      Assert.Equal(3, source.Headings.Count);
      Assert.True(source.SetCollapsed([1]));
      Assert.Contains(header.ReplaceLineEndings("\r"), source.Text, StringComparison.Ordinal);
      Assert.DoesNotContain("Hidden body", source.Text, StringComparison.Ordinal);
      Assert.Contains("# Last\rTail", source.Text, StringComparison.Ordinal);
      Assert.True(source.SetCollapsed([]));
      Assert.Equal(original, source.GetCopyText(0, source.Text.Length));
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


   /// <summary>Native CR display does not change copied LF/CRLF/lone CR, Unicode or literal authored markup.</summary>
   [Theory]
   [InlineData("")]
   [InlineData("😀  text\t\n<script>literal</script>\n")]
   [InlineData("# H\r\n**text**\r\n")]
   [InlineData("One\rTwo\nThree\r\n終 😀\r")]
   public void GetCopyText_OriginalLineEndings_RetainsExactSource(string original)
   {
      var source = new DocumentSourceText(original, new DocumentOutline(original));
      Assert.Equal(original, source.GetCopyText(0, source.Text.Length));
      Assert.Equal("", source.GetCopyText(source.Text.Length, 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => source.GetCopyText(-1, 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => source.GetCopyText(0, -1));
      Assert.Throws<ArgumentOutOfRangeException>(() => source.GetCopyText(source.Text.Length + 1, 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => source.GetCopyText(0, source.Text.Length + 1));
   }



   /// <summary>Selection boundaries include the LF only when the native CR is selected, preserving exact substrings.</summary>
   [Fact]
   public void GetCopyText_PartialNewlineAndUnicode_MapsActualSelection()
   {
      const string Original = "😀 A\r\nB\nC\rD\r\n終";
      var source = new DocumentSourceText(Original, new DocumentOutline(Original));
      Assert.Equal("😀", source.GetCopyText(0, 2));
      Assert.Equal("\r\n", source.GetCopyText(4, 1));
      Assert.Equal("B\nC\rD\r\n", source.GetCopyText(5, 6));
      Assert.Equal("終", source.GetCopyText(source.Text.Length - 1, 1));
      Assert.Equal("", source.GetCopyText(5, 0));
   }



   /// <summary>Folds copy only visible original chunks and a literal ellipsis; reopening restores the exact original.</summary>
   [Fact]
   public void GetCopyText_MergedAndIndependentFolds_OmitsHiddenBodies()
   {
      const string Original = "# First\r\nHidden\r\n## Nested\nMore\n# Second\nKeep 😀\r\n# Last\rTail";
      var source = new DocumentSourceText(Original, new DocumentOutline(Original));
      source.SetCollapsed([0, 1]);
      Assert.Equal("# First\r\n⋯\r# Second\nKeep 😀\r\n# Last\rTail", source.GetCopyText(0, source.Text.Length));
      int visible = source.Text.IndexOf("Keep", StringComparison.Ordinal);
      Assert.Equal("Keep 😀\r\n", source.GetCopyText(visible, 8));
      source.SetCollapsed([1, 2]);
      Assert.Equal("# First\r\nHidden\r\n## Nested\n⋯\r# Second\n⋯\r# Last\rTail", source.GetCopyText(0, source.Text.Length));
      source.SetCollapsed([]);
      Assert.Equal(Original, source.GetCopyText(0, source.Text.Length));
   }
}
