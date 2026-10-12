/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core.Navigation;

/// <summary>A transient reading key and modifiers; uppercase and layout punctuation come from native translation.</summary>
public readonly record struct ReaderKey(char Character, bool Control = false, bool Shift = false, bool Alt = false, bool Windows = false, bool Repeat = false);
