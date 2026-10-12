/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Net;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Preserves the reference rendered-document containers; TOC/source remain native Windows controls.</summary>
public static class DocumentPage
{
   /// <summary>Wraps already-sanitized renderer output; title and theme CSS cannot break their HTML boundaries.</summary>
   public static string Build(string sanitizedHtml, string title, ReaderTheme theme, ReadingAppearance appearance, double zoomScale)
   {
      ArgumentNullException.ThrowIfNull(sanitizedHtml);
      ArgumentNullException.ThrowIfNull(title);
      ArgumentNullException.ThrowIfNull(theme);
      ArgumentNullException.ThrowIfNull(appearance);
      return $"""<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{WebUtility.HtmlEncode(title)}</title><style media="screen">{theme.GetReadingStyles(appearance, zoomScale)}</style></head><body><div id="content" role="main" aria-label="Document reading area" tabindex="0"><div id="content-inner">{sanitizedHtml}</div></div></body></html>""";
   }
}
