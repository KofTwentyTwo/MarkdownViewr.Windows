/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core.Themes;

/// <summary>Deliberate native choices match the reference theme import conflict behavior.</summary>
public enum ThemeImportResolution
{
   Overwrite,
   KeepBoth,
   Skip,
}
