/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Offsets are UTF16 positions into the exact supplied text, including all whitespace and line breaks.</summary>
public sealed record SourceToken(int Start, int Length, SourceTokenKind Kind);
