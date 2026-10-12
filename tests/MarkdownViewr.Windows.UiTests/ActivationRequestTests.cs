/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Sender-relative paths, Unicode, duplicate launches and stable/dev/profile isolation retain their actual meanings.</summary>
public sealed class ActivationRequestTests
{
   /// <summary>Canonicalization belongs to the sender, including literal shell characters and case-insensitive duplicate paths.</summary>
   [Fact]
   public void Create_RelativeUnicodeAndDuplicates_UsesSenderDirectory()
   {
      string directory = Path.Combine(Path.GetTempPath(), "sender space");
      var request = ActivationRequest.Create(["notes & café $HOME.md", "./notes & café $HOME.md", "SECOND.MD", "second.md"], directory);
      Assert.Equal(new[] { Path.Combine(directory, "notes & café $HOME.md"), Path.Combine(directory, "SECOND.MD") }, request.Paths);
      Assert.False(request.HasInvalidPath);
      Assert.Empty(ActivationRequest.Create([], directory).Paths);
   }



   /// <summary>Malformed messages cannot open relative paths or allocate an unbounded number of document windows.</summary>
   [Fact]
   public void Validate_InvalidPaths_RejectsMalformedAndUnboundedRequests()
   {
      Assert.Throws<InvalidDataException>(() => new ActivationRequest(null!).Validate());
      Assert.Throws<InvalidDataException>(() => new ActivationRequest(["relative.md"]).Validate());
      Assert.Throws<InvalidDataException>(() => new ActivationRequest([""]).Validate());
      Assert.Throws<InvalidDataException>(() => new ActivationRequest(["C:\\bad\nfile.md"]).Validate());
      Assert.Throws<InvalidDataException>(() => new ActivationRequest(["C:\\" + new string('a', 32768)]).Validate());
      Assert.Throws<ArgumentException>(() => ActivationRequest.Create(Enumerable.Repeat("one.md", ActivationRequest.MaximumPaths + 1), Path.GetTempPath()));
   }



   /// <summary>Equivalent directory spellings share an instance, while distinct review profiles and channel installation roots do not.</summary>
   [Fact]
   public void InstanceKey_ProfilesAndInstallations_IsolatesBoundaries()
   {
      string root = Path.Combine(Path.GetTempPath(), "viewer");
      string data = Path.Combine(Path.GetTempPath(), "profile");
      string key = ActivationRequest.InstanceKey(root, data);
      Assert.Equal(key, ActivationRequest.InstanceKey(root + Path.DirectorySeparatorChar, data.ToUpperInvariant()));
      Assert.NotEqual(key, ActivationRequest.InstanceKey(root + "-dev", data), StringComparer.Ordinal);
      Assert.NotEqual(key, ActivationRequest.InstanceKey(root, data + "-review"), StringComparer.Ordinal);
      Assert.StartsWith("k22-markdownviewr-", key, StringComparison.Ordinal);
      Assert.DoesNotContain(root, key, StringComparison.OrdinalIgnoreCase);
   }
}
