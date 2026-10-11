/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Net;
using System.Text;


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Adapts Darin's delimiter and line-based metadata display without interpreting YAML as commands.</summary>
internal static class DocumentFrontmatter
{
   /// <summary>An absent closing delimiter leaves the original document untouched.</summary>
   public static (string? Metadata, string Body) Split(string markdown)
   {
      string[] lines = markdown.Split('\n');
      if(lines.Length < 2 || !string.Equals(lines[0].Trim(), "---", StringComparison.Ordinal))
      {
         return (null, markdown);
      }
      int end = Array.FindIndex(lines, 1, line => string.Equals(line.Trim(), "---", StringComparison.Ordinal));
      if(end < 0)
      {
         return (null, markdown);
      }
      return (string.Join('\n', lines[1..end]), string.Join('\n', lines[(end + 1)..]));
   }



   /// <summary>Metadata is display text: split only at the first colon and escape every value before creating table markup.</summary>
   public static string RenderMetadata(string metadata)
   {
      var html = new StringBuilder("<table>");
      foreach(string line in metadata.Split('\n'))
      {
         string text = line.Trim();
         if(text.Length == 0)
         {
            continue;
         }
         int colon = text.IndexOf(':');
         html.Append("<tr>");
         if(colon >= 0)
         {
            html.Append("<td>").Append(WebUtility.HtmlEncode(PlainValue(text[..colon]))).Append("</td>");
            html.Append("<td>").Append(WebUtility.HtmlEncode(PlainValue(text[(colon + 1)..]))).Append("</td>");
         }
         else
         {
            html.Append("<td colspan=\"2\">").Append(WebUtility.HtmlEncode(PlainValue(text))).Append("</td>");
         }
         html.Append("</tr>");
      }
      return html.Append("</table>").ToString();
   }



   /// <summary>Matches the reference's plain metadata labels while preserving mid-word punctuation.</summary>
   private static string PlainValue(string text)
   {
      foreach(string marker in new[] { "***", "**", "__", "~~" })
      {
         text = text.Replace(marker, "", StringComparison.Ordinal);
      }
      text = text.Trim();
      foreach(char marker in new[] { '*', '_', '`' })
      {
         if(text.StartsWith(marker) && text.EndsWith(marker))
         {
            text = text.Length > 1 ? text[1..^1] : "";
         }
      }
      return text.Trim();
   }
}
