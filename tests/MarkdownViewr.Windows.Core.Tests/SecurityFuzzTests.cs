/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using MarkdownViewr.Windows.Core.Documents;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Seeded mutation campaigns assert independent security properties without executing authored content or touching profiles.</summary>
[Trait("Category", "Fuzz")]
public sealed class SecurityFuzzTests
{
   private static readonly Url s_browserBase = new("https://viewer.markdownviewr.invalid/documents/fuzz/index.html");
   private static readonly string[] s_fragments =
   [
      "# Heading\n", "\r\n## Child\r\n", "Title\n=====\n", "---\ntitle: '==metadata=='\n---\n",
      "**strong** *emphasis* ~~deleted~~ ==marked== ^sup^ ~sub~ ++inserted++\n",
      "<script>window.chrome.webview.postMessage('execute')</script>",
      "<img src='x.png' onerror='execute()' srcset='https://outside.invalid/x.png 2x'>",
      "<iframe srcdoc='<script>execute()</script>'></iframe>", "<svg><a xlink:href='javascript:execute()'>x</a></svg>",
      "<h2 id='mv-heading-999' data-mv-body='999' onclick='execute()'>Raw heading</h2>\n",
      "<pre><code>==literal== ^literal^ ++literal++</code></pre>\n", "`<script>literal()</script>`\n",
      "```html\n<script>literal()</script>\n```\n", "<style>@import 'https://outside.invalid';</style>",
      "<form><input type='password' oninput='execute()'><button>submit</button></form>",
      "<a href='java&#x09;script:execute()'>x</a>", "<a href='file:///C:/synthetic/private'>x</a>",
      "[section](#heading) ![image](images/hello%20world.png)\n", "| A | B |\n|---|---|\n| x | y |\n",
      "- [x] Task\n> Quote\n", "😀 café\0\t\r\n", "<!-- <code> -->", "</code></pre></div>",
      "</style><script>execute()</script>", "&lt;script&gt;literal&lt;/script&gt;", "\"'<>:=[]()\\/!?%",
   ];



   /// <summary>Malformed HTML, Markdown, frontmatter and extension combinations never recover executable DOM or authored host attributes.</summary>
   [Theory]
   [InlineData(22023)]
   [InlineData(99173)]
   public void Markdown_MutatedDocuments_RemainPassive(int seed)
      => Campaign(seed, "markdown", GenerateMarkdown, (input, random) =>
      {
         var extensions = new MarkdownExtensions(random.Next(2) == 1, random.Next(2) == 1, random.Next(2) == 1, random.Next(2) == 1);
         string html = new MarkdownRenderer(extensions).Render(input, (FrontmatterMode)random.Next(3));
         using IHtmlDocument document = new HtmlParser().ParseDocument(html);
         AssertPassive(document);
         Assert.All(document.QuerySelectorAll("*"), element => Assert.DoesNotContain(element.Attributes, attribute => attribute.Name.StartsWith("data-mv", StringComparison.OrdinalIgnoreCase)));
      });



   /// <summary>Generated headings/folds preserve the exact literal source and keep every selection within its committed snapshot.</summary>
   [Theory]
   [InlineData(22023)]
   [InlineData(99173)]
   public void Source_MutatedDocumentsAndFolds_KeepLiteralSnapshot(int seed)
      => Campaign(seed, "source", GenerateMarkdown, (input, random) =>
      {
         var outline = new DocumentOutline(input, (FrontmatterMode)random.Next(3));
         var source = new DocumentSourceText(input, outline);
         Assert.Equal(input.ReplaceLineEndings("\r"), source.Text);
         Assert.Equal(input, source.GetCopyText(0, source.Text.Length));
         Assert.All(outline.SourceHeadings, heading =>
         {
            Assert.InRange(heading.Start, 0, input.Length);
            Assert.InRange(heading.End, heading.Start, input.Length);
            Assert.InRange(heading.Index, 0, outline.Headings.Count - 1);
         });
         source.SetCollapsed(outline.Headings.Where(_ => random.Next(2) == 1).Select(heading => heading.Index));
         int start = random.Next(input.Length + 1);
         int length = random.Next(input.Length - start + 1);
         if(source.GetDisplayRange(start, length) is { } range)
         {
            Assert.InRange(range.Start, 0, source.Text.Length);
            Assert.InRange(range.Length, 0, source.Text.Length - range.Start);
         }
         source.SetCollapsed([]);
         Assert.Equal(input.ReplaceLineEndings("\r"), source.Text);
         Assert.Equal(input, source.GetCopyText(0, source.Text.Length));
         using IHtmlDocument document = new HtmlParser().ParseDocument(outline.Html);
         AssertPassive(document);
         Assert.Equal(outline.Headings.Count, document.QuerySelectorAll("h1,h2,h3,h4,h5,h6").Select(element => element.Id).Distinct(StringComparer.Ordinal).Count());
      });



