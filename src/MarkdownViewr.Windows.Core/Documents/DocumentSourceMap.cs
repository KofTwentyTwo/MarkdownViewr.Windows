/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Markdig;
using Markdig.Helpers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Carries parser provenance through sanitation using a fresh private attribute removed before presentation.</summary>
internal sealed class DocumentSourceMap
{
   private readonly int[] _lineEnds;
   private readonly Dictionary<string, (int Start, int End)> _positions = new(StringComparer.Ordinal);



   /// <summary>A document cannot predict the per-render attribute or provide its own source identities.</summary>
   public DocumentSourceMap(string source)
   {
      ArgumentNullException.ThrowIfNull(source);
      var ends = new List<int>();
      for(int index = 0; index < source.Length; index++)
      {
         if(source[index] is '\r' or '\n')
         {
            ends.Add(index);
            if(source[index] == '\r' && index + 1 < source.Length && source[index + 1] == '\n')
            {
               index++;
            }
         }
      }
      ends.Add(source.Length);
      _lineEnds = ends.ToArray();
      AttributeName = "data-mv-source-" + Guid.NewGuid().ToString("N");
   }



   /// <summary>Allowed only during this internal render, never in the public renderer's authored attribute policy.</summary>
   public string AttributeName { get; }



   /// <summary>Maps only surviving host-marked headings, not matching text labels or authored attributes.</summary>
   public bool TryGetPosition(string identity, out (int Start, int End) position) => _positions.TryGetValue(identity, out position);



   /// <summary>AST headings include Setext and exclude fences; raw HTML uses parser tokens and original line slices.</summary>
   public string Render(string markdown, MarkdownPipeline pipeline, int offset)
   {
      MarkdownDocument document = Markdown.Parse(markdown, pipeline);
      foreach(MarkdownObject node in document.Descendants())
      {
         if(node is HeadingBlock heading)
         {
            heading.GetAttributes().AddProperty(AttributeName, AddPosition(offset + heading.Span.Start, EndOfLine(offset + heading.Span.End)));
         }
         else if(node is HtmlBlock block)
         {
            AnnotateHtmlBlock(block, offset);
         }
         if(node is LeafBlock { Inline: not null } leaf)
         {
            AnnotateInlineHeadings(leaf, offset);
         }
      }
      return Markdown.ToHtml(document, pipeline);
   }



   /// <summary>A real opening heading tag is distinguished from comments, closing tags and heading-like tag names.</summary>
   private static bool IsHeadingTag(string tag) => tag.Length > 3 && tag.StartsWith("<h", StringComparison.OrdinalIgnoreCase) && tag[2] is >= '1' and <= '6' && (tag[3] is '>' or '/' || char.IsWhiteSpace(tag[3]));



   /// <summary>Pairs actual inline heading nodes within one Markdown leaf, excluding fenced/inline code and comment lookalikes.</summary>
   private void AnnotateInlineHeadings(LeafBlock leaf, int offset)
   {
      HtmlInline? opening = null;
      foreach(HtmlInline inline in leaf.Inline!.Descendants().OfType<HtmlInline>())
      {
         if(IsHeadingTag(inline.Tag))
         {
            if(opening is not null)
            {
               AnnotateInlineHeading(opening, opening.Span.End, offset);
            }
            opening = inline;
         }
         else if(opening is not null && inline.Tag.Length > 4 && inline.Tag.StartsWith("</h", StringComparison.OrdinalIgnoreCase)
            && inline.Tag[3] is >= '1' and <= '6' && (inline.Tag[4] == '>' || char.IsWhiteSpace(inline.Tag[4])))
         {
            AnnotateInlineHeading(opening, inline.Span.End, offset);
            opening = null;
         }
      }
      if(opening is not null)
      {
         AnnotateInlineHeading(opening, InlineHeadingEnd(leaf, opening), offset);
      }
   }



   /// <summary>Consecutive raw HTML blocks can finish an inline title; a new heading or Markdown block prevents borrowing later syntax.</summary>
   private static int InlineHeadingEnd(LeafBlock leaf, HtmlInline opening)
   {
      ContainerBlock parent = leaf.Parent!;
      for(int index = parent.IndexOf(leaf) + 1; index < parent.Count && parent[index] is HtmlBlock block; index++)
      {
         (int? end, bool boundary) = HeadingBoundary(block);
         if(end is { } close)
         {
            return close;
         }
         if(boundary)
         {
            break;
         }
      }
      return opening.Span.End;
   }



