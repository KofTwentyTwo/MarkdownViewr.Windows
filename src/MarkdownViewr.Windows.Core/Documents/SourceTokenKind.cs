/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Semantic source colors remain independent of HTML/CSS and native capabilities.</summary>
public enum SourceTokenKind
{
   Heading,
   Code,
   Emphasis,
   Strong,
   Strikethrough,
   Link,
   Image,
   List,
   Quote,
   Frontmatter,
   Html,
}