   /// <summary>Once-decoded accepted requests stay under a synthetic Windows folder; foreign namespaces and queries cannot become document commands.</summary>
   [Theory]
   [InlineData(22023)]
   [InlineData(99173)]
   public void Resources_MutatedRequests_KeepNamespaceAndPathBounds(int seed)
      => Campaign(seed, "resources", random =>
      {
         string[] segments = ["images", "nested", "hello%20world", "..", "%2e%2e", "%252e%252e", "%2f", "%5c", "C%3a", "file%3astream", "COM¹", "NUL", "end.", "end%20", "café", "%00", "<script>"];
         string[] endings = [".md", ".txt", ".PNG", ".svg", ".exe", ".html", ".md%3aexe", ".mkd"];
         return string.Join('/', Enumerable.Range(0, random.Next(1, 6)).Select(_ => segments[random.Next(segments.Length)])) + endings[random.Next(endings.Length)];
      }, (input, random) =>
      {
         var policy = new DocumentResourcePolicy();
         Uri request = new(policy.DocumentUri, input);
         AssertAcceptedPath(policy.GetDocumentRelativePath(request));
         AssertAcceptedPath(policy.GetImageRelativePath(request));
         var outside = new UriBuilder(request) { Host = "outside.invalid" };
         Assert.Null(policy.GetDocumentRelativePath(outside.Uri));
         Assert.Null(policy.GetImageRelativePath(outside.Uri));
         var query = new UriBuilder(request) { Query = "command=execute" };
         Assert.Null(policy.GetDocumentRelativePath(query.Uri));
         Assert.NotNull(policy.GetDocumentRelativePath(new Uri(policy.DocumentUri, "safe.md")));
         int count = random.Next(1, 12);
         int index = random.Next(-3, 16);
         Uri heading = new(policy.DocumentUri.AbsoluteUri + "#mv-heading-" + index.ToString(CultureInfo.InvariantCulture));
         int? result = policy.GetHeadingIndex(heading, count);
         if(result is { } accepted)
         {
            Assert.InRange(accepted, 0, count - 1);
         }
         Assert.Null(policy.GetHeadingIndex(new UriBuilder(heading) { Host = "outside.invalid" }.Uri, count));
      });



   /// <summary>Accepted imported JSON and hostile titles/CSS cannot escape the document's title/style containers.</summary>
   [Theory]
   [InlineData(22023)]
   [InlineData(99173)]
   public void Themes_MutatedJson_KeepPassivePageBoundaries(int seed)
      => Campaign(seed, "themes", GenerateTheme, (input, random) =>
      {
         ReaderTheme theme;
         try
         {
            theme = ReaderTheme.ParseCompatible(input);
         }
         catch(JsonException)
         {
            return; // Invalid imports are expected to be rejected, not silently sanitized into stored themes.
         }
         string title = s_fragments[random.Next(s_fragments.Length)];
         string css = s_fragments[random.Next(s_fragments.Length)];
         string html = DocumentPage.Build("<p>trusted-marker</p>", title, theme, new ReadingAppearance(true, 700, css), 1);
         using IHtmlDocument document = new HtmlParser().ParseDocument(html);
         Assert.Equal(title.ReplaceLineEndings("\n").Replace('\0', '\uFFFD'), document.QuerySelector("title")!.TextContent);
         Assert.Equal("trusted-marker", document.QuerySelector("#content-inner")!.TextContent);
         Assert.Single(document.QuerySelectorAll("style"));
         Assert.Single(document.QuerySelectorAll("title"));
         Assert.Empty(document.QuerySelectorAll("script,iframe,object,embed,form,button,img,svg"));
      });



   /// <summary>The minimized campaign input is a browser-relative authority, not a standalone Windows file URI; it never gains local image access.</summary>
   [Fact]
   public void Resources_BackslashAuthority_DeniesForeignImage()
   {
      const string Markdown = "<img src='\\\\x.png' onerror='execute()'>";
      using IHtmlDocument document = new HtmlParser().ParseDocument(new MarkdownRenderer().Render(Markdown));
      AssertPassive(document);
      string source = document.QuerySelector("img")!.GetAttribute("src")!;
      var policy = new DocumentResourcePolicy();
      Uri target = new(new Url(new Url(policy.DocumentUri.AbsoluteUri), source).Href);
      Assert.Equal("https", target.Scheme);
      Assert.Null(policy.GetImageRelativePath(target));
   }



