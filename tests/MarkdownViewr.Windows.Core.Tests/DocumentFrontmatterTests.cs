/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Checks delimiter fidelity, escaped metadata and all three reference display modes.</summary>
public sealed class DocumentFrontmatterTests
{
   /// <summary>Missing/unterminated delimiters preserve the complete original input.</summary>
   [Theory]
   [InlineData("")]
   [InlineData("---")]
   [InlineData("---\nunterminated")]
   [InlineData("before\n---\nkey: value\n---")]
   [InlineData("\n---\nkey: value\n---")]
   [InlineData("----\nkey: value\n---")]
   public void Split_NoCompleteLeadingBlock_PreservesDocument(string markdown)
   {
      (string? metadata, string body) = DocumentFrontmatter.Split(markdown);
      Assert.Null(metadata);
      Assert.Equal(markdown, body);
   }



   /// <summary>Windows newlines, whitespace delimiters, empty blocks and empty bodies are retained correctly.</summary>
   [Theory]
   [InlineData("---\nkey: value\n---\n# Body", "key: value", "# Body")]
   [InlineData("---\r\nkey: value\r\n---\r\n# Body\r\n", "key: value\r", "# Body\r\n")]
   [InlineData(" --- \nkey: value\n \t--- \nbody", "key: value", "body")]
   [InlineData("---\n---", "", "")]
   [InlineData("---\nkey: value\n---", "key: value", "")]
   [InlineData("---\n\n---\nbody", "", "body")]
   public void Split_CompleteBlock_PreservesMetadataAndBody(string markdown, string expectedMetadata, string expectedBody)
   {
      (string? metadata, string body) = DocumentFrontmatter.Split(markdown);
      Assert.Equal(expectedMetadata, metadata);
      Assert.Equal(expectedBody, body);
   }



   /// <summary>All modes retain the body; hide is the default and metadata never becomes active HTML.</summary>
   [Fact]
   public void Render_AllModes_PreservesBodyWithSafeMetadata()
   {
      const string Markdown = "---\ntitle: **Visible value**\nurl: https://example.com/a:b\n<script>bad()</script>\n\n---\n# Body heading";
      var renderer = new MarkdownRenderer();
      string hidden = renderer.Render(Markdown);
      Assert.Contains("<h1>Body heading</h1>", hidden, StringComparison.Ordinal);
      Assert.DoesNotContain("Visible value", hidden, StringComparison.Ordinal);
      string metadata = renderer.Render(Markdown, FrontmatterMode.Metadata);
      Assert.Contains("<th>title</th><td>Visible value</td>", metadata, StringComparison.Ordinal);
      Assert.Contains("https://example.com/a:b", metadata, StringComparison.Ordinal);
      Assert.Contains("&lt;script&gt;bad()&lt;/script&gt;", metadata, StringComparison.Ordinal);
      Assert.DoesNotContain("<script", metadata, StringComparison.Ordinal);
      Assert.Contains("<h1>Body heading</h1>", metadata, StringComparison.Ordinal);
      string rendered = renderer.Render(Markdown, FrontmatterMode.Markdown);
      Assert.Contains("<strong>Visible value</strong>", rendered, StringComparison.Ordinal);
      Assert.DoesNotContain("<script", rendered, StringComparison.Ordinal);
      Assert.Contains("<h1>Body heading</h1>", rendered, StringComparison.Ordinal);
   }



   /// <summary>Empty frontmatter emits no prefix; unknown enum values cannot change rendering policy.</summary>
   [Fact]
   public void Render_EmptyBlockAndInvalidMode_EnforcesContract()
   {
      var renderer = new MarkdownRenderer();
      Assert.Equal("<p>Body</p>\n", renderer.Render("---\n---\nBody", FrontmatterMode.Metadata));
      Assert.Throws<ArgumentOutOfRangeException>(() => renderer.Render("Body", (FrontmatterMode)99));
   }



   /// <summary>Metadata formatting follows the reference and never strips punctuation from inside words.</summary>
   [Theory]
   [InlineData("***Bold***", "Bold")]
   [InlineData("__Bold__", "Bold")]
   [InlineData("~~Removed~~", "Removed")]
   [InlineData("*Italic*", "Italic")]
   [InlineData("_Italic_", "Italic")]
   [InlineData("`code`", "code")]
   [InlineData("*", "")]
   [InlineData("a_b", "a_b")]
   [InlineData("O'Reilly", "O'Reilly")]
   public void RenderMetadata_FormattingMarkers_ProducesPlainText(string value, string expected)
   {
      Assert.Equal("<table><caption>Document metadata</caption><tr><th>key</th><td>" + System.Net.WebUtility.HtmlEncode(expected) + "</td></tr></table>", DocumentFrontmatter.RenderMetadata("key: " + value));
   }



   /// <summary>Blank rows disappear and colon-free values span both columns.</summary>
   [Fact]
   public void RenderMetadata_UnstructuredRows_EscapesText()
   {
      Assert.Equal("<table><caption>Document metadata</caption><tr><td colspan=\"2\">A &amp; B</td></tr></table>", DocumentFrontmatter.RenderMetadata(" \nA & B\n\t"));
   }



   /// <summary>Saved mode values are normalized, while corrupt preferences return to hiding metadata.</summary>
   [Theory]
   [InlineData("Hide", FrontmatterMode.Hide)]
   [InlineData("Metadata", FrontmatterMode.Metadata)]
   [InlineData("Markdown", FrontmatterMode.Markdown)]
   [InlineData("invalid", FrontmatterMode.Hide)]
   [InlineData("99", FrontmatterMode.Hide)]
   [InlineData(null, FrontmatterMode.Hide)]
   public void Sanitize_SavedMode_UsesSupportedChoice(string? name, FrontmatterMode expected)
   {
      var settings = new AppSettings { Frontmatter = name! };
      settings.Sanitize();
      Assert.Equal(expected, settings.FrontmatterKind);
      Assert.Equal(expected.ToString(), settings.Frontmatter);
   }
}
