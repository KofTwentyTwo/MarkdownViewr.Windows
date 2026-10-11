/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using System.Text;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.Core.Printing;

/// <summary>Generates print-only CSS; the native host keeps reading styles in a separate screen stylesheet.</summary>
public static class PrintStyles
{
   private static readonly ReaderTheme s_clean = ReaderTheme.Parse("""
      {"name":"Clean Printing","schemaVersion":1,
       "colors":{"background":"#ffffff","text":"#24292e","heading1":"#1b1f23","heading2":"#1b1f23",
         "heading3":"#1b1f23","heading4":"#1b1f23","heading5":"#1b1f23","heading6":"#1b1f23",
         "link":"#0366d6","codeBackground":"#f6f8fa","codeText":"#24292e","blockquoteBorder":"#dfe2e5",
         "blockquoteBackground":"#fafbfc","highlightBackground":"#fff5b1","highlightText":"#24292e"},
       "fonts":{"body":"System","heading":"System","code":"SF Mono"},
       "sizes":{"baseFontSize":14,"h1Size":28,"h2Size":23,"h3Size":20,"h4Size":18,"h5Size":16,"h6Size":15,
         "codeFontSize":13,"lineHeight":1.7},"customCSS":""}
      """);



   /// <summary>Resolves the reference's special choices and current catalog without mutating any theme or document state.</summary>
   public static string Build(PrintOptions options, ReaderTheme activeTheme, IEnumerable<ReaderTheme> themes, double readingZoom)
   {
      ArgumentNullException.ThrowIfNull(options);
      ArgumentNullException.ThrowIfNull(activeTheme);
      ArgumentNullException.ThrowIfNull(themes);
      if(!double.IsFinite(readingZoom) || readingZoom is < 0.3 or > 5)
      {
         throw new ArgumentOutOfRangeException(nameof(readingZoom));
      }
      options = options.Sanitize();
      ReaderTheme? theme = options.Theme switch
      {
         "Active Theme" => activeTheme,
         "Plain HTML" => null,
         "Clean Printing" => s_clean,
         _ => themes.FirstOrDefault(candidate => string.Equals(candidate.Name, options.Theme, StringComparison.Ordinal)) ?? s_clean,
      };
      var css = new StringBuilder(theme?.GetStyles(options.MatchScreenZoom ? readingZoom : 1) ?? "");
      css.Append("""
         html,body{height:auto!important;overflow:visible!important;display:block!important;}
         [data-mv-body][hidden]{display:block!important;}
         *{-webkit-print-color-adjust:exact!important;print-color-adjust:exact!important;}
         h1,h2,h3,h4,h5,h6{break-after:avoid!important;}
         pre,blockquote,tr,li,img{break-inside:avoid!important;}
         pre{white-space:pre-wrap!important;overflow:visible!important;}
         img{max-width:100%!important;}thead{display:table-header-group!important;}
         th,td{overflow-wrap:anywhere!important;white-space:normal!important;}
         """);
      css.Append("body{padding:").Append(options.PaddingPixels.ToString("R", CultureInfo.InvariantCulture)).Append("px!important;}");
      if(options.FullPage)
      {
         css.Append("body{max-width:none!important;margin:0!important;}");
      }
      if(!options.Backgrounds)
      {
         css.Append("""
            html,body{background:#ffffff!important;color:#000000!important;}
            *{background:transparent!important;background-image:none!important;color:#000000!important;
              -webkit-print-color-adjust:economy!important;print-color-adjust:economy!important;}
            a{color:#0969da!important;text-decoration:underline!important;}
            hr,h1,h2{border-bottom:1px solid #dddddd!important;}
            th,td{border:1px solid #dddddd!important;}blockquote{border-left:3px solid #cccccc!important;}
            """);
      }
      if(options.Borders)
      {
         css.Append("pre,blockquote,code,details{border:1px solid currentColor!important;}");
      }
      if(!options.Images)
      {
         css.Append("img{display:none!important;}");
      }
      return css.ToString();
   }
}