   /// <summary>Combines valid/malformed corpus pieces, then deletes, inserts or repeats one synthetic character.</summary>
   private static string GenerateMarkdown(Random random)
   {
      string input = string.Concat(Enumerable.Range(0, random.Next(1, 18)).Select(_ => s_fragments[random.Next(s_fragments.Length)]));
      const string MutationCharacters = "<>[]`~^=+\r\n'\"\\";
      int at = random.Next(input.Length + 1);
      return random.Next(3) switch
      {
         0 when at < input.Length => input.Remove(at, 1),
         1 => input.Insert(at, s_fragments[random.Next(s_fragments.Length)]),
         _ => input.Insert(at, new string(MutationCharacters[random.Next(MutationCharacters.Length)], random.Next(1, 20))),
      };
   }



   /// <summary>Mutates actual built-in schemas and syntax without substituting a mock parser or supplying private documents.</summary>
   private static string GenerateTheme(Random random)
   {
      JsonNode theme = JsonNode.Parse(ThemeCatalog.BuiltIn[random.Next(ThemeCatalog.BuiltIn.Count)].Json)!;
      string payload = s_fragments[random.Next(s_fragments.Length)];
      switch(random.Next(7))
      {
         case 0:
            theme["name"] = payload;
            break;
         case 1:
            theme["fonts"]!["body"] = payload;
            break;
         case 2:
            theme["colors"]!["text"] = payload;
            break;
         case 3:
            theme["customCSS"] = payload;
            break;
         case 4:
            theme["schemaVersion"] = random.Next(-2, 6);
            break;
         case 5:
            theme["sizes"]!["baseFontSize"] = random.Next(-16, 160);
            break;
         default:
            theme["metadata"] = payload;
            break;
      }
      string input = theme.ToJsonString();
      return random.Next(5) == 0 ? input.Remove(random.Next(input.Length), 1) : input;
   }



   /// <summary>Checks the real platform path result independently, with no disk access, URL execution or host bridge.</summary>
   private static void AssertAcceptedPath(string? path)
   {
      if(path is null)
      {
         return;
      }
      string root = Path.Combine(Path.GetTempPath(), "k22-synthetic-fuzz-root") + Path.DirectorySeparatorChar;
      Assert.False(Path.IsPathRooted(path));
      Assert.StartsWith(root, Path.GetFullPath(Path.Combine(root, path)), StringComparison.Ordinal);
      Assert.DoesNotContain(':', path);
      Assert.DoesNotContain('\\', path);
      Assert.DoesNotContain(path, char.IsControl);
      Assert.All(path.Split('/'), segment => Assert.False(segment.EndsWith('.') || segment.EndsWith(' ')));
   }



   /// <summary>Uses the parsed DOM's executable capabilities as the oracle, not the renderer's private allowlist.</summary>
   private static void AssertPassive(IHtmlDocument document)
   {
      Assert.Empty(document.QuerySelectorAll("script,svg,math,iframe,object,embed,form,button,style,base,meta[http-equiv],audio,video"));
      foreach(IElement element in document.QuerySelectorAll("*"))
      {
         Assert.DoesNotContain(element.Attributes, attribute => attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase));
         Assert.False(element.HasAttribute("srcdoc") || element.HasAttribute("srcset") || element.HasAttribute("style"));
         foreach(string name in new[] { "href", "src" })
         {
            string? value = element.GetAttribute(name);
            if(value is not null)
            {
               var target = new Url(s_browserBase, value);
               if(!target.IsInvalid)
               {
                  Assert.Contains(target.Scheme, new[] { "http", "https", "mailto" }, StringComparer.Ordinal);
               }
            }
         }
      }
      Assert.All(document.QuerySelectorAll("input"), element =>
      {
         IHtmlInputElement input = Assert.IsAssignableFrom<IHtmlInputElement>(element);
         Assert.True(input.IsDisabled);
         Assert.Equal("checkbox", input.Type);
      });
   }



   /// <summary>Every failure includes a reproducible seed, exact synthetic input and property exception; campaign limits fail closed.</summary>
   private static void Campaign(int seed, string target, Func<Random, string> generate, Action<string, Random> verify)
   {
      string? configured = Environment.GetEnvironmentVariable("K22_FUZZ_ITERATIONS");
      int iterations = 500;
      if(configured is not null && (!int.TryParse(configured, NumberStyles.None, CultureInfo.InvariantCulture, out iterations) || iterations is < 1 or > 100000))
      {
         throw new InvalidOperationException("K22_FUZZ_ITERATIONS must be between1 and100000.");
      }
      var random = new Random(seed);
      for(int iteration = 0; iteration < iterations; iteration++)
      {
         string input = generate(random);
         Assert.InRange(input.Length, 0, 4096);
         try
         {
            verify(input, random);
         }
         catch(Exception exception)
         {
            throw new InvalidOperationException(FormattableString.Invariant($"Target={target}; Seed={seed}; Iteration={iteration}; Input={JsonSerializer.Serialize(input)}"), exception);
         }
      }
   }
}
