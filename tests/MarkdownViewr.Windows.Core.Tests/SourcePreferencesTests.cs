/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Invalid saved widths cannot hide the reading surface or prevent startup.</summary>
public sealed class SourcePreferencesTests
{
   /// <summary>Both visibility choices retain bounded widths; nonfinite values restore the default.</summary>
   [Theory]
   [InlineData(true, double.NaN, 320)]
   [InlineData(false, double.PositiveInfinity, 320)]
   [InlineData(true, -100, 160)]
   [InlineData(false, 900, 600)]
   [InlineData(true, 280, 280)]
   public void Sanitize_SourcePreferences_RetainsVisibilityAndClampsWidth(bool visible, double width, double expected)
   {
      var settings = new AppSettings { ShowSource = visible, SourceWidth = width };
      settings.Sanitize();
      Assert.Equal(visible, settings.ShowSource);
      Assert.Equal(expected, settings.SourceWidth);
   }
}
