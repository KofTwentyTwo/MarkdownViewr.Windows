/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>
/// Pins the exact elements, attributes and link schemes that authored HTML may keep. Widening
/// the renderer's allowlist fails here until this independent list is changed deliberately.
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
      "address", "applet", "area", "article", "aside", "audio", "base", "basefont", "bdi", "bdo", "bgsound", "big", "blink",
      "button", "canvas", "center", "cite", "col", "colgroup", "data", "datalist", "dfn", "dialog", "dir", "embed", "fencedframe",
      "fieldset", "figcaption", "figure", "font", "footer", "form", "frame", "frameset", "header", "hgroup", "iframe",
      "keygen", "label", "legend", "link", "listing", "main", "map", "marquee", "math", "menu", "menuitem", "meta", "meter",
      "nav", "nobr", "noembed", "noframes", "noscript", "object", "optgroup", "option", "output", "param", "picture",
      "plaintext", "portal", "progress", "q", "rb", "rp", "rt", "rtc", "ruby", "samp", "script", "search", "section", "select",
      "selectedcontent", "slot", "source", "strike", "style", "svg", "template", "textarea", "tfoot", "time", "title", "track",
      "tt", "u", "var", "video", "wbr", "xmp", "custom-element",
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
      "enctype", "enterkeyhint", "face", "for", "form", "formaction", "formmethod", "headers", "hidden", "http-equiv", "id",
      "inert", "inputmode", "integrity", "is", "ismap", "itemprop", "label", "lang", "list", "loading", "longdesc", "loop",
      "lowsrc", "manifest", "max", "maxlength", "media", "method", "min", "multiple", "muted", "name", "nonce", "novalidate",
      "nowrap", "onclick", "onerror", "onfocus", "onload", "onmouseover", "onpointerdown", "ontoggle", "open", "part",
      "pattern", "ping", "placeholder", "popover", "popovertarget", "poster", "preload", "readonly", "referrerpolicy", "rel",
      "required", "reversed", "role", "sandbox", "scope", "selected", "shape", "size", "sizes", "slot", "span", "spellcheck",
      "srcdoc", "srclang", "srcset", "start", "step", "style", "tabindex", "target", "translate", "usemap", "valign", "value",
      "wrap", "xlink:href", "xml:lang", "xmlns", "aria-label",
   ];



   /// <summary>Every allowed element survives in a document that nests each one where HTML permits it.</summary>
   [Fact]
   public void Render_EveryAllowedElement_SurvivesSanitation()
   {
      const string Markup = """
         <div><p><a href="x.md">a</a><abbr>b</abbr><b>c</b><br><code>d</code><del>e</del><em>f</em><i>g</i><img src="i.png" alt="h"><input type="checkbox"><ins>j</ins><kbd>k</kbd><mark>l</mark><s>m</s><small>n</small><span>o</span><strong>p</strong><sub>q</sub><sup>r</sup></p><blockquote>s</blockquote><hr><pre>t</pre><h1>1</h1><h2>2</h2><h3>3</h3><h4>4</h4><h5>5</h5><h6>6</h6><dl><dt>u</dt><dd>v</dd></dl><details><summary>w</summary>x</details><ul><li>y</li></ul><ol><li>z</li></ol><table><caption>c</caption><thead><tr><th>h</th></tr></thead><tbody><tr><td>d</td></tr></tbody></table></div>
         """;
      using IHtmlDocument document = Parse(new MarkdownRenderer().Render(Markup));
      Assert.All(s_allowedTags, tag => Assert.NotNull(document.Body!.QuerySelector(tag)));
      Assert.Equal(s_allowedTags.Order(StringComparer.Ordinal), ElementNames(document), StringComparer.Ordinal);
   }



   /// <summary>No other element survives, alone or between allowed elements; its text may remain but its capability does not.</summary>
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



   /// <summary>Exactly the allowed attributes survive on an allowed element; identity, behaviour, styling and data attributes do not.</summary>
   [Fact]
   public void Render_EveryAttribute_KeepsExactlyTheAllowedOnes()
   {
      string attributes = string.Join(' ', s_allowedAttributes.Concat(s_otherAttributes).Select(name => string.Equals(name, "class", StringComparison.Ordinal) ? "class=\"frontmatter\"" : name + "=\"1\""));
      using IHtmlDocument document = Parse(new MarkdownRenderer().Render($"<div><span {attributes}>text</span></div>"));
      IElement span = Assert.Single(document.Body!.QuerySelectorAll("span"));
      Assert.Equal(s_allowedAttributes, span.Attributes.Select(attribute => attribute.Name).Order(StringComparer.Ordinal), StringComparer.Ordinal);
      Assert.Empty(s_allowedAttributes.Intersect(s_otherAttributes, StringComparer.Ordinal));
   }



   /// <summary>Only web and mail links keep a target; every other scheme, including ones Windows would hand to another application, is removed.</summary>
   [Theory]
   [InlineData("http://example.com/a", true)]
   [InlineData("https://example.com/a", true)]
   [InlineData("mailto:someone@example.com", true)]
   [InlineData("child.md", true)]
   [InlineData("#section", true)]
   [InlineData("ftp://example.com/a", false)]
   [InlineData("file:///C:/private.txt", false)]
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
   [InlineData("search-ms:query=x", false)]
   [InlineData("shell:startup", false)]
   [InlineData("res://ieframe.dll/x", false)]
   [InlineData("view-source:https://example.com", false)]
   [InlineData("onenote:x", false)]
   [InlineData("HTTPS://example.com/a", true)]
   [InlineData("FILE:///C:/private.txt", false)]
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
