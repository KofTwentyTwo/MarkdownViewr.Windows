/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Ganss.Xss;
using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>
/// The elements, attributes and link schemes authored HTML may keep. The configured policy is
/// compared with the lists below as sets, so any change to it has to be made here as well, and
/// the rendering tests show the policy doing what the lists say.
/// </summary>
public sealed class SanitizerAllowlistTests
{
   private static readonly string[] s_allowedTags =
   [
      "a", "abbr", "b", "blockquote", "br", "caption", "code", "dd", "del", "details", "div", "dl", "dt", "em",
      "h1", "h2", "h3", "h4", "h5", "h6", "hr", "i", "img", "input", "ins", "kbd", "li", "mark", "ol", "p", "pre",
      "s", "small", "span", "strong", "sub", "summary", "sup", "table", "tbody", "td", "th", "thead", "tr", "ul",
   ];

   private static readonly string[] s_otherTags =
   [
      "acronym", "address", "applet", "area", "article", "aside", "audio", "base", "basefont", "bdi", "bdo", "bgsound", "big",
      "blink", "button", "canvas", "center", "cite", "data", "datalist", "dfn", "dialog", "dir", "embed", "fencedframe",
      "fieldset", "figcaption", "figure", "font", "footer", "form", "header", "hgroup", "iframe", "keygen", "label", "legend",
      "link", "listing", "main", "map", "marquee", "math", "menu", "menuitem", "meta", "meter", "nav", "nobr", "noembed",
      "noframes", "noscript", "object", "optgroup", "option", "output", "param", "picture", "plaintext", "portal", "progress",
      "q", "rb", "rp", "rt", "rtc", "ruby", "samp", "script", "search", "section", "select", "selectedcontent", "slot", "source",
      "strike", "style", "svg", "template", "textarea", "time", "title", "track", "tt", "u", "var", "video", "wbr", "xmp",
      "custom-element",
   ];

   private static readonly string[] s_allowedAttributes =
   [
      "align", "alt", "checked", "class", "colspan", "disabled", "height", "href", "rowspan", "src", "title", "type", "width",
   ];

   private static readonly string[] s_otherAttributes =
   [
      "accesskey", "action", "allow", "as", "autocapitalize", "autofocus", "autoplay", "background", "bgcolor", "border",
      "cellpadding", "cellspacing", "charset", "cite", "color", "content", "contenteditable", "controls", "coords",
      "crossorigin", "data", "data-mv-body", "data-mv-source-fixed", "data-x", "datetime", "dir", "download", "draggable",
      "enctype", "enterkeyhint", "face", "for", "form", "formaction", "formmethod", "formtarget", "headers", "hidden",
      "http-equiv", "id", "inert", "inputmode", "integrity", "is", "ismap", "itemprop", "label", "lang", "list", "loading",
      "longdesc", "loop", "lowsrc", "manifest", "max", "maxlength", "media", "method", "min", "multiple", "muted", "name",
      "nonce", "novalidate", "nowrap", "onclick", "onerror", "onfocus", "onload", "onmouseover", "onpointerdown", "ontoggle",
      "onwheel", "open", "part", "pattern", "ping", "placeholder", "popover", "popovertarget", "poster", "preload", "readonly",
      "referrerpolicy", "rel", "required", "reversed", "role", "sandbox", "scope", "selected", "shape", "size", "sizes", "slot",
      "span", "spellcheck", "srcdoc", "srclang", "srcset", "start", "step", "tabindex", "target", "translate", "usemap",
      "valign", "value", "wrap", "xlink:href", "xml:lang", "xmlns", "aria-label",
   ];



   /// <summary>The policy the renderer configures is exactly these elements, attributes and schemes, with no styling or data attributes.</summary>
   [Fact]
   public void Constructor_ConfiguredPolicy_AllowsExactlyTheListedMarkup()
   {
      HtmlSanitizer policy = new MarkdownRenderer().Policy;
      Assert.Equal(s_allowedTags.Order(StringComparer.Ordinal), policy.AllowedTags.Order(StringComparer.Ordinal), StringComparer.Ordinal);
      Assert.Equal(s_allowedAttributes.Order(StringComparer.Ordinal), policy.AllowedAttributes.Order(StringComparer.Ordinal), StringComparer.Ordinal);
      Assert.Equal(["http", "https", "mailto"], policy.AllowedSchemes.Order(StringComparer.Ordinal), StringComparer.Ordinal);
      Assert.Empty(policy.AllowedCssProperties);
      Assert.Empty(policy.AllowedAtRules);
      Assert.False(policy.AllowDataAttributes);
      Assert.False(policy.AllowCssCustomProperties);
   }



   /// <summary>Every allowed element survives in a document that nests each one where HTML permits it.</summary>
   [Fact]
   public void Render_EveryAllowedElement_SurvivesSanitation()
   {
      const string Markup = """
         <div><p><a href="x.md">a</a><abbr>b</abbr><b>c</b><br><code>d</code><del>e</del><em>f</em><i>g</i><img src="i.png" alt="h"><input type="checkbox"><ins>j</ins><kbd>k</kbd><mark>l</mark><s>m</s><small>n</small><span>o</span><strong>p</strong><sub>q</sub><sup>r</sup></p><blockquote>s</blockquote><hr><pre>t</pre><h1>1</h1><h2>2</h2><h3>3</h3><h4>4</h4><h5>5</h5><h6>6</h6><dl><dt>u</dt><dd>v</dd></dl><details><summary>w</summary>x</details><ul><li>y</li></ul><ol><li>z</li></ol><table><caption>c</caption><thead><tr><th>h</th></tr></thead><tbody><tr><td>d</td></tr></tbody></table></div>
         """;
      using IHtmlDocument document = Parse(new MarkdownRenderer().Render(Markup));
      Assert.Equal(s_allowedTags.Order(StringComparer.Ordinal), ElementNames(document), StringComparer.Ordinal);
   }



