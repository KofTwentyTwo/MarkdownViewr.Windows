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



   /// <summary>Sanitizes first; authored identifiers cannot impersonate the generated navigation targets.</summary>
   public DocumentOutline(string markdown, FrontmatterMode frontmatter = FrontmatterMode.Hide)
   {
      using AngleSharp.Html.Dom.IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer().Render(markdown, frontmatter));
      IElement body = document.Body!;
      var fragments = new Dictionary<string, string>(StringComparer.Ordinal);
      foreach(IElement element in body.QuerySelectorAll("h1,h2,h3,h4,h5,h6"))
      {
         var heading = new DocumentHeading(_headings.Count, element.LocalName[1] - '0', element.TextContent.Trim());
         _headings.Add(heading);
         _elements.Add(element, heading);
         element.Id = "mv-heading-" + heading.Index.ToString(CultureInfo.InvariantCulture);
         string slug = Markdig.Helpers.LinkHelper.UrilizeAsGfm(heading.Title);
         string fragment = slug;
         for(int suffix = 1; fragments.ContainsKey(fragment); suffix++)
         {
            fragment = slug + "-" + suffix.ToString(CultureInfo.InvariantCulture);
         }
         fragments.Add(fragment, element.Id);
      }
      foreach(IElement link in body.QuerySelectorAll("a[href]"))
      {
         string href = link.GetAttribute("href")!;
         if(href.StartsWith('#') && fragments.TryGetValue(Uri.UnescapeDataString(href[1..]), out string? target))
         {
            link.SetAttribute("href", "#" + target);
         }
      }
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
   }



   /// <summary>Sanitized HTML plus only the host's generated identifiers and structural attributes.</summary>
   public string Html { get; }



   /// <summary>Headings in document order, including duplicate labels and headings inside authored containers.</summary>
   public IReadOnlyList<DocumentHeading> Headings { get; }



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
