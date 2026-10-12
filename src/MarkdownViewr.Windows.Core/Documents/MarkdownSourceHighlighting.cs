/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Markdig;
using Markdig.Extensions.TaskLists;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Classifies literal UTF16 source spans with the existing parser; no token renders or executes document content.</summary>
public static class MarkdownSourceHighlighting
{
   private static readonly MarkdownPipeline s_pipeline = new MarkdownPipelineBuilder().UsePipeTables().UseTaskLists().UseEmphasisExtras().UseAutoLinks().UseYamlFrontMatter().UsePreciseSourceLocation().Build();



   /// <summary>Parser work is bounded by the supported document limit; cancellation discards stale classification during traversal.</summary>
   public static IReadOnlyList<SourceToken> Parse(string source, CancellationToken cancellationToken = default)
   {
      ArgumentNullException.ThrowIfNull(source);
      if(source.Length > MarkdownRenderer.MaximumCharacters)
      {
         throw new ArgumentException("The source exceeds the document size limit.", nameof(source));
      }
      cancellationToken.ThrowIfCancellationRequested();
      MarkdownDocument document = Markdown.Parse(source, s_pipeline);
      var tokens = new List<SourceToken>();
      foreach(MarkdownObject node in document.Descendants())
      {
         cancellationToken.ThrowIfCancellationRequested();
         SourceTokenKind? kind = Kind(node);
         if(kind is { } value && node.Span.Start >= 0 && node.Span.End >= node.Span.Start && node.Span.End < source.Length)
         {
            int length = node.Span.End - node.Span.Start + 1;
            if(node is ListItemBlock)
            {
               length = ListMarkerLength(source.AsSpan(node.Span.Start, length));
            }
            if(length > 0)
            {
               tokens.Add(new SourceToken(node.Span.Start, length, value));
            }
         }
      }
      return tokens.AsReadOnly();
   }



   /// <summary>Preorder parent/child spans permit nested markup to refine a block's color without changing its literal characters.</summary>
   private static SourceTokenKind? Kind(MarkdownObject node) => node switch
   {
      YamlFrontMatterBlock => SourceTokenKind.Frontmatter,
      HeadingBlock => SourceTokenKind.Heading,
      CodeBlock or CodeInline => SourceTokenKind.Code,
      EmphasisInline emphasis when emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 2 => SourceTokenKind.Strikethrough,
      EmphasisInline emphasis when emphasis.DelimiterCount == 2 => SourceTokenKind.Strong,
      EmphasisInline => SourceTokenKind.Emphasis,
      LinkInline { IsImage: true } => SourceTokenKind.Image,
      LinkInline or AutolinkInline => SourceTokenKind.Link,
      ListItemBlock or TaskList => SourceTokenKind.List,
      QuoteBlock => SourceTokenKind.Quote,
      HtmlBlock or HtmlInline => SourceTokenKind.Html,
      _ => null,
   };



   /// <summary>Only a list marker is colored; ordinary list prose remains readable and child code/emphasis keeps its own style.</summary>
   private static int ListMarkerLength(ReadOnlySpan<char> span)
   {
      int length = 0;
      while(length < span.Length && char.IsAsciiDigit(span[length]))
      {
         length++;
      }
      // Markdig's valid ListItemBlock span begins at its bullet or ordered marker.
      return length + 1;
   }
}
