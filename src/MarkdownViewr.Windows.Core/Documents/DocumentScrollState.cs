/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Validated numeric geometry from a fixed host query, used for bidirectional heading-anchored synchronization.</summary>
public sealed class DocumentScrollState
{
   /// <summary>Stores only current generated identities, finite geometry and bounded collapse choices.</summary>
   private DocumentScrollState(double position, double maximum, Dictionary<int, double> headings, List<int> collapsed)
   {
      Position = Math.Clamp(position, 0, maximum);
      Maximum = maximum;
      Headings = headings;
      Collapsed = collapsed.AsReadOnly();
   }



   /// <summary>The actual vertical viewport offset, bounded by the available scroll range.</summary>
   public double Position { get; }



   /// <summary>The available vertical scroll range, independent of content height.</summary>
   public double Maximum { get; }



   /// <summary>Visible generated heading identities and document-coordinate tops.</summary>
   public IReadOnlyDictionary<int, double> Headings { get; }



   /// <summary>Actual hidden generated section bodies, including independent nested choices.</summary>
   public IReadOnlyList<int> Collapsed { get; }



   /// <summary>Rejects malformed, oversized, duplicate or out-of-range geometry rather than allowing arbitrary browser data into commands.</summary>
   public static DocumentScrollState? FromBrowserResult(string? json, int headingCount)
   {
      ArgumentOutOfRangeException.ThrowIfNegative(headingCount);
      if(json is null || json.Length > headingCount * 128L + 512)
      {
         return null;
      }
      try
      {
         using var document = JsonDocument.Parse(json);
         JsonElement root = document.RootElement;
         if(root.ValueKind != JsonValueKind.Array || root.GetArrayLength() != 4 || !Number(root[0], out double position) || !Number(root[1], out double maximum) || maximum < 0 ||
            root[2].ValueKind != JsonValueKind.Array || root[2].GetArrayLength() > headingCount || root[3].ValueKind != JsonValueKind.Array || root[3].GetArrayLength() > headingCount)
         {
            return null;
         }
         var headings = new Dictionary<int, double>();
         foreach(JsonElement entry in root[2].EnumerateArray())
         {
            if(entry.ValueKind != JsonValueKind.Array || entry.GetArrayLength() != 2 || !Identity(entry[0], headingCount, out int index) || !Number(entry[1], out double top) || !headings.TryAdd(index, top))
            {
               return null;
            }
         }
         var collapsed = new List<int>();
         var seen = new HashSet<int>();
         foreach(JsonElement entry in root[3].EnumerateArray())
         {
            if(!Identity(entry, headingCount, out int index) || !seen.Add(index))
            {
               return null;
            }
            collapsed.Add(index);
         }
         return new DocumentScrollState(position, maximum, headings, collapsed);
      }
      catch(JsonException)
      {
         return null;
      }
   }



   /// <summary>Interpolates paired heading coordinates with explicit top/bottom anchors; no-heading documents use relative scrolling.</summary>
   public static double MapPosition(double position, double fromMaximum, double toMaximum, IEnumerable<(double From, double To)> anchors)
   {
      ArgumentNullException.ThrowIfNull(anchors);
      if(!double.IsFinite(position) || !double.IsFinite(fromMaximum) || !double.IsFinite(toMaximum) || fromMaximum <= 0 || toMaximum <= 0)
      {
         return 0;
      }
      double current = Math.Clamp(position, 0, fromMaximum);
      double previousFrom = 0;
      double previousTo = 0;
      foreach((double from, double to) in anchors.Where(pair => double.IsFinite(pair.From) && double.IsFinite(pair.To) && pair.From > 0 && pair.From < fromMaximum).OrderBy(pair => pair.From))
      {
         if(from <= previousFrom)
         {
            continue;
         }
         double target = Math.Clamp(to, 0, toMaximum);
         if(current <= from)
         {
            return Math.Clamp(previousTo + (current - previousFrom) / (from - previousFrom) * (target - previousTo), 0, toMaximum);
         }
         previousFrom = from;
         previousTo = target;
      }
      return Math.Clamp(previousTo + (current - previousFrom) / (fromMaximum - previousFrom) * (toMaximum - previousTo), 0, toMaximum);
   }



   /// <summary>Only finite JSON numbers participate in pixel interpolation.</summary>
   private static bool Number(JsonElement element, out double value)
   {
      value = 0;
      return element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out value) && double.IsFinite(value);
   }



   /// <summary>Only the exact current generated heading range can be addressed.</summary>
   private static bool Identity(JsonElement element, int count, out int value)
   {
      value = -1;
      return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value) && value >= 0 && value < count;
   }
}
