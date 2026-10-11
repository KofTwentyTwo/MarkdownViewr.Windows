/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using AngleSharp.Html.Dom;
using Ganss.Xss;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Extensions.Tables;


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Renders GitHub Flavored Markdown into passive HTML for the isolated document surface.</summary>
public sealed class MarkdownRenderer
{
   /// <summary>Bounds parser work before an opened document can consume unbounded memory.</summary>
   public const int MaximumCharacters = 16 * 1024 * 1024;

   private static readonly MarkdownPipeline s_pipeline = new MarkdownPipelineBuilder()
      .UsePipeTables(new PipeTableOptions { UseGfmRules = true })
      .UseTaskLists()
      .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
      .UseAutoLinks()
      .Build();

   private readonly HtmlSanitizer _sanitizer;



   /// <summary>Allows document formatting without scripts, CSS, forms or native host capabilities.</summary>
   public MarkdownRenderer()
   {
      _sanitizer = new HtmlSanitizer();
      _sanitizer.AllowedTags.Clear();
      _sanitizer.AllowedTags.UnionWith(["a", "abbr", "b", "blockquote", "br", "caption", "code", "dd", "del", "details", "div", "dl", "dt", "em", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "i", "img", "input", "kbd", "li", "ol", "p", "pre", "s", "small", "span", "strong", "sub", "summary", "sup", "table", "tbody", "td", "th", "thead", "tr", "ul"]);
      _sanitizer.AllowedAttributes.Clear();
      _sanitizer.AllowedAttributes.UnionWith(["href", "src", "alt", "title", "align", "colspan", "rowspan", "width", "height", "type", "checked", "disabled"]);
      _sanitizer.AllowedSchemes.Clear();
      _sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto"]);
      _sanitizer.AllowedCssProperties.Clear();
      _sanitizer.AllowedAtRules.Clear();
      _sanitizer.PostProcessNode += MakeInputsPassive;
   }



   /// <summary>Preserves useful markup while sanitizing every parser result, including authored raw HTML.</summary>
   public string Render(string markdown, FrontmatterMode frontmatter = FrontmatterMode.Hide)
   {
      ArgumentNullException.ThrowIfNull(markdown);
      if(markdown.Length > MaximumCharacters)
      {
         throw new ArgumentException("The document exceeds the 16 Mi-character rendering limit.", nameof(markdown));
      }
      if(!Enum.IsDefined(frontmatter))
      {
         throw new ArgumentOutOfRangeException(nameof(frontmatter));
      }
      (string? metadata, string body) = DocumentFrontmatter.Split(markdown);
      string prefix = !string.IsNullOrEmpty(metadata) ? frontmatter switch
      {
         FrontmatterMode.Metadata => DocumentFrontmatter.RenderMetadata(metadata),
         FrontmatterMode.Markdown => Markdown.ToHtml("---\n" + metadata + "\n---", s_pipeline),
         _ => "",
      } : "";
      return _sanitizer.Sanitize(prefix + Markdown.ToHtml(body, s_pipeline));
   }



   /// <summary>Task markers are display-only checkboxes; authored inputs cannot become active controls.</summary>
   private static void MakeInputsPassive(object? sender, PostProcessNodeEventArgs args)
   {
      if(args.Node is IHtmlInputElement input)
      {
         input.Type = "checkbox";
         input.IsDisabled = true;
      }
   }
}
