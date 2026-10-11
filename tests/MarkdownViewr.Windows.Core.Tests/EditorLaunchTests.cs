/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Editors;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Proves chosen executable boundaries and literal document arguments without launching an external application.</summary>
public sealed class EditorLaunchTests
{
   /// <summary>Whitespace, command punctuation and Unicode remain one process argument rather than shell syntax.</summary>
   [Theory]
   [InlineData(@"C:\Documents\my notes.md")]
   [InlineData(@"C:\Documents\日本語 café.md")]
   [InlineData(@"C:\Documents\x & calc; $HOME `whoami`.md")]
   [InlineData(@"C:\Documents\--reuse-window.md")]
   public void Create_LiteralDocument_PreservesSingleArgument(string document)
   {
      System.Diagnostics.ProcessStartInfo start = EditorLaunch.Create(@"C:\Program Files\Editor\Editor.EXE", document);
      Assert.Equal(@"C:\Program Files\Editor\Editor.EXE", start.FileName);
      Assert.False(start.UseShellExecute);
      Assert.Equal(@"C:\Documents", start.WorkingDirectory);
      Assert.Equal(document, Assert.Single(start.ArgumentList));
      Assert.Empty(start.Arguments);
   }



   /// <summary>Relative commands, scripts, malformed saved settings and oversized paths cannot select the editor.</summary>
   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData("editor.exe")]
   [InlineData(@"C:editor.exe")]
   [InlineData(@"C:\editor.cmd")]
   [InlineData("C:\\editor\n.exe")]
   public void Create_InvalidExecutable_RejectsChoice(string? executable)
   {
      Assert.False(EditorLaunch.IsExecutablePath(executable));
      Assert.Throws<ArgumentException>(() => EditorLaunch.Create(executable!, @"C:\Documents\test.md"));
      var settings = new AppSettings { ExternalEditor = executable! };
      settings.Sanitize();
      Assert.Empty(settings.ExternalEditor);
   }



   /// <summary>Saved paths are bounded independently of filesystem existence.</summary>
   [Fact]
   public void Sanitize_ExecutableLength_RepairsOversizedChoiceAndRetainsValidChoice()
   {
      string oversized = "C:\\" + new string('a', 32767) + ".exe";
      Assert.False(EditorLaunch.IsExecutablePath(oversized));
      Assert.Throws<ArgumentException>(() => EditorLaunch.Create(oversized, @"C:\Documents\test.md"));
      var settings = new AppSettings { ExternalEditor = oversized };
      settings.Sanitize();
      Assert.Empty(settings.ExternalEditor);
      settings.ExternalEditor = @"C:\Editor\editor.exe";
      settings.Sanitize();
      Assert.Equal(@"C:\Editor\editor.exe", settings.ExternalEditor);
   }



   /// <summary>An incomplete or corrupt document path cannot be passed to an editor.</summary>
   [Theory]
   [InlineData(null)]
   [InlineData("")]
   [InlineData("relative.md")]
   [InlineData("C:\\Documents\\bad\nname.md")]
   public void Create_InvalidDocument_RejectsArgument(string? document)
      => Assert.Throws<ArgumentException>(() => EditorLaunch.Create(@"C:\Editor\editor.exe", document!));



   /// <summary>Oversized document paths are rejected before process construction.</summary>
   [Fact]
   public void Create_OversizedDocument_RejectsArgument()
      => Assert.Throws<ArgumentException>(() => EditorLaunch.Create(@"C:\Editor\editor.exe", "C:\\" + new string('x', 32767)));
}
