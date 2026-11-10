/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Activation;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>What one launch may ask the running reader to open, and which launches share a reader.</summary>
public sealed class ActivationRequestTests
{
   /// <summary>Relative arguments resolve against the launching folder; shell characters stay literal and duplicate paths collapse without regard to case.</summary>
   [Fact]
   public void Create_RelativeUnicodeAndDuplicates_UsesSenderDirectory()
   {
      string directory = Path.Combine(Path.GetTempPath(), "sender space");
      var request = ActivationRequest.Create(["notes & café $HOME.md", "./notes & café $HOME.md", "SECOND.MD", "second.md"], directory);
      Assert.Equal(new[] { Path.Combine(directory, "notes & café $HOME.md"), Path.Combine(directory, "SECOND.MD") }, request.Paths);
      Assert.False(request.HasInvalidPath);
      Assert.Empty(ActivationRequest.Create([], directory).Paths);
      Assert.False(ActivationRequest.Create([], directory).HasInvalidPath);
   }



   /// <summary>An argument that cannot name a document is dropped and flagged; the documents beside it still open.</summary>
   [Theory]
   [InlineData("")]
   [InlineData("   ")]
   [InlineData("bad\0name.md")]
   [InlineData("line\nbreak.md")]
   public void Create_UnusableArgumentBesideUsableOnes_KeepsTheUsableOnesAndFlagsTheRest(string unusable)
   {
      string directory = Path.Combine(Path.GetTempPath(), "sender");
      var request = ActivationRequest.Create(["first.md", unusable, "second.md"], directory);
      Assert.Equal(new[] { Path.Combine(directory, "first.md"), Path.Combine(directory, "second.md") }, request.Paths);
      Assert.True(request.HasInvalidPath);
      request.Validate();
   }



   /// <summary>A launch may name at most the bounded number of documents; one more is refused outright.</summary>
   [Fact]
   public void Create_PathCountAtAndPastTheBound_IsBoundedExactly()
   {
      string directory = Path.GetTempPath();
      IEnumerable<string> names = Enumerable.Range(0, ActivationRequest.MaximumPaths + 1).Select(index => $"document{index}.md");
      Assert.Equal(ActivationRequest.MaximumPaths, ActivationRequest.Create(names.Take(ActivationRequest.MaximumPaths), directory).Paths.Length);
      Assert.Throws<ArgumentException>(() => ActivationRequest.Create(names, directory));
      Assert.Throws<ArgumentNullException>(() => ActivationRequest.Create(null!, directory));
      Assert.Equal(128, ActivationRequest.MaximumPaths);
   }



   /// <summary>A request that did not come from Create is refused: relative, empty, control-bearing, overlong or too many paths.</summary>
   [Fact]
   public void Validate_InvalidPaths_RejectsMalformedAndUnboundedRequests()
   {
      Assert.Throws<InvalidDataException>(() => new ActivationRequest(null!).Validate());
      Assert.Throws<InvalidDataException>(() => new ActivationRequest(["relative.md"]).Validate());
      Assert.Throws<InvalidDataException>(() => new ActivationRequest([""]).Validate());
      Assert.Throws<InvalidDataException>(() => new ActivationRequest([null!]).Validate());
      Assert.Throws<InvalidDataException>(() => new ActivationRequest(["C:\\bad\nfile.md"]).Validate());
      Assert.Throws<InvalidDataException>(() => new ActivationRequest(Enumerable.Repeat(@"C:\one.md", ActivationRequest.MaximumPaths + 1).ToArray()).Validate());
      new ActivationRequest(Enumerable.Repeat(@"C:\one.md", ActivationRequest.MaximumPaths).ToArray()).Validate();
   }



   /// <summary>A path is accepted up to the Windows limit and refused one character past it.</summary>
   [Fact]
   public void Validate_PathAtAndPastTheWindowsLimit_IsBoundedExactly()
   {
      string atLimit = @"C:\" + new string('a', 32767 - 3);
      Assert.Equal(32767, atLimit.Length);
      new ActivationRequest([atLimit]).Validate();
      Assert.Throws<InvalidDataException>(() => new ActivationRequest([atLimit + "a"]).Validate());
   }



   /// <summary>
   /// One reader serves one executable folder and one data folder. Spelling does not matter:
   /// case and a trailing separator name the same folders. Any other folder is another reader,
   /// even one whose name has the same length.
   /// </summary>
   [Fact]
   public void InstanceKey_ProfilesAndInstallations_IsolatesBoundaries()
   {
      string root = Path.Combine(Path.GetTempPath(), "viewer");
      string data = Path.Combine(Path.GetTempPath(), "profile");
      string key = ActivationRequest.InstanceKey(root, data);
      Assert.Equal(key, ActivationRequest.InstanceKey(root + Path.DirectorySeparatorChar, data.ToUpperInvariant()));
      Assert.Equal(key, ActivationRequest.InstanceKey(root.ToUpperInvariant(), data + Path.DirectorySeparatorChar));
      Assert.NotEqual(key, ActivationRequest.InstanceKey(root + "-dev", data), StringComparer.Ordinal);
      Assert.NotEqual(key, ActivationRequest.InstanceKey(root, data + "-review"), StringComparer.Ordinal);
      Assert.NotEqual(key, ActivationRequest.InstanceKey(root, Path.Combine(Path.GetTempPath(), "profilf")), StringComparer.Ordinal);
      Assert.NotEqual(key, ActivationRequest.InstanceKey(data, root), StringComparer.Ordinal);
      Assert.Matches("^k22-markdownviewr-[0-9A-F]{64}$", key);
   }
}
