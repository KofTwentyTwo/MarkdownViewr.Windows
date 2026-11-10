/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Builds native heading data and nested passive sections only after Markdown has been sanitized.</summary>
public sealed class DocumentOutline
{
   private readonly List<DocumentHeading> _headings = [];
   private readonly Dictionary<IElement, DocumentHeading> _elements = [];



   /// <summary>
   /// Sanitizes first; authored identifiers cannot impersonate the generated navigation targets.
   /// A large document takes time that grows with its size: cancellation is honoured between the
   /// render, the parse and each heading, so a closed window or a newer load does not wait for it.
   /// </summary>
   public DocumentOutline(string markdown, FrontmatterMode frontmatter = FrontmatterMode.Hide, MarkdownExtensions? extensions = null, CancellationToken cancellation = default)
   {
      var sources = new DocumentSourceMap(markdown);
      var sourceHeadings = new List<DocumentSourceHeading>();
      cancellation.ThrowIfCancellationRequested();
      string rendered = new MarkdownRenderer(extensions).RenderWithSources(markdown, frontmatter, sources);
      cancellation.ThrowIfCancellationRequested();
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser().ParseDocument(rendered);
      IElement body = document.Body!;
      var fragments = new Dictionary<string, string>(StringComparer.Ordinal);
      var nextSuffix = new Dictionary<string, int>(StringComparer.Ordinal);
      foreach(IElement element in body.QuerySelectorAll("h1,h2,h3,h4,h5,h6"))
      {
         cancellation.ThrowIfCancellationRequested();
         var heading = new DocumentHeading(_headings.Count, element.LocalName[1] - '0', element.TextContent.Trim());
         _headings.Add(heading);
         if(element.GetAttribute(sources.AttributeName) is { } identity && sources.TryGetPosition(identity, out (int Start, int End) position))
         {
            sourceHeadings.Add(new DocumentSourceHeading(heading.Index, position.Start, position.End));
         }
         _elements.Add(element, heading);
         element.Id = "mv-heading-" + heading.Index.ToString(CultureInfo.InvariantCulture);
         string slug = Markdig.Helpers.LinkHelper.UrilizeAsGfm(heading.Title);
         // Repeated titles continue from the last suffix used for that title instead of rescanning from one.
         string fragment = slug;
         int suffix = nextSuffix.GetValueOrDefault(slug, 1);
         while(fragments.ContainsKey(fragment))
         {
            fragment = slug + "-" + suffix.ToString(CultureInfo.InvariantCulture);
            suffix++;
         }
         nextSuffix[slug] = suffix;
         fragments.Add(fragment, element.Id);
      }
      foreach(IElement element in body.QuerySelectorAll("[" + sources.AttributeName + "]"))
      {
         element.RemoveAttribute(sources.AttributeName);
      }
      foreach(IElement link in body.QuerySelectorAll("a[href]"))
      {
         string href = link.GetAttribute("href")!;
         if(href.StartsWith('#') && fragments.TryGetValue(Uri.UnescapeDataString(href[1..]), out string? target))
         {
            link.SetAttribute("href", "#" + target);
         }
      }
      cancellation.ThrowIfCancellationRequested();
      foreach(IElement element in body.QuerySelectorAll("*").Reverse())
      {
         if(!_elements.ContainsKey(element))
         {
            WrapSections(element, document);
         }
      }
      WrapSections(body, document);
      Html = body.InnerHtml;
      Headings = _headings.AsReadOnly();
      SourceHeadings = sourceHeadings.AsReadOnly();
   }



   /// <summary>Sanitized HTML plus only the host's generated identifiers and structural attributes.</summary>
   public string Html { get; }



   /// <summary>Headings in document order, including duplicate labels and headings inside authored containers.</summary>
   public IReadOnlyList<DocumentHeading> Headings { get; }



   /// <summary>Parser-derived offsets of surviving headings in the original literal snapshot, independent of duplicate labels.</summary>
   public IReadOnlyList<DocumentSourceHeading> SourceHeadings { get; }



   /// <summary>Groups each heading until the next peer or ancestor, keeping nested section collapse independent.</summary>
   private void WrapSections(IElement parent, IDocument document)
   {
      INode[] children = parent.ChildNodes.ToArray();

      parent.TextContent = "";
      var sections = new Stack<(int Level, IElement Content)>();
      foreach(INode child in children)
      {
         if(child is IElement element && _elements.TryGetValue(element, out DocumentHeading? heading))
         {
            while(sections.TryPeek(out (int Level, IElement Content) top) && top.Level >= heading.Level)
            {
               sections.Pop();
            }
            IElement section = document.CreateElement("section");
            IElement content = document.CreateElement("div");
            content.SetAttribute("data-mv-body", heading.Index.ToString(CultureInfo.InvariantCulture));
            section.AppendChild(element);
            section.AppendChild(content);
            (sections.Count > 0 ? sections.Peek().Content : parent).AppendChild(section);
            sections.Push((heading.Level, content));
         }
         else
         {
            (sections.Count > 0 ? sections.Peek().Content : parent).AppendChild(child);
         }
      }
   }
}
