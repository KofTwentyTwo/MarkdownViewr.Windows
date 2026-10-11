/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Verifies useful document output and hostile-markup boundaries before browser integration.</summary>
public sealed class MarkdownRendererTests
{
   /// <summary>GFM content retains headings, tables, task markers, strikethrough and fenced code.</summary>
   [Fact]
   public void Render_GfmDocument_PreservesRequiredFormatting()
   {
      const string Markdown = "# Heading\n\n| Name | Value |\n| --- | --- |\n| A | B |\n\n- [x] Done\n- [ ] Pending\n\n~~Old~~\n\n```csharp\nvar x = 1;\n```\n\nhttps://example.com\n";
      IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer().Render(Markdown));
      Assert.Equal("Heading", document.QuerySelector("h1")!.TextContent);
      Assert.Equal("B", document.QuerySelectorAll("td")[1].TextContent);
      Assert.Equal("Old", document.QuerySelector("del")!.TextContent);
      Assert.Contains("var x = 1;", document.QuerySelector("pre code")!.TextContent, StringComparison.Ordinal);
      Assert.Equal("https://example.com", document.QuerySelector("a")!.GetAttribute("href"));
      IElement[] inputs = document.QuerySelectorAll("input").ToArray();
      Assert.Equal(2, inputs.Length);
      Assert.All(inputs, input => Assert.True(Assert.IsAssignableFrom<IHtmlInputElement>(input).IsDisabled));
      Assert.True(Assert.IsAssignableFrom<IHtmlInputElement>(inputs[0]).IsChecked);
      Assert.False(Assert.IsAssignableFrom<IHtmlInputElement>(inputs[1]).IsChecked);
   }



   /// <summary>Useful authored HTML and relative resources survive without active styling or identity attributes.</summary>
   [Fact]
   public void Render_SafeHtml_PreservesContentAndRelativeResources()
   {
      const string Markdown = "<details><summary>More</summary><p id='native-host' class='shell' style='color:red'>Content</p></details>\n\n![Image](images/example.png)\n\n[Section](#heading)";
      IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer().Render(Markdown));
      Assert.Equal("More", document.QuerySelector("summary")!.TextContent);
      IElement paragraph = document.QuerySelector("details p")!;
      Assert.Equal("Content", paragraph.TextContent);
      Assert.Empty(paragraph.Attributes);
      Assert.Equal("images/example.png", document.QuerySelector("img")!.GetAttribute("src"));
      Assert.Equal("#heading", document.QuerySelector("a")!.GetAttribute("href"));
   }



   /// <summary>Malformed and active markup cannot introduce executable nodes or host-facing attributes.</summary>
   [Theory]
   [InlineData("<script>window.chrome.webview.postMessage('run');</script><p onclick='evil()'>Safe</p>")]
   [InlineData("<svg><script>alert(1)</script><a xlink:href='javascript:evil()'>Bad</a></svg>")]
   [InlineData("<iframe srcdoc='<script>evil()</script>'></iframe><object data='file:///C:/private'></object>")]
   [InlineData("<form action='https://example.com'><button>Send</button><input type='password' oninput='evil()'></form>")]
   [InlineData("<meta http-equiv='refresh' content='0;url=file:///C:/private'><base href='file:///C:/private'>")]
   [InlineData("<style>@import url(https://example.com);</style><img src='image.png' onerror='evil()' srcset='https://example.com 2x'>")]
   public void Render_ActiveMarkup_RemovesExecutableElementsAndAttributes(string markdown)
   {
      IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer().Render(markdown));
      Assert.Empty(document.QuerySelectorAll("script,svg,iframe,object,embed,form,button,style,base,meta[http-equiv]"));
      foreach(IElement element in document.Body!.QuerySelectorAll("*"))
      {
         Assert.DoesNotContain(element.Attributes, attribute => attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase));
         Assert.False(element.HasAttribute("srcdoc"));
         Assert.False(element.HasAttribute("srcset"));
      }
   }



   /// <summary>Unsafe URI schemes are rejected after HTML entity/control-character decoding.</summary>
   [Theory]
   [InlineData("<a href='javascript:alert(1)'>Click</a>")]
   [InlineData("<a href='JaVaScRiPt:alert(1)'>Click</a>")]
   [InlineData("<a href='java&#x09;script:alert(1)'>Click</a>")]
   [InlineData("<a href='vbscript:evil()'>Click</a>")]
   [InlineData("<a href='file:///C:/private'>Click</a>")]
   [InlineData("<img src='data:image/svg+xml,%3Csvg%20onload=evil()%3E'>")]
   public void Render_UnsafeUrls_RemovesResourceCapabilities(string markdown)
   {
      IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer().Render(markdown));
      Assert.All(document.QuerySelectorAll("a,img"), element =>
      {
         Assert.False(element.HasAttribute("href"));
         Assert.False(element.HasAttribute("src"));
      });
   }



   /// <summary>Document-authored input types are normalized to passive checkbox markers.</summary>
   [Fact]
   public void Render_AuthoredInput_ForcesDisabledCheckbox()
   {
      IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer().Render("<input type='text' checked>"));
      IHtmlInputElement input = Assert.IsAssignableFrom<IHtmlInputElement>(document.QuerySelector("input"));
      Assert.Equal("checkbox", input.Type);
      Assert.True(input.IsDisabled);
      Assert.True(input.IsChecked);
   }



   /// <summary>Missing documents fail explicitly; empty documents render as empty content.</summary>
   [Fact]
   public void Render_NullAndEmpty_UsesExplicitContract()
   {
      var renderer = new MarkdownRenderer();
      Assert.Throws<ArgumentNullException>(() => renderer.Render(null!));
      Assert.Equal("", renderer.Render(""));
   }



   /// <summary>The size guard rejects over-limit input before parsing it.</summary>
   [Fact]
   public void Render_OverLimit_RejectsBeforeParsing()
   {
      Assert.Throws<ArgumentException>(() => new MarkdownRenderer().Render(new string('a', MarkdownRenderer.MaximumCharacters + 1)));
   }



   /// <summary>A shared renderer handles concurrent documents without cross-document state.</summary>
   [Fact]
   public async Task Render_ConcurrentDocuments_DoesNotLeakState()
   {
      var renderer = new MarkdownRenderer();
      Task<string>[] tasks = Enumerable.Range(0, 24).Select(index => Task.Run(() => renderer.Render($"# Document {index}"))).ToArray();
      string[] output = await Task.WhenAll(tasks);
      for(int index = 0; index < output.Length; index++)
      {
         Assert.Equal($"<h1>Document {index}</h1>\n", output[index]);
      }
   }
}