   /// <summary>No other element survives between allowed elements; its text may remain but the element does not.</summary>
   [Fact]
   public void Render_EveryOtherElement_IsRemoved()
   {
      var renderer = new MarkdownRenderer();
      Assert.All(s_otherTags, tag =>
      {
         using IHtmlDocument document = Parse(renderer.Render($"<div><{tag} title=\"t\">inner</{tag}><span>after</span></div>"));
         Assert.Empty(ElementNames(document).Except(["div", "span"], StringComparer.Ordinal));
      });
      Assert.Empty(s_allowedTags.Intersect(s_otherTags, StringComparer.Ordinal));
   }



   /// <summary>Table parts that only exist inside a table, and are not allowed, are removed there while the allowed parts stay.</summary>
   [Fact]
   public void Render_TablePartsOutsideTheAllowlist_AreRemovedInsideATable()
   {
      const string Markup = "<table><colgroup><col span=\"2\"></colgroup><thead><tr><th>h</th></tr></thead><tbody><tr><td>d</td></tr></tbody><tfoot><tr><td>f</td></tr></tfoot></table>";
      using IHtmlDocument document = Parse(new MarkdownRenderer().Render(Markup));
      Assert.Empty(document.Body!.QuerySelectorAll("colgroup,col,tfoot"));
      Assert.NotNull(document.Body.QuerySelector("table > thead > tr > th"));
      Assert.NotNull(document.Body.QuerySelector("table > tbody > tr > td"));
   }



   /// <summary>Exactly the allowed attributes survive on an allowed element; identity, behaviour, styling and data attributes do not.</summary>
   [Fact]
   public void Render_EveryAttribute_KeepsExactlyTheAllowedOnes()
   {
      string attributes = string.Join(' ', s_allowedAttributes.Concat(s_otherAttributes).Select(name => string.Equals(name, "class", StringComparison.Ordinal) ? "class=\"frontmatter\"" : name + "=\"1\"")) + " style=\"color:red\"";
      using IHtmlDocument document = Parse(new MarkdownRenderer().Render($"<div><span {attributes}>text</span></div>"));
      IElement span = Assert.Single(document.Body!.QuerySelectorAll("span"));
      Assert.Equal(s_allowedAttributes, span.Attributes.Select(attribute => attribute.Name).Order(StringComparer.Ordinal), StringComparer.Ordinal);
      Assert.Empty(s_allowedAttributes.Intersect(s_otherAttributes, StringComparer.Ordinal));
   }



   /// <summary>Only the fixed styling classes and bounded code-language classes survive, spelled exactly.</summary>
   [Theory]
   [InlineData("frontmatter fm-key fm-value task-list task-list-item", "frontmatter fm-key fm-value task-list task-list-item")]
   [InlineData("language-csharp hljs keyword", "language-csharp")]
   [InlineData("LANGUAGE-csharp Language-go language-rust", "language-rust")]
   [InlineData("Frontmatter FM-KEY", null)]
   [InlineData("language- language-a;b language-<x>", null)]
   public void Render_ClassNames_KeepsOnlyTheStylingClasses(string authored, string? kept)
   {
      using IHtmlDocument document = Parse(new MarkdownRenderer().Render($"<span class=\"{authored}\">text</span>"));
      Assert.Equal(kept, Assert.Single(document.Body!.QuerySelectorAll("span")).GetAttribute("class"));
   }



   /// <summary>Only web and mail links keep a target; every other scheme, including ones Windows would hand to another application, is removed.</summary>
   [Theory]
   [InlineData("http://example.com/a", true)]
   [InlineData("https://example.com/a", true)]
   [InlineData("mailto:someone@example.com", true)]
   [InlineData("child.md", true)]
   [InlineData("#section", true)]
   [InlineData("HTTPS://example.com/a", true)]
   [InlineData("ftp://example.com/a", false)]
   [InlineData("file:///C:/private.txt", false)]
   [InlineData("FILE:///C:/private.txt", false)]
   [InlineData("javascript:run()", false)]
   [InlineData("vbscript:run()", false)]
   [InlineData("data:text/html,<p>x</p>", false)]
   [InlineData("blob:https://example.com/1", false)]
   [InlineData("about:blank", false)]
   [InlineData("ws://example.com", false)]
   [InlineData("tel:5551234", false)]
   [InlineData("sms:5551234", false)]
   [InlineData("ms-settings:defaultapps", false)]
   [InlineData("ms-msdt:/id", false)]
   [InlineData("ms-word:ofe|u|https://example.com/a.docx", false)]
   [InlineData("search-ms:query=x", false)]
   [InlineData("shell:startup", false)]
   [InlineData("res://ieframe.dll/x", false)]
   [InlineData("view-source:https://example.com", false)]
   [InlineData("onenote:x", false)]
   public void Render_LinkScheme_KeepsOnlyWebAndMailTargets(string target, bool kept)
   {
      using IHtmlDocument document = Parse(new MarkdownRenderer().Render($"<a href=\"{target}\">link</a>"));
      IElement link = Assert.Single(document.Body!.QuerySelectorAll("a"));
      Assert.Equal(kept, link.HasAttribute("href"));
   }



   /// <summary>The distinct element names in the sanitized body, in ordinal order.</summary>
   private static string[] ElementNames(IHtmlDocument document)
      => document.Body!.QuerySelectorAll("*").Select(element => element.LocalName).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();



   /// <summary>Parses renderer output the way the browser surface does.</summary>
   private static IHtmlDocument Parse(string html) => new HtmlParser().ParseDocument("<!doctype html><html><body>" + html + "</body></html>");
}
