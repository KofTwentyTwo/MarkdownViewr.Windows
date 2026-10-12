/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core.Themes;

/// <summary>Reader-wide layout and CSS remain independent of saved theme data and print preferences.</summary>
/// <param name="LimitWidth">Whether the reader applies a finite content width, enabled by default in the reference.</param>
/// <param name="WidthPixels">Maximum content width before reading zoom is applied.</param>
/// <param name="GlobalCss">Native user CSS applied before per-theme CSS.</param>
public sealed record ReadingAppearance(bool LimitWidth = true, double WidthPixels = 1000, string GlobalCss = "")
{
   /// <summary>Bounds native CSS input and persisted layout without allocating unbounded style output.</summary>
   public const int MaximumCssCharacters = 128 * 1024;



   /// <summary>Repairs corrupt saved values while retaining the user's valid preferences.</summary>
   public ReadingAppearance Sanitize() => this with
   {
      WidthPixels = double.IsFinite(WidthPixels) && WidthPixels > 0 ? Math.Clamp(WidthPixels, 200, 4000) : 1000,
      GlobalCss = GlobalCss is null || GlobalCss.Length > MaximumCssCharacters ? "" : GlobalCss,
   };



   /// <summary>Invalid native drafts stay open; repairing stored values must not silently accept an invalid edit.</summary>
   public void Validate()
   {
      if(!double.IsFinite(WidthPixels) || WidthPixels is < 200 or > 4000)
      {
         throw new InvalidOperationException("Enter a content width between 200 and 4000 pixels.");
      }
      if(GlobalCss is null)
      {
         throw new InvalidOperationException("Global CSS cannot be null.");
      }
      if(GlobalCss.Length > MaximumCssCharacters)
      {
         throw new InvalidOperationException("Global CSS exceeds the 128 Ki-character limit.");
      }
   }
}
