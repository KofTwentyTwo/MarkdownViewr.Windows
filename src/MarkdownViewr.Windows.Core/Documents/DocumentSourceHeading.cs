/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Original UTF-16 heading syntax offsets, with an exclusive end, paired with a generated rendered identity.</summary>
public sealed record DocumentSourceHeading(int Index, int Start, int End);
