/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Source colors come from parser spans without modifying literal Unicode, whitespace or inert markup.</summary>
public sealed class SourceHighlightingTests
{
   /// <summary>Common Markdown forms retain exact parser locations, including nested inline forms and task lists.</summary>
   [Theory]
   [InlineData("😀 Intro\r\n# Heading", SourceTokenKind.Heading, "# Heading")]
   [InlineData("Title\n=====", SourceTokenKind.Heading, "Title\n=====")]
   [InlineData("A `code` word", SourceTokenKind.Code, "`code`")]
   [InlineData("```cs\nvar x = 1;\n```", SourceTokenKind.Code, "```cs\nvar x = 1;\n```")]
   [InlineData("    indented\n", SourceTokenKind.Code, "    indented")]
   [InlineData("A *word*", SourceTokenKind.Emphasis, "*word*")]
   [InlineData("A **word**", SourceTokenKind.Strong, "**word**")]
   [InlineData("A ~~word~~", SourceTokenKind.Strikethrough, "~~word~~")]
   [InlineData("[label](https://example.invalid)", SourceTokenKind.Link, "[label](https://example.invalid)")]
   [InlineData("<https://example.invalid>", SourceTokenKind.Link, "<https://example.invalid>")]
   [InlineData("![image](pic.png)", SourceTokenKind.Image, "![image](pic.png)")]
   [InlineData("- item", SourceTokenKind.List, "-")]
   [InlineData("12. item", SourceTokenKind.List, "12.")]
   [InlineData("- [x] task", SourceTokenKind.List, "[x]")]
   [InlineData("> quote", SourceTokenKind.Quote, "> quote")]
   [InlineData("---\nname: literal\n---\nText", SourceTokenKind.Frontmatter, "---\nname: literal\n---")]
   [InlineData("<script>alert('literal')</script>", SourceTokenKind.Html, "<script>alert('literal')</script>")]
   [InlineData("A <b>literal</b> word", SourceTokenKind.Html, "<b>")]
   public void Parse_MarkdownForms_RetainsLiteralSpans(string source, SourceTokenKind kind, string expected)
   {
      IReadOnlyList<SourceToken> tokens = MarkdownSourceHighlighting.Parse(source);
      Assert.Contains(tokens, token => token.Kind == kind && string.Equals(source.Substring(token.Start, token.Length), expected, StringComparison.Ordinal));
      Assert.All(tokens, token => Assert.InRange(token.Start + token.Length, 1, source.Length));
   }



   /// <summary>Parent colors precede child emphasis, while ordinary whitespace, escaped syntax and fences stay literal.</summary>
   [Fact]
   public void Parse_NestedHeadingAndEscapes_OrdersOverridesWithoutInventingMarkup()
   {
      const string Source = "# **Bold** and *soft*\n\n\\*literal*\n\n```\n# not a heading\n```";
      IReadOnlyList<SourceToken> tokens = MarkdownSourceHighlighting.Parse(Source);
      Assert.Equal(SourceTokenKind.Heading, tokens[0].Kind);
      Assert.Single(tokens, token => token.Kind == SourceTokenKind.Heading);
      Assert.Single(tokens, token => token.Kind == SourceTokenKind.Strong);
      Assert.Single(tokens, token => token.Kind == SourceTokenKind.Emphasis);
      Assert.Empty(MarkdownSourceHighlighting.Parse("  plain 😀 text\r\n\t"));
      Assert.Empty(MarkdownSourceHighlighting.Parse(""));
   }



   /// <summary>The supported size limit and cancellation reject work before parsing, rather than silently truncating source.</summary>
   [Fact]
   public void Parse_InvalidOrCancelledInput_RejectsWork()
   {
      Assert.Throws<ArgumentNullException>(() => MarkdownSourceHighlighting.Parse(null!));
      Assert.Throws<ArgumentException>(() => MarkdownSourceHighlighting.Parse(new string('x', MarkdownRenderer.MaximumCharacters + 1)));
      using var cancellation = new CancellationTokenSource();
      cancellation.Cancel();
      Assert.Throws<OperationCanceledException>(() => MarkdownSourceHighlighting.Parse("# Heading", cancellation.Token));
   }
}
