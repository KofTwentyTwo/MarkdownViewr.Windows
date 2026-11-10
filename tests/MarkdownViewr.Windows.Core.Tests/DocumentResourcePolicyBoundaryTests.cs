/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>
/// Each resource-policy check is exercised alone: a request that differs from a valid one in a
/// single component must be denied by that component's check, not by an unrelated one.
/// </summary>
public sealed class DocumentResourcePolicyBoundaryTests
{
   /// <summary>The valid baseline every single-component variation below is derived from.</summary>
   [Fact]
   public void Baseline_ValidRequests_AreAccepted()
   {
      var policy = new DocumentResourcePolicy();
      Assert.Equal("image.png", policy.GetImageRelativePath(Vary(policy, "image.png", _ => { })));
      Assert.Equal("child.md", policy.GetDocumentRelativePath(Vary(policy, "child.md", _ => { })));
   }



   /// <summary>Plain HTTP on the same host, port and path is not the document's origin.</summary>
   [Fact]
   public void OtherScheme_SameHostPortAndPath_IsDenied()
   {
      var policy = new DocumentResourcePolicy();
      Uri image = Vary(policy, "image.png", builder =>
      {
         builder.Scheme = "http";
         builder.Port = 443;
      });
      Assert.Equal(443, image.Port);
      Assert.Null(policy.GetImageRelativePath(image));
      Assert.Null(policy.GetDocumentRelativePath(Vary(policy, "child.md", builder =>
      {
         builder.Scheme = "http";
         builder.Port = 443;
      })));
   }



   /// <summary>Another port on the same host is another origin.</summary>
   [Fact]
   public void OtherPort_SameSchemeHostAndPath_IsDenied()
   {
      var policy = new DocumentResourcePolicy();
      Assert.Null(policy.GetImageRelativePath(Vary(policy, "image.png", builder => builder.Port = 444)));
      Assert.Null(policy.GetDocumentRelativePath(Vary(policy, "child.md", builder => builder.Port = 8443)));
   }



   /// <summary>Credentials in the request never reach a resource.</summary>
   [Fact]
   public void UserInformation_SameOriginAndPath_IsDenied()
   {
      var policy = new DocumentResourcePolicy();
      Assert.Null(policy.GetImageRelativePath(Vary(policy, "image.png", builder => builder.UserName = "user")));
      Assert.Null(policy.GetDocumentRelativePath(Vary(policy, "child.md", builder =>
      {
         builder.UserName = "user";
         builder.Password = "secret";
      })));
   }



   /// <summary>Another host, or the same host in another letter case position such as a subdomain, is denied.</summary>
   [Theory]
   [InlineData("example.com")]
   [InlineData("sub.viewer.markdownviewr.invalid")]
   [InlineData("viewer.markdownviewr.invalid.example.com")]
   public void OtherHost_SameSchemePortAndPath_IsDenied(string host)
   {
      var policy = new DocumentResourcePolicy();
      Assert.Null(policy.GetImageRelativePath(Vary(policy, "image.png", builder => builder.Host = host)));
      Assert.Null(policy.GetDocumentRelativePath(Vary(policy, "child.md", builder => builder.Host = host)));
   }



   /// <summary>Exactly these image types are served; documents, pages, scripts and executables are not images.</summary>
   [Theory]
   [InlineData(".png", true)]
   [InlineData(".jpg", true)]
   [InlineData(".jpeg", true)]
   [InlineData(".gif", true)]
   [InlineData(".bmp", true)]
   [InlineData(".webp", true)]
   [InlineData(".svg", true)]
   [InlineData(".avif", true)]
   [InlineData(".ico", true)]
   [InlineData(".PNG", true)]
   [InlineData(".md", false)]
   [InlineData(".markdown", false)]
   [InlineData(".txt", false)]
   [InlineData(".html", false)]
   [InlineData(".htm", false)]
   [InlineData(".js", false)]
   [InlineData(".css", false)]
   [InlineData(".json", false)]
   [InlineData(".pdf", false)]
   [InlineData(".tiff", false)]
   [InlineData(".exe", false)]
   [InlineData(".lnk", false)]
   [InlineData(".url", false)]
   [InlineData("", false)]
   public void GetImageRelativePath_Extension_ServesOnlyImageTypes(string extension, bool served)
   {
      var policy = new DocumentResourcePolicy();
      Assert.Equal(served ? "file" + extension : null, policy.GetImageRelativePath(new Uri(policy.DocumentUri, "file" + extension)));
   }



   /// <summary>Exactly these document types open in a new reader; pages, images, scripts and executables do not.</summary>
   [Theory]
   [InlineData(".md", true)]
   [InlineData(".markdown", true)]
   [InlineData(".mdown", true)]
   [InlineData(".mkd", true)]
   [InlineData(".txt", true)]
   [InlineData(".MD", true)]
   [InlineData(".html", false)]
   [InlineData(".htm", false)]
   [InlineData(".png", false)]
   [InlineData(".svg", false)]
   [InlineData(".json", false)]
   [InlineData(".pdf", false)]
   [InlineData(".exe", false)]
   [InlineData(".bat", false)]
   [InlineData(".cmd", false)]
   [InlineData(".ps1", false)]
   [InlineData(".lnk", false)]
   [InlineData(".url", false)]
   [InlineData(".mdx", false)]
   [InlineData("", false)]
   public void GetDocumentRelativePath_Extension_OpensOnlyDocumentTypes(string extension, bool opened)
   {
      var policy = new DocumentResourcePolicy();
      Assert.Equal(opened ? "file" + extension : null, policy.GetDocumentRelativePath(new Uri(policy.DocumentUri, "file" + extension)));
   }



   /// <summary>Each character Windows forbids in a name is refused on its own, for images and documents.</summary>
   [Theory]
   [InlineData("%3C")]
   [InlineData("%3E")]
   [InlineData("%3A")]
   [InlineData("%22")]
   [InlineData("%5C")]
   [InlineData("%7C")]
   [InlineData("%3F")]
   [InlineData("%2A")]
   public void ForbiddenCharacter_InAnySegment_IsDenied(string encoded)
   {
      var policy = new DocumentResourcePolicy();
      Assert.Null(policy.GetImageRelativePath(new Uri(policy.DocumentUri, "images/a" + encoded + "b.png")));
      Assert.Null(policy.GetImageRelativePath(new Uri(policy.DocumentUri, "im" + encoded + "ages/ab.png")));
      Assert.Null(policy.GetDocumentRelativePath(new Uri(policy.DocumentUri, "docs/a" + encoded + "b.md")));
   }



   /// <summary>The generated heading fragment is matched exactly; another letter case is not a heading target.</summary>
   [Theory]
   [InlineData("#MV-HEADING-0")]
   [InlineData("#Mv-Heading-0")]
   [InlineData("#mv-heading-0x")]
   [InlineData("#mv-heading-00a")]
   [InlineData("#mv-heading- 0")]
   public void GetHeadingIndex_OtherSpelling_IsNotAHeadingTarget(string fragment)
   {
      var policy = new DocumentResourcePolicy();
      Assert.Null(policy.GetHeadingIndex(new Uri(policy.DocumentUri.AbsoluteUri + fragment), 3));
   }



   /// <summary>Builds this document's own resource request and changes exactly one component of it.</summary>
   private static Uri Vary(DocumentResourcePolicy policy, string file, Action<UriBuilder> change)
   {
      var builder = new UriBuilder(new Uri(policy.DocumentUri, file));
      change(builder);
      return builder.Uri;
   }
}
