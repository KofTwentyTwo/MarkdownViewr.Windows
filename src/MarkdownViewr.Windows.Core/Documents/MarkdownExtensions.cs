/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Markdig.Extensions.EmphasisExtras;


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Independent reference Markdown extensions, enabled by default without altering the literal source.</summary>
/// <param name="Highlight">Renders double-equals text as a passive mark.</param>
/// <param name="Superscript">Renders single-caret text above the baseline.</param>
/// <param name="Subscript">Renders single-tilde text below the baseline, leaving double-tilde strikethrough available.</param>
/// <param name="Underline">Renders double-plus text as inserted/underlined content.</param>
public sealed record MarkdownExtensions(bool Highlight = true, bool Superscript = true, bool Subscript = true, bool Underline = true)
{
   /// <summary>GFM strikethrough remains available regardless of the four independent extension preferences.</summary>
   internal EmphasisExtraOptions EmphasisOptions
   {
      get
      {
         EmphasisExtraOptions options = EmphasisExtraOptions.Strikethrough;
         if(Highlight)
         {
            options |= EmphasisExtraOptions.Marked;
         }
         if(Superscript)
         {
            options |= EmphasisExtraOptions.Superscript;
         }
         if(Subscript)
         {
            options |= EmphasisExtraOptions.Subscript;
         }
         if(Underline)
         {
            options |= EmphasisExtraOptions.Inserted;
         }
         return options;
      }
   }
}
