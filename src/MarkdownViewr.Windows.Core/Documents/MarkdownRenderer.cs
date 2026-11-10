/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Css.Parser;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
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

   // The sanitizer's own parser settings: scripting on, so noscript content stays text, and its CSS-aware context.
   private static readonly IBrowsingContext s_sanitizerContext = BrowsingContext.New(Configuration.Default.WithCss(new CssParserOptions
   {
      IsIncludingUnknownDeclarations = true,
      IsIncludingUnknownRules = true,
      IsToleratingInvalidSelectors = true,
   }));

   private readonly MarkdownPipeline _pipeline;

   private readonly MarkdownExtensions _extensions;

   private readonly HtmlSanitizer _sanitizer;



   /// <summary>Allows document formatting without scripts, CSS, forms or native host capabilities.</summary>
   public MarkdownRenderer(MarkdownExtensions? extensions = null)
   {
      _extensions = extensions ?? new MarkdownExtensions();
      MarkdownPipelineBuilder pipeline = new MarkdownPipelineBuilder()
         .UsePipeTables(new PipeTableOptions { UseGfmRules = true })
         .UseTaskLists()
         .UseEmphasisExtras(_extensions.EmphasisOptions)
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
         FrontmatterMode.Metadata => RenderDocument(Markdown.Parse(DocumentFrontmatter.RenderMetadata(metadata), _pipeline), extendSubscript: false),
         FrontmatterMode.Markdown => sources is null ? RenderDocument(Markdown.Parse("---\n" + metadata + "\n---", _pipeline)) : sources.Render("---\n" + metadata + "\n---", _pipeline, markdown.IndexOf('\n') + 1 - 4, RenderDocument),
         _ => "",
      } : "";
      string html = sources is null ? RenderDocument(Markdown.Parse(body, _pipeline)) : sources.Render(body, _pipeline, markdown.Length - body.Length, RenderDocument);
      return Sanitize(prefix + html);
   }



   /// <summary>
   /// Parses once with the sanitizer's own settings, proves the tree is shallow enough for the
   /// sanitizer's recursive passes, and only then sanitizes and serializes it.
   /// </summary>
   private string Sanitize(string html)
   {
      var nesting = new HtmlNesting();
      var parser = new HtmlParser(new HtmlParserOptions { IsScripting = true, OnToken = (token, _) => nesting.Observe(token) }, s_sanitizerContext);
      using IHtmlDocument document = parser.ParseDocument("<!doctype html><html><body>" + html);
      HtmlNesting.ThrowIfTooDeep(document.Body!);
      _sanitizer.SanitizeDom(document, document.Body!);
      return document.Body!.ChildNodes.ToHtml(_sanitizer.OutputFormatter);
   }



   /// <summary>Normal documents use every selected extension; source annotation remains upstream of visible raw-text formatting.</summary>
   private string RenderDocument(MarkdownDocument document) => RenderDocument(document, extendSubscript: true);



   /// <summary>Formats visible authored HTML text without touching attributes/code; reference metadata excludes body-only subscript preprocessing.</summary>
   private string RenderDocument(MarkdownDocument document, bool extendSubscript)
   {
      int codeDepth = 0;
      foreach(MarkdownObject node in document.Descendants().Where(item => item is HtmlInline or HtmlBlock))
      {
         if(node is HtmlInline inline)
         {
            codeDepth = Math.Max(0, codeDepth + HtmlCodeBoundary(inline));
            continue;
         }
         var block = (HtmlBlock)node;
         string html = string.Join('\n', block.Lines.Lines.Take(block.Lines.Count).Select(line => line.Slice.ToString()));
         var changes = new List<(int Start, int End, string Value)>();
         var nesting = new HtmlNesting();
         var parser = new HtmlParser(new HtmlParserOptions
         {
            OnToken = (token, range) =>
            {
               nesting.Observe(token);
               if(token.Type is HtmlTokenType.StartTag or HtmlTokenType.EndTag && token.Name is "code" or "pre")
               {
                  codeDepth = Math.Max(0, codeDepth + (token.Type == HtmlTokenType.StartTag ? 1 : -1));
               }
               else if(token.Type == HtmlTokenType.Character && codeDepth == 0)
               {
                  // An unterminated script or comment reports a range past the end of the block.
                  int start = Math.Min(range.Start.Index, html.Length);
                  int end = Math.Clamp(range.End.Index, start, html.Length);
                  string text = html[start..end];
                  string formatted = FormatRawText(text, extendSubscript);
                  if(!string.Equals(text, formatted, StringComparison.Ordinal))
                  {
                     changes.Add((start, end, formatted));
                  }
               }
            },
         });
         using IHtmlDocument parsed = parser.ParseDocument(html);
         if(changes.Count == 0)
         {
            continue;
         }
         string[] lines = Rewrite(html, changes).Split('\n');
         for(int index = 0; index < block.Lines.Count; index++)
         {
            block.Lines.Lines[index].Slice = new Markdig.Helpers.StringSlice(lines[index]);
         }
      }
      return Markdown.ToHtml(document, _pipeline);
   }



   /// <summary>
   /// Applies replacements in one forward pass, so the work is linear in the block. Malformed markup
   /// can report a text run that starts inside an earlier one; that run is left as authored.
   /// </summary>
   private static string Rewrite(string html, List<(int Start, int End, string Value)> changes)
   {
      var rewritten = new StringBuilder(html.Length + 64);
      int copied = 0;
      foreach((int start, int end, string value) in changes)
      {
         if(start < copied)
         {
            continue;
         }
         rewritten.Append(html, copied, start - copied).Append(value);
         copied = end;
      }
      return rewritten.Append(html, copied, html.Length - copied).ToString();
   }



   /// <summary>Reference extension syntax applies only to tokenized visible text, with bounded regex work and unchanged entity spelling.</summary>
   private string FormatRawText(string text, bool extendSubscript)
   {
      foreach((bool enabled, string pattern, string tag) in new[]
      {
         (_extensions.Highlight, "==(.+?)==", "mark"),
         (_extensions.Superscript, "\\^([^\\s\\^]+)\\^", "sup"),
         (_extensions.Subscript && extendSubscript, "(?<!~)~([^\\s~]+)~(?!~)", "sub"),
         (_extensions.Underline, "\\+\\+(.+?)\\+\\+", "ins"),
      })
      {
         if(enabled)
         {
            text = Regex.Replace(text, pattern, "<" + tag + ">$1</" + tag + ">", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
         }
      }
      return text;
   }



   /// <summary>Reference delimiters cannot span code/newlines; single caret/tilde pairs also cannot contain whitespace.</summary>
   private static void PreserveLiteralExtensionBoundaries(MarkdownDocument document)
   {
      int codeDepth = 0;
      foreach(MarkdownObject node in document.Descendants().Where(item => item is EmphasisInline or HtmlInline or HtmlBlock).ToArray())
      {
         if(node is HtmlInline html)
         {
            codeDepth = Math.Max(0, codeDepth + HtmlCodeBoundary(html));
         }
         else if(node is HtmlBlock block)
         {
            var nesting = new HtmlNesting();
            var parser = new HtmlParser(new HtmlParserOptions
            {
               OnToken = (token, _) =>
               {
                  nesting.Observe(token);
                  if(token.Type is HtmlTokenType.StartTag or HtmlTokenType.EndTag && token.Name is "code" or "pre")
                  {
                     codeDepth = Math.Max(0, codeDepth + (token.Type == HtmlTokenType.StartTag ? 1 : -1));
                  }
               },
            });
            using IHtmlDocument htmlDocument = parser.ParseDocument(string.Join('\n', block.Lines.Lines.Take(block.Lines.Count).Select(line => line.Slice.ToString())));
         }
         if(node is not EmphasisInline emphasis || emphasis.DelimiterChar is not ('=' or '+' or '^' or '~') || (emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 2))
         {
            continue;
         }
         bool single = emphasis.DelimiterChar is '^' or '~';
         if(codeDepth > 0 || emphasis.Descendants().Any(child => child is CodeInline or LineBreakInline || child is HtmlInline boundary && HtmlCodeBoundary(boundary) != 0 || (single && child is LiteralInline literal && literal.Content.ToString().Any(char.IsWhiteSpace))))
         {
            string delimiter = new(emphasis.DelimiterChar, emphasis.DelimiterCount);
            Inline last = emphasis.ReplaceBy(new LiteralInline(delimiter));
            last.InsertAfter(new LiteralInline(delimiter));
         }
      }
   }



   /// <summary>Only actual authored code/pre tag names protect extension delimiters, including mixed-case tags.</summary>
   private static int HtmlCodeBoundary(HtmlInline html)
   {
      ReadOnlySpan<char> tag = html.Tag.AsSpan(1);
      bool closing = tag.StartsWith("/", StringComparison.Ordinal);
      if(closing)
      {
         tag = tag[1..];
      }
      tag = tag[..tag.IndexOfAny(" />\t\r\n\f".AsSpan())];
      return tag.Equals("code", StringComparison.OrdinalIgnoreCase) || tag.Equals("pre", StringComparison.OrdinalIgnoreCase) ? closing ? -1 : 1 : 0;
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
