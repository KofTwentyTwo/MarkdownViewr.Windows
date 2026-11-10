/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Documents;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>The written-out Windows name rules, and the two places that depend on them.</summary>
public sealed class WindowsFileNamesTests
{
   private static readonly string[] s_devices =
   [
      "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$",
      "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM¹", "COM²", "COM³",
      "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "LPT¹", "LPT²", "LPT³",
   ];



   /// <summary>A device is a device in any letter case, with any extension, and with spaces before the extension or at the end.</summary>
   [Fact]
   public void IsReservedDeviceName_EveryDeviceSpelling_IsReserved()
   {
      Assert.All(Spellings(), spelling =>
      {
         Assert.True(WindowsFileNames.IsReservedDeviceName(spelling), spelling);
         Assert.True(WindowsFileNames.IsReservedDeviceName(spelling + ".png"), spelling);
         Assert.True(WindowsFileNames.IsReservedDeviceName(spelling + ".tar.md"), spelling);
         Assert.True(WindowsFileNames.IsReservedDeviceName(spelling + " .png"), spelling);
         Assert.True(WindowsFileNames.IsReservedDeviceName(spelling + "  "), spelling);
      });
      Assert.Equal(96, Spellings().Count());
   }



   /// <summary>Names that only resemble a device stay ordinary file names; only spaces are ignored before the extension.</summary>
   [Theory]
   [InlineData("")]
   [InlineData("console.md")]
   [InlineData("COM10.png")]
   [InlineData("COM")]
   [InlineData("LPT.txt")]
   [InlineData("xCON.png")]
   [InlineData("CON-notes.md")]
   [InlineData("CON-.md")]
   [InlineData("null.md")]
   [InlineData(" NUL.png")]
   [InlineData("NUL .png")]
   [InlineData("NUL\t.png")]
   [InlineData("readme.NUL")]
   [InlineData("COM⁴.png")]
   [InlineData("CLOCK$")]
   [InlineData("CLOCK$.txt")]
   public void IsReservedDeviceName_OrdinaryName_IsNotReserved(string name) => Assert.False(WindowsFileNames.IsReservedDeviceName(name));



   /// <summary>A missing name is a contract error, not an ordinary file.</summary>
   [Fact]
   public void IsReservedDeviceName_Null_RejectsInput() => Assert.Throws<ArgumentNullException>(() => WindowsFileNames.IsReservedDeviceName(null!));



   /// <summary>The printable characters Windows forbids in a name; control characters are refused separately by each caller.</summary>
   [Fact]
   public void InvalidCharacters_Constant_ListsThePrintableForbiddenCharacters() => Assert.Equal("\"*/:<>?\\|", new string(WindowsFileNames.InvalidCharacters.Order().ToArray()));



   /// <summary>No image or linked-document request can name a device in any spelling, as the file or as a folder on the way to it.</summary>
   [Fact]
   public void GetRelativePath_DeviceSegment_IsDenied()
   {
      var policy = new DocumentResourcePolicy();
      Assert.All(Spellings(), spelling =>
      {
         string segment = Uri.EscapeDataString(spelling);
         Assert.Null(policy.GetImageRelativePath(new Uri(policy.DocumentUri, segment + ".png")));
         Assert.Null(policy.GetImageRelativePath(new Uri(policy.DocumentUri, segment + "%20.png")));
         Assert.Null(policy.GetImageRelativePath(new Uri(policy.DocumentUri, segment + "/picture.png")));
         Assert.Null(policy.GetDocumentRelativePath(new Uri(policy.DocumentUri, segment + ".md")));
         Assert.Null(policy.GetDocumentRelativePath(new Uri(policy.DocumentUri, segment + ".tar.md")));
         Assert.Null(policy.GetDocumentRelativePath(new Uri(policy.DocumentUri, "docs/" + segment + "/child.md")));
      });
      Assert.Equal("console.png", policy.GetImageRelativePath(new Uri(policy.DocumentUri, "console.png")));
      Assert.Equal("docs/com10/child.md", policy.GetDocumentRelativePath(new Uri(policy.DocumentUri, "docs/com10/child.md")));
   }



   /// <summary>A theme named after a device, in any case, never becomes the suggested export file name.</summary>
   [Fact]
   public void SuggestedFileName_EveryDeviceName_IsPrefixed()
   {
      Assert.All(s_devices, device =>
      {
         Assert.Equal("theme-" + device.ToLowerInvariant() + ".json", ThemeJson.SuggestedFileName(ThemeCatalog.Find(null).Rename(device)));
         Assert.Equal("theme-" + device.ToLowerInvariant() + ".json", ThemeJson.SuggestedFileName(ThemeCatalog.Find(null).Rename(device.ToLowerInvariant())));
         Assert.Equal("theme-" + device.ToLowerInvariant() + ".backup.json", ThemeJson.SuggestedFileName(ThemeCatalog.Find(null).Rename(device + ".backup")));
      });
   }



   /// <summary>Leading and trailing dots and hyphens are dropped from a suggested name, and a name made only of them falls back to a plain one.</summary>
   [Theory]
   [InlineData("-Edge-", "edge.json")]
   [InlineData(".Dotted.", "dotted.json")]
   [InlineData("--", "theme.json")]
   [InlineData(" spaced ", "spaced.json")]
   [InlineData("-con-", "theme-con.json")]
   public void SuggestedFileName_LeadingAndTrailingSeparators_AreTrimmed(string name, string expected)
      => Assert.Equal(expected, ThemeJson.SuggestedFileName(ThemeCatalog.Find(null).Rename(name)));



   /// <summary>Each device name in upper, lower and mixed case.</summary>
   private static IEnumerable<string> Spellings()
      => s_devices.SelectMany(device => new[] { device, device.ToLowerInvariant(), char.ToUpperInvariant(device[0]) + device[1..].ToLowerInvariant() });
}
