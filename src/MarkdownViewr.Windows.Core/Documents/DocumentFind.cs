/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Pairs literal source occurrences with rendered matches without counting the two views twice.</summary>
public sealed class DocumentFind
{
   /// <summary>Bounds a native search term independently of document size.</summary>
   public const int MaximumQueryCharacters = 256;

   private int _renderedCount;



   /// <summary>Finds nonoverlapping case-insensitive literal text; user input is never a regular expression.</summary>
   public DocumentFind(string source, string query)
   {
      ArgumentNullException.ThrowIfNull(source);
      ArgumentNullException.ThrowIfNull(query);
      if(source.Length > MarkdownRenderer.MaximumCharacters || query.Length > MaximumQueryCharacters)
      {
         throw new ArgumentException("The document or find term exceeds its supported size.", source.Length > MarkdownRenderer.MaximumCharacters ? nameof(source) : nameof(query));
      }
      Query = query;
      var matches = new List<int>();
      if(query.Length > 0)
      {
         int offset = 0;
         while(offset <= source.Length - query.Length)
         {
            int position = source.IndexOf(query, offset, StringComparison.OrdinalIgnoreCase);
            if(position < 0)
            {
               break;
            }
            matches.Add(position);
            offset = position + query.Length;
         }
      }
      SourceMatches = matches.AsReadOnly();
      Current = matches.Count > 0 ? 1 : 0;
   }



   /// <summary>The literal query shared by both surfaces.</summary>
   public string Query { get; }



   /// <summary>UTF-16 offsets into the literal source, in document order.</summary>
   public IReadOnlyList<int> SourceMatches { get; }



   /// <summary>Uses the reference's paired occurrence count, including text visible in only one surface.</summary>
   public int Total => Math.Max(SourceMatches.Count, _renderedCount);



   /// <summary>One-based current occurrence; zero means no matches.</summary>
   public int Current { get; private set; }



   /// <summary>The current source range, or no range when the rendered document has more occurrences.</summary>
   public (int Start, int Length)? SourceSelection => Current > 0 && Current <= SourceMatches.Count ? (SourceMatches[Current - 1], Query.Length) : null;



   /// <summary>Browser count changes retain a valid occurrence and cannot create a negative total.</summary>
   public void SetRenderedCount(int count)
   {
      _renderedCount = Query.Length > 0 ? Math.Max(0, count) : 0;
      Current = Total > 0 ? Math.Clamp(Current, 1, Total) : 0;
   }



   /// <summary>Advances one paired occurrence and wraps at the end.</summary>
   public void Next() => Current = Total > 0 ? (Current >= Total ? 1 : Current + 1) : 0;



   /// <summary>Moves back one paired occurrence and wraps at the beginning.</summary>
   public void Previous() => Current = Total > 0 ? (Current <= 1 ? Total : Current - 1) : 0;
}
