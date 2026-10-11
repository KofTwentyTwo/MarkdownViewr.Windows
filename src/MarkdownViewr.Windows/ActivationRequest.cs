/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Security.Cryptography;
using System.Text;


namespace MarkdownViewr.Windows;

/// <summary>Only bounded literal absolute document paths cross the current-user activation pipe; no native commands are accepted.</summary>
internal sealed record ActivationRequest(string[] Paths, bool HasInvalidPath = false)
{
   internal const int MaximumPaths = 128;
   internal const int MaximumPayloadBytes = 1024 * 1024;



   /// <summary>Canonicalizes at the sender, preserving relative-path meaning even when the existing reader has a different working directory.</summary>
   internal static ActivationRequest Create(IEnumerable<string> paths, string workingDirectory)
   {
      string[] candidates = paths.Take(MaximumPaths + 1).ToArray();
      if(candidates.Length > MaximumPaths)
      {
         throw new ArgumentException("Too many document paths.", nameof(paths));
      }
      var request = new ActivationRequest(candidates.Select(path => Path.GetFullPath(path, workingDirectory)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
      request.Validate();
      return request;
   }



   /// <summary>Malformed, oversized or relative pipe input cannot become an unbounded launch or be interpreted using the receiver's directory.</summary>
   internal void Validate()
   {
      if(Paths is null || Paths.Length > MaximumPaths || Paths.Any(path => string.IsNullOrEmpty(path) || path.Length > 32767 || !Path.IsPathFullyQualified(path) || path.Any(char.IsControl)))
      {
         throw new InvalidDataException("Invalid document activation paths.");
      }
   }



   /// <summary>Profile and executable directories define distinct stable/dev, development-build and review/test instance identities.</summary>
   internal static string InstanceKey(string executableDirectory, string dataDirectory)
   {
      string identity = Path.TrimEndingDirectorySeparator(Path.GetFullPath(executableDirectory)).ToUpperInvariant() + "\0" + Path.TrimEndingDirectorySeparator(Path.GetFullPath(dataDirectory)).ToUpperInvariant();
      return "k22-markdownviewr-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
   }
}
