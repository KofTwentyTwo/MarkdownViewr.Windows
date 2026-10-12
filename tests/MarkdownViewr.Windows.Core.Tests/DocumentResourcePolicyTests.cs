/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Verifies document image isolation and Windows path rejection before any native file access.</summary>
public sealed class DocumentResourcePolicyTests
{
   /// <summary>Generated fragments address only bounded heading identities, never an arbitrary native command.</summary>
   [Theory]
   [InlineData("#mv-heading-0", 1, 0)]
   [InlineData("#mv-heading-2", 3, 2)]
   [InlineData("#mv-heading-3", 3, null)]
   [InlineData("#mv-heading-0", 0, null)]
   [InlineData("#mv-heading--1", 3, null)]
   [InlineData("#mv-heading-+1", 3, null)]
   [InlineData("#mv-heading-2147483648", 3, null)]
   [InlineData("#mv-heading-", 3, null)]
   [InlineData("#heading", 3, null)]
   [InlineData("?command=edit#mv-heading-0", 3, null)]
   public void GetHeadingIndex_Fragment_RequiresBoundedGeneratedIndex(string suffix, int count, int? expected)
   {
      var policy = new DocumentResourcePolicy();
      Assert.Equal(expected, policy.GetHeadingIndex(new Uri(policy.DocumentUri.AbsoluteUri + suffix), count));
   }



   /// <summary>Relative requests, another namespace/path/authority, and missing input cannot impersonate this document.</summary>
   [Fact]
   public void GetHeadingIndex_OtherDocumentOrAuthority_RejectsTarget()
   {
      var policy = new DocumentResourcePolicy();
      Assert.Null(policy.GetHeadingIndex(new Uri("#mv-heading-0", UriKind.Relative), 1));
      Assert.Null(policy.GetHeadingIndex(new Uri(new DocumentResourcePolicy().DocumentUri.AbsoluteUri + "#mv-heading-0"), 1));
      Assert.Null(policy.GetHeadingIndex(new Uri(policy.DocumentUri, "other.html#mv-heading-0"), 1));
      var authority = new UriBuilder(policy.DocumentUri) { Host = "example.com", Fragment = "mv-heading-0" };
      Assert.Null(policy.GetHeadingIndex(authority.Uri, 1));
      authority.Host = policy.DocumentUri.Host;
      authority.UserName = "attacker";
      Assert.Null(policy.GetHeadingIndex(authority.Uri, 1));
      Assert.Throws<ArgumentNullException>(() => policy.GetHeadingIndex(null!, 1));
   }


   /// <summary>Each opened document has a distinct, inert HTTPS namespace.</summary>
   [Fact]
   public void Constructor_DifferentDocuments_UsesDistinctNamespaces()
   {
      var first = new DocumentResourcePolicy();
      var second = new DocumentResourcePolicy();
      Assert.NotEqual(first.DocumentUri, second.DocumentUri);
      Assert.Equal("https", first.DocumentUri.Scheme);
      Assert.Equal("viewer.markdownviewr.invalid", first.DocumentUri.Host);
      Assert.EndsWith("/index.html", first.DocumentUri.AbsolutePath, StringComparison.Ordinal);
   }



   /// <summary>Supported local images retain their relative spelling and are decoded only once.</summary>
   [Theory]
   [InlineData("images/photo.PNG", "images/photo.PNG")]
   [InlineData("images/hello%20world.jpg", "images/hello world.jpg")]
   [InlineData("images/%252e%252e.png", "images/%2e%2e.png")]
   [InlineData("diagram.svg#view", "diagram.svg")]
   [InlineData("image.webp?cache=1", "image.webp")]
   public void GetImageRelativePath_AllowedImage_ReturnsOnceDecodedPath(string path, string expected)
   {
      var policy = new DocumentResourcePolicy();
      Assert.Equal(expected, policy.GetImageRelativePath(new Uri(policy.DocumentUri, path)));
   }



   /// <summary>Other origins, ports, credentials and document namespaces have no resource access.</summary>
   [Theory]
   [InlineData("http://viewer.markdownviewr.invalid/image.png")]
   [InlineData("https://example.com/image.png")]
   [InlineData("https://viewer.markdownviewr.invalid:444/image.png")]
   [InlineData("https://user@viewer.markdownviewr.invalid/image.png")]
   [InlineData("file:///C:/private.png")]
   [InlineData("data:image/png;base64,AAAA")]
   public void GetImageRelativePath_ForeignRequest_DeniesAccess(string url)
   {
      Assert.Null(new DocumentResourcePolicy().GetImageRelativePath(new Uri(url)));
   }



