/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.InteropServices;
using System.Text.Json;


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>A validated relative viewport that keeps the reading position when document height changes.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct DocumentViewport
{
   /// <summary>Constructs a validated pair; arbitrary browser strings never become executable script.</summary>
   private DocumentViewport(double x, double y)
   {
      HorizontalFraction = Math.Clamp(x, 0, 1);
      VerticalFraction = Math.Clamp(y, 0, 1);
   }



   /// <summary>The horizontal position as a fraction of the available scroll range.</summary>
   public double HorizontalFraction { get; }



   /// <summary>The vertical position as a fraction of the available scroll range.</summary>
   public double VerticalFraction { get; }



   /// <summary>Reads the exact two-number result of the application's fixed viewport query.</summary>
   public static DocumentViewport FromBrowserResult(string? json)
   {
      if(json is null || json.Length > 128)
      {
         return default;
      }
      try
      {
         using var document = JsonDocument.Parse(json);
         JsonElement root = document.RootElement;
         if(root.ValueKind == JsonValueKind.Array && root.GetArrayLength() == 2 &&
            root[0].ValueKind == JsonValueKind.Number && root[1].ValueKind == JsonValueKind.Number &&
            root[0].TryGetDouble(out double x) && root[1].TryGetDouble(out double y) &&
            double.IsFinite(x) && double.IsFinite(y))
         {
            return new DocumentViewport(x, y);
         }
      }
      catch(JsonException)
      {
         // An unavailable or malformed browser result falls back to the top of the document.
      }
      return default;
   }
}
