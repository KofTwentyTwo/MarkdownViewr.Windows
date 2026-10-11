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
      int start = 0;
      while(source.IndexOf('\n', start) is int end && end >= 0)
      {
         ends.Add(end);
         start = end + 1;
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
         else if(node is HtmlInline inline && IsHeadingTag(inline.Tag))
         {
            int start = offset + inline.Span.Start;
            inline.Tag = inline.Tag.Insert(3, Attribute(AddPosition(start, EndOfLine(start))));
         }
      }
      return Markdown.ToHtml(document, pipeline);
   }



   /// <summary>A real opening heading tag is distinguished from comments, closing tags and heading-like tag names.</summary>
   private static bool IsHeadingTag(string tag) => tag.Length > 3 && tag.StartsWith("<h", StringComparison.OrdinalIgnoreCase) && tag[2] is >= '1' and <= '6' && (tag[3] is '>' or '/' || char.IsWhiteSpace(tag[3]));



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



   /// <summary>Source slices retain quote/list indentation offsets while emitted raw HTML omits those Markdown prefixes.</summary>
   private void AnnotateHtmlBlock(HtmlBlock block, int offset)
   {
      StringLine[] lines = block.Lines.Lines;
      string html = string.Join('\n', lines.Take(block.Lines.Count).Select(line => line.Slice.ToString()));
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser(new HtmlParserOptions { IsKeepingSourceReferences = true }).ParseDocument(html);
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
         changes.Add((column + 3, Attribute(AddPosition(start, EndOfLine(start)))));
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
