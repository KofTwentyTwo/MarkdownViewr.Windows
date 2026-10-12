/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>A passive heading label and host-generated numeric identity for native navigation.</summary>
public sealed record DocumentHeading(int Index, int Level, string Title);
