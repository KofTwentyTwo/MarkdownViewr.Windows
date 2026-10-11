/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Settings;


namespace MarkdownViewr.Windows.Core;

/// <summary>Persistent AppKit shell and Markdown reading preferences.</summary>
public sealed class AppSettings : ShellSettings
{
   /// <summary>The selected document theme; independent of the Windows shell theme.</summary>
   public string DocumentTheme { get; set; } = "Catppuccin Mocha";
}
