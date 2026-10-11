/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Proves browser data cannot inject script or invalid offsets during reload.</summary>
public sealed class DocumentViewportTests
{
   /// <summary>Valid fractions retain their precision; out-of-range positions clamp to the document limits.</summary>
   [Theory]
   [InlineData("[0.125,0.75]", 0.125, 0.75)]
   [InlineData("[-4, -0.5]", 0, 0)]
   [InlineData("[0,2.5e3]", 0, 1)]
   public void FromBrowserResult_NumericOffsets_PreservesValidPosition(string json, double x, double y)
   {
      var viewport = DocumentViewport.FromBrowserResult(json);
      Assert.Equal(x, viewport.HorizontalFraction);
      Assert.Equal(y, viewport.VerticalFraction);
   }



   /// <summary>Malformed, wrong-shaped, nonnumeric or nonfinite results never become JavaScript arguments.</summary>
   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData("null")]
   [InlineData("{}")]
   [InlineData("[]")]
   [InlineData("[1]")]
   [InlineData("[1,2,3]")]
   [InlineData("[\"0); alert(1);//\",2]")]
   [InlineData("[1,\"2\"]")]
   [InlineData("[true,2]")]
   [InlineData("[1,{}]")]
   [InlineData("[1e400,2]")]
   [InlineData("[1,1e400]")]
   [InlineData("undefined")]
   public void FromBrowserResult_InvalidResult_ReturnsTop(string? json)
   {
      Assert.Equal(default, DocumentViewport.FromBrowserResult(json));
   }



   /// <summary>Oversized browser results are refused before JSON parsing.</summary>
   [Fact]
   public void FromBrowserResult_OverLengthLimit_ReturnsTop()
   {
      Assert.Equal(default, DocumentViewport.FromBrowserResult(new string(' ', 129)));
   }
}
