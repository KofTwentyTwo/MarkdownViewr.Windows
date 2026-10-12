/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Proves bounded fixed-query geometry and paired heading interpolation without executing arbitrary data.</summary>
public sealed class DocumentScrollStateTests
{
   /// <summary>Finite generated geometry is preserved and viewport overshoot is bounded.</summary>
   [Fact]
   public void FromBrowserResult_ValidFrame_PreservesGeneratedGeometry()
   {
      DocumentScrollState state = DocumentScrollState.FromBrowserResult("[120,100,[[0,12],[2,90]],[1,2]]", 3)!;
      Assert.Equal(100, state.Position);
      Assert.Equal(100, state.Maximum);
      Assert.Equal(12, state.Headings[0]);
      Assert.Equal(90, state.Headings[2]);
      Assert.Equal(new[] { 1, 2 }, state.Collapsed);
      Assert.Equal(0, DocumentScrollState.FromBrowserResult("[-5,100,[],[]]", 0)!.Position);
      Assert.Equal(0, DocumentScrollState.FromBrowserResult("[0,0,[],[]]", 0)!.Maximum);
   }



   /// <summary>Untrusted shapes, identities, duplicate entries and nonfinite geometry cannot become scroll commands.</summary>
   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData("null")]
   [InlineData("{}")]
   [InlineData("[0,100,[]]")]
   [InlineData("[\"0\",100,[],[]]")]
   [InlineData("[0,-1,[],[]]")]
   [InlineData("[0,1e999,[],[]]")]
   [InlineData("[0,100,{},[]]")]
   [InlineData("[0,100,[],{}]")]
   [InlineData("[0,100,[[0,1],[1,2],[2,3]],[]]")]
   [InlineData("[0,100,[],[0,1,1]]")]
   [InlineData("[0,100,[0],[]]")]
   [InlineData("[0,100,[[0]],[]]")]
   [InlineData("[0,100,[[\"0\",1]],[]]")]
   [InlineData("[0,100,[[-1,1]],[]]")]
   [InlineData("[0,100,[[2,1]],[]]")]
   [InlineData("[0,100,[[0.5,1]],[]]")]
   [InlineData("[0,100,[[0,\"one\"]],[]]")]
   [InlineData("[0,100,[[0,1e999]],[]]")]
   [InlineData("[0,100,[[0,1],[0,2]],[]]")]
   [InlineData("[0,100,[],[0,0]]")]
   [InlineData("[0,100,[],[\"0\"]]")]
   [InlineData("[0,100,[],[2]]")]
   public void FromBrowserResult_InvalidFrame_ReturnsNoGeometry(string? json)
   {
      Assert.Null(DocumentScrollState.FromBrowserResult(json, 2));
   }



   /// <summary>Query payload size scales only with the current generated heading count.</summary>
   [Fact]
   public void FromBrowserResult_OversizedOrInvalidCount_RejectsFrame()
   {
      Assert.Null(DocumentScrollState.FromBrowserResult(new string(' ', 513), 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => DocumentScrollState.FromBrowserResult("[]", -1));
   }



   /// <summary>Paired anchors retain viewport fractions between headings and reach exact document ends.</summary>
   [Theory]
   [InlineData(-1, 0)]
   [InlineData(0, 0)]
   [InlineData(12.5, 50)]
   [InlineData(25, 100)]
   [InlineData(50, 120)]
   [InlineData(75, 140)]
   [InlineData(87.5, 170)]
   [InlineData(100, 200)]
   [InlineData(110, 200)]
   public void MapPosition_PairedHeadings_InterpolatesAndClamps(double position, double expected)
   {
      Assert.Equal(expected, DocumentScrollState.MapPosition(position, 100, 200, [(75, 140), (25, 100)]));
   }



   /// <summary>No headings use relative scroll; duplicate/nonfinite/off-range anchors cannot corrupt interpolation.</summary>
   [Fact]
   public void MapPosition_MissingOrUnusableAnchors_UsesBoundedGeometry()
   {
      Assert.Equal(200, DocumentScrollState.MapPosition(50, 100, 400, []));
      Assert.Equal(120, DocumentScrollState.MapPosition(50, 100, 200, [(25, 100), (25, 110), (75, 140), (double.NaN, 1), (1, double.PositiveInfinity), (-1, 1), (100, 1)]));
      Assert.Equal(100, DocumentScrollState.MapPosition(25, 100, 100, [(25, 200)]));
      Assert.Equal(0, DocumentScrollState.MapPosition(25, 100, 100, [(25, -1)]));
      Assert.Equal(0, DocumentScrollState.MapPosition(double.NaN, 100, 100, []));
      Assert.Equal(0, DocumentScrollState.MapPosition(0, double.PositiveInfinity, 100, []));
      Assert.Equal(0, DocumentScrollState.MapPosition(0, 100, double.NaN, []));
      Assert.Equal(0, DocumentScrollState.MapPosition(0, 0, 100, []));
      Assert.Equal(0, DocumentScrollState.MapPosition(0, 100, -1, []));
      Assert.Throws<ArgumentNullException>(() => DocumentScrollState.MapPosition(0, 1, 1, null!));
   }
}
