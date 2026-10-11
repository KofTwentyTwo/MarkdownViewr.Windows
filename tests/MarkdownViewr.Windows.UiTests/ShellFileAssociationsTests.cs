/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.Win32;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Actual Windows association reads are bounded and never replace the current user's selections.</summary>
public sealed class ShellFileAssociationsTests
{
   /// <summary>The standard text-file association exercises the real API; Markdown reads leave the existing choice untouched.</summary>
   [Fact]
   public void QueryProgId_CurrentWindowsAssociations_ReadsWithoutChangingChoice()
   {
      using RegistryKey? choice = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\.md\\UserChoice");
      object? before = choice?.GetValue("ProgId");
      string? text = ShellFileAssociations.QueryProgId(".txt");
      Assert.False(string.IsNullOrWhiteSpace(text));
      Assert.InRange(text!.Length, 1, 1023);
      Assert.DoesNotContain(text, char.IsControl);
      _ = ShellFileAssociations.QueryProgId(".md");
      Assert.Equal(before, choice?.GetValue("ProgId"));
   }
}
