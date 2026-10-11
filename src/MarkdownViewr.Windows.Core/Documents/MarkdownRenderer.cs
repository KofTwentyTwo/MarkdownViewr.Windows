/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Ganss.Xss;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Renders GitHub Flavored Markdown into passive HTML for the isolated document surface.</summary>
public sealed class MarkdownRenderer
{
   /// <summary>Bounds parser work before an opened document can consume unbounded memory.</summary>
   public const int MaximumCharacters = 16 * 1024 * 1024;

   private readonly MarkdownPipeline _pipeline;

   private readonly HtmlSanitizer _sanitizer;



   /// <summary>Allows document formatting without scripts, CSS, forms or native host capabilities.</summary>
   public MarkdownRenderer(MarkdownExtensions? extensions = null)
   {
      MarkdownPipelineBuilder pipeline = new MarkdownPipelineBuilder()
         .UsePipeTables(new PipeTableOptions { UseGfmRules = true })
         .UseTaskLists()
         .UseEmphasisExtras((extensions ?? new MarkdownExtensions()).EmphasisOptions)
         .UseAutoLinks()
         .UsePreciseSourceLocation();
      pipeline.DocumentProcessed += PreserveLiteralExtensionBoundaries;
      _pipeline = pipeline.Build();
      _sanitizer = new HtmlSanitizer();
      _sanitizer.AllowedTags.Clear();
      _sanitizer.AllowedTags.UnionWith(["a", "abbr", "b", "blockquote", "br", "caption", "code", "dd", "del", "details", "div", "dl", "dt", "em", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "i", "img", "input", "ins", "kbd", "li", "mark", "ol", "p", "pre", "s", "small", "span", "strong", "sub", "summary", "sup", "table", "tbody", "td", "th", "thead", "tr", "ul"]);
      _sanitizer.AllowedAttributes.Clear();
      _sanitizer.AllowedAttributes.UnionWith(["href", "src", "alt", "title", "align", "colspan", "rowspan", "width", "height", "type", "checked", "disabled", "class"]);
      _sanitizer.AllowedSchemes.Clear();
      _sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto"]);
      _sanitizer.AllowedCssProperties.Clear();
      _sanitizer.AllowedAtRules.Clear();
      _sanitizer.PostProcessNode += MakeInputsPassive;
      _sanitizer.PostProcessNode += FilterStylingClasses;
   }



   /// <summary>Preserves useful markup while sanitizing every parser result, including authored raw HTML.</summary>
   public string Render(string markdown, FrontmatterMode frontmatter = FrontmatterMode.Hide) => Render(markdown, frontmatter, null);



   /// <summary>Admits only the current private provenance attribute, then restores the ordinary authored-markup policy.</summary>
   internal string RenderWithSources(string markdown, FrontmatterMode frontmatter, DocumentSourceMap sources)
   {
      _sanitizer.AllowedAttributes.Add(sources.AttributeName);
      try
      {
         return Render(markdown, frontmatter, sources);
      }
      finally
      {
         _sanitizer.AllowedAttributes.Remove(sources.AttributeName);
      }
   }



   /// <summary>Uses the same bounded parser and passive sanitation for public HTML and internal source-aware outlines.</summary>
   private string Render(string markdown, FrontmatterMode frontmatter, DocumentSourceMap? sources)
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
         FrontmatterMode.Markdown => sources is null ? Markdown.ToHtml("---\n" + metadata + "\n---", _pipeline) : sources.Render("---\n" + metadata + "\n---", _pipeline, markdown.IndexOf('\n') + 1 - 4),
         _ => "",
      } : "";
      string html = sources is null ? Markdown.ToHtml(body, _pipeline) : sources.Render(body, _pipeline, markdown.Length - body.Length);
      return _sanitizer.Sanitize(prefix + html);
   }



   /// <summary>Reference delimiters cannot span code/newlines; single caret/tilde pairs also cannot contain whitespace.</summary>
   private static void PreserveLiteralExtensionBoundaries(MarkdownDocument document)
   {
      foreach(EmphasisInline emphasis in document.Descendants().OfType<EmphasisInline>().ToArray())
      {
         if(emphasis.DelimiterChar is not ('=' or '+' or '^' or '~') || (emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 2))
         {
            continue;
         }
         bool single = emphasis.DelimiterChar is '^' or '~';
         if(emphasis.Descendants().Any(node => node is CodeInline or LineBreakInline || (single && node is LiteralInline literal && literal.Content.ToString().Any(char.IsWhiteSpace))))
         {
            string delimiter = new(emphasis.DelimiterChar, emphasis.DelimiterCount);
            Inline last = emphasis.ReplaceBy(new LiteralInline(delimiter));
            last.InsertAfter(new LiteralInline(delimiter));
         }
      }
   }



   /// <summary>Retains only passive reference styling classes; native identifiers, handlers and arbitrary document classes remain unavailable.</summary>
   private static void FilterStylingClasses(object? sender, PostProcessNodeEventArgs args)
   {
      if(args.Node is IElement element && element.GetAttribute("class") is { } classes)
      {
         string[] allowed = classes.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Where(value =>
            value is "task-list" or "task-list-item" or "frontmatter" or "fm-key" or "fm-value" ||
            value.StartsWith("language-", StringComparison.Ordinal) && value.Length is > 9 and <= 80 &&
            value[9..].All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '+' or '#')).Distinct(StringComparer.Ordinal).ToArray();
         if(allowed.Length == 0)
         {
            element.RemoveAttribute("class");
         }
         else
         {
            element.SetAttribute("class", string.Join(' ', allowed));
         }
      }
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