   /// <summary>Supported local document types retain once-decoded literal paths, without providing shell commands.</summary>
   [Theory]
   [InlineData("child.md", "child.md")]
   [InlineData("docs/child.MARKDOWN", "docs/child.MARKDOWN")]
   [InlineData("child.mdown", "child.mdown")]
   [InlineData("child.mkd", "child.mkd")]
   [InlineData("child.txt", "child.txt")]
   [InlineData("docs/caf%C3%A9%20%26%20%24HOME.md", "docs/café & $HOME.md")]
   [InlineData("docs/%252e%252e.md", "docs/%2e%2e.md")]
   public void GetDocumentRelativePath_SupportedType_DecodesOnlyOnce(string path, string expected)
   {
      var policy = new DocumentResourcePolicy();
      Assert.Equal(expected, policy.GetDocumentRelativePath(new Uri(policy.DocumentUri, path)));
   }



   /// <summary>Documents reject queries, traversal, devices, alternate streams and executable/non-document targets before native access.</summary>
   [Theory]
   [InlineData("child.md?command=run")]
   [InlineData("../private.md")]
   [InlineData("docs/%2e%2e%2f%2e%2e%2fprivate.md")]
   [InlineData("docs/CON.md")]
   [InlineData("docs/child.md%3aprivate")]
   [InlineData("docs/%5cprivate.md")]
   [InlineData("docs/%00private.md")]
   [InlineData("docs/folder./child.md")]
   [InlineData("docs/folder%20/child.md")]
   [InlineData("child.exe")]
   [InlineData("child.lnk")]
   [InlineData("child.png")]
   public void GetDocumentRelativePath_UnsafeTarget_DeniesAccess(string path)
   {
      var policy = new DocumentResourcePolicy();
      Assert.Null(policy.GetDocumentRelativePath(new Uri(policy.DocumentUri, path)));
   }



   /// <summary>Other namespaces, origins, schemes and relative requests never gain document-open authority.</summary>
   [Fact]
   public void GetDocumentRelativePath_ForeignContext_DeniesAccess()
   {
      var policy = new DocumentResourcePolicy();
      Assert.Null(policy.GetDocumentRelativePath(new Uri("child.md", UriKind.Relative)));
      Assert.Null(policy.GetDocumentRelativePath(new Uri(new DocumentResourcePolicy().DocumentUri, "child.md")));
      Assert.Null(policy.GetDocumentRelativePath(new Uri("file:///C:/private.md")));
      Assert.Null(policy.GetDocumentRelativePath(new Uri("https://example.com/child.md")));
      Assert.Throws<ArgumentNullException>(() => policy.GetDocumentRelativePath(null!));
   }



   /// <summary>Requests cannot cross into another opened document.</summary>
   [Fact]
   public void GetImageRelativePath_OtherDocument_DeniesAccess()
   {
      var policy = new DocumentResourcePolicy();
      var other = new DocumentResourcePolicy();
      Assert.Null(policy.GetImageRelativePath(new Uri(other.DocumentUri, "image.png")));
   }



   /// <summary>Non-images, traversal, device names, encoded separators and filename aliases are refused.</summary>
   [Theory]
   [InlineData("index.html")]
   [InlineData("script.js")]
   [InlineData("../private.png")]
   [InlineData("%2e%2e/private.png")]
   [InlineData("images/%2e%2e%2f%2e%2e%2fprivate.png")]
   [InlineData("images/%5cprivate.png")]
   [InlineData("images/C%3a/private.png")]
   [InlineData("images/%00private.png")]
   [InlineData("images/%0aprivate.png")]
   [InlineData("images/a%3f.png")]
   [InlineData("images/a%2a.png")]
   [InlineData("images/a%3c.png")]
   [InlineData("images/a%7c.png")]
   [InlineData("images//private.png")]
   [InlineData("images/folder%20/private.png")]
   [InlineData("images/folder./private.png")]
   [InlineData("CON.png")]
   [InlineData("images/LPT1.png")]
   [InlineData("images/COM%C2%B9.png")]
   public void GetImageRelativePath_UnsafePath_DeniesAccess(string path)
   {
      var policy = new DocumentResourcePolicy();
      Assert.Null(policy.GetImageRelativePath(new Uri(policy.DocumentUri, path)));
   }



   /// <summary>Relative request URIs and absent requests have explicit refusal behavior.</summary>
   [Fact]
   public void GetImageRelativePath_RelativeAndNull_RejectsInvalidRequests()
   {
      var policy = new DocumentResourcePolicy();
      Assert.Null(policy.GetImageRelativePath(new Uri("image.png", UriKind.Relative)));
      Assert.Throws<ArgumentNullException>(() => policy.GetImageRelativePath(null!));
   }
}
