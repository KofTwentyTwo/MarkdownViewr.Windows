/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core.Printing;

/// <summary>Immutable print preferences adapted from the reference; cancellation retains the saved draft.</summary>
public sealed record PrintOptions(string Theme = "Clean Printing", bool Backgrounds = false, bool Images = true,
   bool MatchScreenZoom = false, bool FullPage = true, bool Borders = false, double PaddingPixels = 0)
{
   /// <summary>Repairs corrupt preferences without allowing arbitrary CSS values or unbounded padding.</summary>
   public PrintOptions Sanitize() => this with
   {
      Theme = string.IsNullOrWhiteSpace(Theme) || Theme.Length > 80 || Theme.Any(char.IsControl) ? "Clean Printing" : Theme,
      PaddingPixels = double.IsFinite(PaddingPixels) ? Math.Clamp(PaddingPixels, 0, 96) : 0,
   };
}
