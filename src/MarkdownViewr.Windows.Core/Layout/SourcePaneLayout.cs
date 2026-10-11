/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core.Layout;

/// <summary>Allocates the space shared only by the document and source, preserving usable columns without a fixed maximum source width.</summary>
public static class SourcePaneLayout
{
   /// <summary>A narrow window keeps the two panes equal; larger windows reserve160 source pixels and200 document pixels.</summary>
   public static double Fraction(double available, double preferred = 0.5)
   {
      if(!double.IsFinite(available) || available <= 0)
      {
         return 0.5;
      }
      if(!double.IsFinite(preferred))
      {
         preferred = 0.5;
      }
      double minimum = Math.Min(160, available / 2) / available;
      double maximum = 1 - Math.Min(200, available / 2) / available;
      return Math.Clamp(preferred, minimum, maximum);
   }



   /// <summary>Dragging right reduces the source; invalid input cannot hide either reading surface.</summary>
   public static double Resize(double available, double sourceWidth, double horizontalChange)
   {
      if(!double.IsFinite(available) || available <= 0 || !double.IsFinite(sourceWidth) || !double.IsFinite(horizontalChange))
      {
         return 0.5;
      }
      return Fraction(available, (sourceWidth - horizontalChange) / available);
   }
}