   /// <summary>Actual HTML tokens distinguish a finishing tag from a new heading, excluding comments and quoted-attribute lookalikes.</summary>
   private static (int? End, bool Boundary) HeadingBoundary(HtmlBlock block)
   {
      StringLine[] lines = block.Lines.Lines;
      string html = string.Join('\n', lines.Take(block.Lines.Count).Select(line => line.Slice.ToString()));
      int? close = null;
      bool seenTag = false;
      var parser = new HtmlParser(new HtmlParserOptions
      {
         OnToken = (token, range) =>
         {
            if(!seenTag && token.Type is HtmlTokenType.StartTag or HtmlTokenType.EndTag
               && token.Name.Length == 2 && token.Name[0] == 'h' && token.Name[1] is >= '1' and <= '6')
            {
               seenTag = true;
               if(token.Type == HtmlTokenType.EndTag)
               {
                  close = range.End.Index - 1;
               }
            }
         },
      });
      using AngleSharp.Html.Dom.IHtmlDocument document = parser.ParseDocument(html);
      if(close is not { } position)
      {
         return (null, seenTag);
      }
      int index = 0;
      while(position >= lines[index].Slice.Length)
      {
         position -= lines[index].Slice.Length + 1;
         index++;
      }
      return (lines[index].Slice.Start + position, true);
   }



   /// <summary>Retains original syntax through the parsed closing line while inserting only the private identity in the opening tag.</summary>
   private void AnnotateInlineHeading(HtmlInline opening, int end, int offset)
   {
      opening.Tag = opening.Tag.Insert(3, Attribute(AddPosition(offset + opening.Span.Start, EndOfLine(offset + end))));
   }



   /// <summary>Registers the parser-derived range under a generated identity; offsets stay in the immutable original snapshot.</summary>
   private string AddPosition(int start, int end)
   {
      string identity = _positions.Count.ToString(CultureInfo.InvariantCulture);
      _positions.Add(identity, (start, end));
      return identity;
   }



   /// <summary>Raw heading markup stays on its original source line; the canonical snapshot is never rewritten.</summary>
   private int EndOfLine(int start)
   {
      int index = Array.BinarySearch(_lineEnds, start);
      return _lineEnds[index >= 0 ? index : ~index];
   }



   /// <summary>The private marker contains only generated ASCII identities, never document text.</summary>
   private string Attribute(string identity) => " " + AttributeName + "=\"" + identity + "\"";



   /// <summary>Tokenizer spans preserve complete raw headings across lines without mistaking quoted attributes or comments for closing syntax.</summary>
   private static HtmlParser CreateSourceParser(Dictionary<int, int> headingEnds)
   {
      int pendingHeading = -1;
      return new HtmlParser(new HtmlParserOptions
      {
         IsKeepingSourceReferences = true,
         OnToken = (token, range) =>
         {
            if(token.Name.Length == 2 && token.Name[0] == 'h' && token.Name[1] is >= '1' and <= '6')
            {
               if(token.Type == HtmlTokenType.StartTag)
               {
                  pendingHeading = range.Start.Index;
                  headingEnds.Add(pendingHeading, range.End.Index - 1);
               }
               else if(token.Type == HtmlTokenType.EndTag && pendingHeading >= 0)
               {
                  headingEnds[pendingHeading] = range.End.Index - 1;
                  pendingHeading = -1;
               }
            }
         },
      });
   }



   /// <summary>Source slices retain quote/list indentation offsets while emitted raw HTML omits those Markdown prefixes.</summary>
   private void AnnotateHtmlBlock(HtmlBlock block, int offset)
   {
      StringLine[] lines = block.Lines.Lines;
      string html = string.Join('\n', lines.Take(block.Lines.Count).Select(line => line.Slice.ToString()));
      var headingEnds = new Dictionary<int, int>();
      using AngleSharp.Html.Dom.IHtmlDocument document = CreateSourceParser(headingEnds).ParseDocument(html);
      int[] lineStarts = new int[block.Lines.Count];
      int length = 0;
      for(int index = 0; index < lineStarts.Length; index++)
      {
         lineStarts[index] = length;
         length += lines[index].Slice.Length + 1;
      }
      var inserts = new Dictionary<int, List<(int Position, string Attribute)>>();
      foreach(IElement heading in document.QuerySelectorAll("h1,h2,h3,h4,h5,h6"))
      {
         int position = heading.SourceReference!.Position.Index;
         int index = Array.BinarySearch(lineStarts, position);
         if(index < 0)
         {
            index = ~index - 1;
         }
         int column = position - lineStarts[index];
         int start = offset + lines[index].Slice.Start + column;
         if(!inserts.TryGetValue(index, out List<(int Position, string Attribute)>? changes))
         {
            changes = [];
            inserts.Add(index, changes);
         }
         int endPosition = headingEnds[position];
         int endIndex = Array.BinarySearch(lineStarts, endPosition);
         endIndex = endIndex >= 0 ? endIndex : ~endIndex - 1;
         int end = offset + lines[endIndex].Slice.Start + endPosition - lineStarts[endIndex];
         changes.Add((column + 3, Attribute(AddPosition(start, EndOfLine(end)))));
      }
      foreach((int index, List<(int Position, string Attribute)> changes) in inserts)
      {
         string line = lines[index].Slice.ToString();
         foreach((int position, string attribute) in changes.OrderByDescending(change => change.Position))
         {
            line = line.Insert(position, attribute);
         }
         lines[index].Slice = new StringSlice(line) { NewLine = lines[index].Slice.NewLine };
      }
   }
}
