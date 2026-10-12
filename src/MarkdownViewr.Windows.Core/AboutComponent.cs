/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core;

/// <summary>A verified dependency credit with fixed official project and license destinations, never document-supplied links.</summary>
public sealed record AboutComponent(string Name, string Version, string License, string ProjectUrl, string LicenseUrl);
