/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Bounds passive image requests and user-initiated native document links to one opened document namespace.</summary>
public sealed class DocumentResourcePolicy
{
   private static readonly HashSet<string> s_imageExtensions = new(StringComparer.OrdinalIgnoreCase)
   {
      ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".svg", ".avif", ".ico",
   };

   private static readonly HashSet<string> s_documentExtensions = new(StringComparer.OrdinalIgnoreCase)
   {
      ".md", ".markdown", ".mdown", ".mkd", ".txt",
   };

   private static readonly HashSet<string> s_deviceNames = new(StringComparer.OrdinalIgnoreCase)
   {
      "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
      "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "COM¹", "COM²", "COM³", "LPT¹", "LPT²", "LPT³",
   };

   private readonly string _prefix;



   /// <summary>Creates an isolated logical resource namespace for this document; no HTTP server is started.</summary>
   public DocumentResourcePolicy()
   {
      _prefix = $"/documents/{Guid.NewGuid():N}/";
      DocumentUri = new Uri("https://viewer.markdownviewr.invalid" + _prefix + "index.html");
   }



   /// <summary>The only top-level document URI the browser host may serve.</summary>
   public Uri DocumentUri { get; }



   /// <summary>Allows only an in-range generated heading fragment on this exact document, without query/native arguments.</summary>
   public int? GetHeadingIndex(Uri request, int headingCount)
   {
      ArgumentNullException.ThrowIfNull(request);
      if(!request.IsAbsoluteUri || request.Query.Length != 0 || !string.Equals(request.GetLeftPart(UriPartial.Path), DocumentUri.AbsoluteUri, StringComparison.Ordinal))
      {
         return null;
      }
      return request.Fragment.StartsWith("#mv-heading-", StringComparison.Ordinal) &&
         int.TryParse(request.Fragment.AsSpan(12), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int index) && index < headingCount ? index : null;
   }


   /// <summary>Returns a decoded relative image path, or denies the request before filesystem access.</summary>
   public string? GetImageRelativePath(Uri request) => GetRelativePath(request, s_imageExtensions);



   /// <summary>A deliberate local document link may read a supported type, never another scheme, query or native command.</summary>
   public string? GetDocumentRelativePath(Uri request)
   {
      ArgumentNullException.ThrowIfNull(request);
      return request.IsAbsoluteUri && request.Query.Length == 0 ? GetRelativePath(request, s_documentExtensions) : null;
   }



   /// <summary>Shares once-only decoding and segment containment between images and native document-link requests.</summary>
   private string? GetRelativePath(Uri request, HashSet<string> extensions)
   {
      ArgumentNullException.ThrowIfNull(request);
      if(!request.IsAbsoluteUri || !string.Equals(request.Scheme, "https", StringComparison.Ordinal) || !string.Equals(request.Host, DocumentUri.Host, StringComparison.Ordinal) || request.Port != 443 ||
         request.UserInfo.Length != 0 || !request.AbsolutePath.StartsWith(_prefix, StringComparison.Ordinal))
      {
         return null;
      }
      string relative = Uri.UnescapeDataString(request.AbsolutePath[_prefix.Length..]);
      if(!extensions.Contains(Path.GetExtension(relative)) || relative.Split('/').Any(segment => !IsSafeSegment(segment)))
      {
         return null;
      }
      return relative;
   }



   /// <summary>Rejects traversal, device names, separators, controls and Windows filename aliases.</summary>
   private static bool IsSafeSegment(string segment)
   {
      return segment.Length > 0 && segment is not ("." or "..") && !segment.EndsWith(' ') && !segment.EndsWith('.') &&
         !segment.Any(character => char.IsControl(character) || "<>:\\|?*".Contains(character, StringComparison.Ordinal)) &&
         !s_deviceNames.Contains(segment.Split('.')[0]);
   }
}
