/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text;


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Mirrors section folds in a literal native display while preserving the original snapshot and source identities.</summary>
public sealed class DocumentSourceText
{
   private const string Ellipsis = "⋯\r\n";
   private readonly string _source;
   private readonly string _native;
   private readonly int[] _singleBreaks;
   private readonly int[] _lineStarts;
   private readonly DocumentSourceHeading[] _originalHeadings;
   private readonly (int Index, int Start, int End)[] _sections;
   private HashSet<int>? _collapsed;
   private (int Start, int End)[] _ranges = [];
   private int[] _rangeStarts = [];
   private int[] _removed = [0];



   /// <summary>Indexes original/native line coordinates once and derives section boundaries from parser identities and heading levels.</summary>
   public DocumentSourceText(string source, DocumentOutline outline)
   {
      ArgumentNullException.ThrowIfNull(source);
      ArgumentNullException.ThrowIfNull(outline);
      if(source.Length > MarkdownRenderer.MaximumCharacters)
      {
         throw new ArgumentException("The source exceeds the document size limit.", nameof(source));
      }
      if(outline.SourceHeadings.Any(heading => heading.Start < 0 || heading.End < heading.Start || heading.End > source.Length))
      {
         throw new ArgumentException("Source positions must belong to the committed snapshot.", nameof(outline));
      }
      _source = source;
      _native = source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Replace("\n", "\r\n", StringComparison.Ordinal);
      var singleBreaks = new List<int>();
      var lineStarts = new List<int> { 0 };
      for(int index = 0; index < source.Length; index++)
      {
         if(source[index] is '\r' or '\n')
         {
            if(source[index] == '\r' && index + 1 < source.Length && source[index + 1] == '\n')
            {
               index++;
            }
            else
            {
               singleBreaks.Add(index);
            }
            lineStarts.Add(index + 1);
         }
      }
      _singleBreaks = singleBreaks.ToArray();
      _lineStarts = lineStarts.ToArray();
      _originalHeadings = outline.SourceHeadings.OrderBy(heading => heading.Start).ToArray();
      _sections = new (int, int, int)[_originalHeadings.Length];
      var stack = new Stack<int>();
      for(int index = 0; index < _originalHeadings.Length; index++)
      {
         DocumentSourceHeading heading = _originalHeadings[index];
         int level = outline.Headings[heading.Index].Level;
         while(stack.TryPeek(out int previous) && outline.Headings[_originalHeadings[previous].Index].Level >= level)
         {
            stack.Pop();
            (int Index, int Start, int End) section = _sections[previous];
            _sections[previous] = (section.Index, section.Start, LineStart(heading.Start));
         }
         _sections[index] = (heading.Index, AfterLineBreak(heading.End), source.Length);
         stack.Push(index);
      }
      SetCollapsed([]);
   }



   /// <summary>Visible literal text with Windows newlines and a plain ellipsis for omitted bodies, never HTML or rich text.</summary>
   public string Text { get; private set; } = "";



   /// <summary>Only visible headings, with offsets into the actual displayed native string.</summary>
   public IReadOnlyList<DocumentSourceHeading> Headings { get; private set; } = [];



   /// <summary>Nested folds remain independent; overlapping parent/child bodies merge without losing saved choices.</summary>
   public bool SetCollapsed(IEnumerable<int> collapsed)
   {
      ArgumentNullException.ThrowIfNull(collapsed);
      var choices = collapsed.ToHashSet();
      if(_collapsed is not null && _collapsed.SetEquals(choices))
      {
         return false;
      }
      _collapsed = choices;
      var ranges = new List<(int Start, int End)>();
      foreach((int index, int start, int end) in _sections.Where(section => choices.Contains(section.Index) && section.Start < section.End).OrderBy(section => section.Start))
      {
         if(ranges.Count > 0 && start <= ranges[^1].End)
         {
            ranges[^1] = (ranges[^1].Start, Math.Max(end, ranges[^1].End));
         }
         else
         {
            ranges.Add((start, end));
         }
      }
      _ranges = ranges.Select(range => (ToNative(range.Start), ToNative(range.End))).ToArray();
      _rangeStarts = _ranges.Select(range => range.Start).ToArray();
      _removed = new int[_ranges.Length + 1];
      var text = new StringBuilder();
      int cursor = 0;
      for(int index = 0; index < _ranges.Length; index++)
      {
         (int start, int end) = _ranges[index];
         text.Append(_native, cursor, start - cursor).Append(Ellipsis);
         _removed[index + 1] = _removed[index] + end - start - Ellipsis.Length;
         cursor = end;
      }
      text.Append(_native, cursor, _native.Length - cursor);
      Text = text.ToString();
      var headings = new List<DocumentSourceHeading>();
      foreach(DocumentSourceHeading heading in _originalHeadings)
      {
         if(GetDisplayRange(heading.Start, heading.End - heading.Start) is { } range)
         {
            headings.Add(new DocumentSourceHeading(heading.Index, range.Start, range.Start + range.Length));
         }
      }
      Headings = headings.AsReadOnly();
      return true;
   }



   /// <summary>Maps original query spans into the visible native display; hidden or partly folded occurrences have no selection.</summary>
   public (int Start, int Length)? GetDisplayRange(int start, int length)
   {
      ArgumentOutOfRangeException.ThrowIfNegative(start);
      ArgumentOutOfRangeException.ThrowIfNegative(length);
      if(start > _source.Length || length > _source.Length - start)
      {
         throw new ArgumentOutOfRangeException(nameof(length));
      }
      int nativeStart = ToNative(start);
      int nativeEnd = ToNative(start + length);
      int index = Array.BinarySearch(_rangeStarts, nativeStart);
      index = index >= 0 ? index : ~index - 1;
      if(index >= 0 && nativeStart < _ranges[index].End || index + 1 < _ranges.Length && nativeEnd > _ranges[index + 1].Start)
      {
         return null;
      }
      return (nativeStart - _removed[index + 1], nativeEnd - nativeStart);
   }



   /// <summary>Each original lone CR/LF expands by one native character; existing CRLF and Unicode text keep their lengths.</summary>
   private int ToNative(int position)
   {
      int index = Array.BinarySearch(_singleBreaks, position);
      return position + (index >= 0 ? index : ~index);
   }



   /// <summary>Preserves the complete next heading line, including quote/list prefixes.</summary>
   private int LineStart(int position)
   {
      int index = Array.BinarySearch(_lineStarts, position);
      return _lineStarts[index >= 0 ? index : ~index - 1];
   }



   /// <summary>A fold starts after the full ATX/Setext heading line and its original line break.</summary>
   private int AfterLineBreak(int position)
   {
      if(position < _source.Length && _source[position] == '\r')
      {
         position++;
      }
      if(position < _source.Length && _source[position] == '\n')
      {
         position++;
      }
      return position;
   }
}
