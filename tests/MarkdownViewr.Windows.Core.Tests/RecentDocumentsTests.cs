/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>History is private, bounded and tolerant of corrupt settings without probing the filesystem.</summary>
public sealed class RecentDocumentsTests
{
   /// <summary>Invalid persisted entries cannot become relative file activations or unbounded menu text.</summary>
   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData("  ")]
   [InlineData("notes.md")]
   [InlineData("https://example.com/notes.md")]
   [InlineData("C:\\notes\u0000.md")]
   [InlineData("C:\\notes\n.md")]
   public void Sanitize_InvalidPath_RemovesEntry(string? path)
      => Assert.Empty(RecentDocuments.Sanitize([path]));



   /// <summary>The history retains Windows path order, drops duplicate casing and imposes the fixed limit.</summary>
   [Fact]
   public void Sanitize_MixedHistory_PreservesUniqueBoundedPaths()
   {
      string first = Path.Combine(Path.GetTempPath(), "notes & café.md");
      string[] paths = [first, first.ToUpperInvariant(), .. Enumerable.Range(0, 25).Select(index => Path.Combine(Path.GetTempPath(), index + ".md"))];
      string[] history = RecentDocuments.Sanitize(paths);
      Assert.Equal(20, history.Length);
      Assert.Equal(first, history[0]);
      Assert.EndsWith("18.md", history[^1], StringComparison.Ordinal);
      Assert.Empty(RecentDocuments.Sanitize([Path.GetTempPath() + new string('x', 32768)]));
   }



   /// <summary>Recording a successful open promotes a duplicate and accepts a fresh history without touching files.</summary>
   [Fact]
   public void Record_SuccessfulPath_PromotesOnlyThatEntry()
   {
      string first = Path.Combine(Path.GetTempPath(), "one.md");
      string second = Path.Combine(Path.GetTempPath(), "two.md");
      Assert.Equal([second, first], RecentDocuments.Record([first, second], second));
      Assert.Equal([first], RecentDocuments.Record(null, first));
      Assert.Empty(RecentDocuments.Sanitize(null));
   }



   /// <summary>Old or malformed settings load an empty repaired history and use the same bounds as the menu.</summary>
   [Fact]
   public void Sanitize_Settings_RepairsHistory()
   {
      var settings = new AppSettings { RecentFiles = null! };
      settings.Sanitize();
      Assert.Empty(settings.RecentFiles);
      settings.RecentFiles = ["relative.md", Path.Combine(Path.GetTempPath(), "existing.md")];
      settings.Sanitize();
      Assert.Single(settings.RecentFiles);
   }
}
