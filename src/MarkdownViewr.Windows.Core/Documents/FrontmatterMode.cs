/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>The reference app's three ways to display a leading frontmatter block.</summary>
public enum FrontmatterMode
{
   /// <summary>Keep metadata out of the reading surface.</summary>
   Hide,
   /// <summary>Show escaped, line-based key/value rows.</summary>
   Metadata,
   /// <summary>Render the frontmatter block through the normal Markdown boundary.</summary>
   Markdown,
}
