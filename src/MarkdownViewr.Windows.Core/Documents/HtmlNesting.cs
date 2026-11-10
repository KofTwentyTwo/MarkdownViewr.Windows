/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Parser.Tokens;


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>
/// Bounds how deeply authored HTML may nest. Libraries that walk the parsed tree recursively
/// overflow the stack on a few thousand levels, and a stack overflow cannot be caught, so the
/// bound is enforced before any such walk: a cheap count while tokens arrive stops runaway
/// input early, and an exact measurement of the parsed tree applies the real limit.
/// </summary>
internal sealed class HtmlNesting
{
   /// <summary>The deepest element nesting a document may have; ordinary documents use a few dozen levels.</summary>
   public const int MaximumDepth = 256;

   /// <summary>Start tags left open before parsing stops; an upper bound on depth that keeps the parser's work small.</summary>
   public const int MaximumOpenTags = 4096;

   private const string Message = "The document nests HTML more deeply than the supported limit.";

   private static readonly HashSet<string> s_neverOpen = new(StringComparer.Ordinal)
   {
      // Void elements have no content.
      "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr",
      // A new one of these closes the previous one, so siblings left unclosed do not nest.
      "caption", "colgroup", "dd", "dt", "li", "optgroup", "option", "p", "tbody", "td", "tfoot", "th", "thead", "tr",
   };

   private readonly Stack<string> _open = new();



   /// <summary>
   /// Counts start tags that are still open. An end tag closes only the tag on top, so the count
   /// can exceed the real depth for carelessly closed markup but never falls below it.
   /// </summary>
   public void Observe(HtmlToken token)
   {
      if(token.Type == HtmlTokenType.StartTag && !s_neverOpen.Contains(token.Name))
      {
         _open.Push(token.Name);
         if(_open.Count > MaximumOpenTags)
         {
            throw new ArgumentException(Message, nameof(token));
         }
      }
      else if(token.Type == HtmlTokenType.EndTag && _open.TryPeek(out string? top) && string.Equals(top, token.Name, StringComparison.Ordinal))
      {
         _open.Pop();
      }
   }



   /// <summary>Measures the parsed tree without recursion and rejects one deeper than <see cref="MaximumDepth"/>.</summary>
   public static void ThrowIfTooDeep(INode root)
   {
      int depth = 0;
      INode? node = root;
      while(node is not null)
      {
         if(node.FirstChild is { } child)
         {
            node = child;
            if(++depth > MaximumDepth)
            {
               throw new ArgumentException(Message, nameof(root));
            }
         }
         else
         {
            // A leaf: climb to the nearest ancestor that has a sibling still to visit.
            while(node != root && node!.NextSibling is null)
            {
               node = node.Parent;
               depth--;
            }
            node = node == root ? null : node!.NextSibling;
         }
      }
   }
}
