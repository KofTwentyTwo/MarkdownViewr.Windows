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



   /// <summary>Every device name, in each letter case and with the suffixes Windows ignores.</summary>
   public static TheoryData<string> DeviceSpellings()
   {
      var data = new TheoryData<string>();
      foreach(string device in s_devices)
      {
         string mixed = char.ToUpperInvariant(device[0]) + device[1..].ToLowerInvariant();
         foreach(string spelling in new[] { device, device.ToLowerInvariant(), mixed })
         {
            data.Add(spelling);
            data.Add(spelling + ".png");
            data.Add(spelling + ".tar.md");
            data.Add(spelling + " .png");
            data.Add(spelling + "  ");
         }
      }
      return data;
   }



   /// <summary>A device is a device whatever its case, extension, or trailing spaces before the extension.</summary>
   [Theory]
   [MemberData(nameof(DeviceSpellings))]
   public void IsReservedDeviceName_DeviceSpelling_IsReserved(string name) => Assert.True(WindowsFileNames.IsReservedDeviceName(name));



   /// <summary>Names that only resemble a device stay ordinary file names.</summary>
   [Theory]
   [InlineData("")]
   [InlineData("console.md")]
   [InlineData("COM10.png")]
   [InlineData("COM")]
   [InlineData("LPT.txt")]
   [InlineData("xCON.png")]
   [InlineData("CON-notes.md")]
   [InlineData("null.md")]
   [InlineData(" NUL.png")]
   [InlineData("readme.NUL")]
   [InlineData("COM⁴.png")]
   public void IsReservedDeviceName_OrdinaryName_IsNotReserved(string name) => Assert.False(WindowsFileNames.IsReservedDeviceName(name));



   /// <summary>A missing name is a contract error, not an ordinary file.</summary>
   [Fact]
   public void IsReservedDeviceName_Null_RejectsInput() => Assert.Throws<ArgumentNullException>(() => WindowsFileNames.IsReservedDeviceName(null!));



   /// <summary>The characters Windows forbids in a name are exactly these nine.</summary>
   [Fact]
   public void InvalidCharacters_ListsTheNineForbiddenCharacters() => Assert.Equal("\"*/:<>?\\|", new string(WindowsFileNames.InvalidCharacters.Order().ToArray()));



   /// <summary>No image or linked-document request can name a device in any spelling, at any depth.</summary>
   [Theory]
   [MemberData(nameof(DeviceSpellings))]
   public void ResourcePolicy_DeviceSegment_DeniesImageAndDocument(string name)
   {
      var policy = new DocumentResourcePolicy();
      string segment = Uri.EscapeDataString(name.TrimEnd(' '));
      Assert.Null(policy.GetImageRelativePath(new Uri(policy.DocumentUri, segment + (name.Contains('.', StringComparison.Ordinal) ? "" : ".png"))));
      Assert.Null(policy.GetImageRelativePath(new Uri(policy.DocumentUri, segment + "/picture.png")));
      Assert.Null(policy.GetDocumentRelativePath(new Uri(policy.DocumentUri, segment + (name.Contains('.', StringComparison.Ordinal) ? "" : ".md"))));
      Assert.Null(policy.GetDocumentRelativePath(new Uri(policy.DocumentUri, "docs/" + segment + "/child.md")));
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
}
