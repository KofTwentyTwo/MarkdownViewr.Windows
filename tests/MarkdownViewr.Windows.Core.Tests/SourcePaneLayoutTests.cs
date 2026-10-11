/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Layout;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Checks usable source/document bounds, equal reopening and unconstrained large-window resizing.</summary>
public sealed class SourcePaneLayoutTests
{
   /// <summary>Initial columns remain equal at supported widths, even when there is no measured space yet.</summary>
   [Theory]
   [InlineData(0)]
   [InlineData(-1)]
   [InlineData(double.NaN)]
   [InlineData(double.PositiveInfinity)]
   [InlineData(280)]
   [InlineData(800)]
   [InlineData(2000)]
   public void Fraction_InitialWidth_ReturnsEqualColumns(double available) => Assert.Equal(0.5, SourcePaneLayout.Fraction(available));



   /// <summary>Invalid ratios recover safely; bounded edges protect both panes and allow source to exceed600px.</summary>
   [Theory]
   [InlineData(1000, double.NaN, 0.5)]
   [InlineData(1000, -10, 0.16)]
   [InlineData(1000, 10, 0.8)]
   [InlineData(2000, 0.6, 0.6)]
   [InlineData(200, 0.9, 0.5)]
   public void Fraction_PreferredRatio_ProtectsUsableColumns(double available, double preferred, double expected) => Assert.Equal(expected, SourcePaneLayout.Fraction(available, preferred), precision: 8);



   /// <summary>Left/right movement changes the split in the correct direction, including physical-space clamping.</summary>
   [Theory]
   [InlineData(1000, 500, -70, 0.57)]
   [InlineData(1000, 500, 70, 0.43)]
   [InlineData(1000, 500, -900, 0.8)]
   [InlineData(1000, 500, 900, 0.16)]
   [InlineData(2000, 1000, -200, 0.6)]
   [InlineData(0, 500, 70, 0.5)]
   [InlineData(double.NaN, 500, 70, 0.5)]
   [InlineData(1000, double.NaN, 70, 0.5)]
   [InlineData(1000, 500, double.NaN, 0.5)]
   public void Resize_Drag_RespectsDirectionAndBounds(double available, double sourceWidth, double delta, double expected) => Assert.Equal(expected, SourcePaneLayout.Resize(available, sourceWidth, delta), precision: 8);
}
