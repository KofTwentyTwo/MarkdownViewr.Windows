/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.Win32;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Real isolated registry readbacks verify quoting, default preservation, update ownership and channel-safe uninstall.</summary>
public sealed class FileTypeRegistrationTests
{
   /// <summary>Each channel is discoverable and removing stable leaves dev, other viewers and the user's chosen defaults unchanged.</summary>
   [Fact]
   public void Register_ChannelsAndUninstall_PreservesDefaultAndSibling()
   {
      using var fixture = new RegistryFixture();
      using RegistryKey extension = fixture.Root.CreateSubKey("Software\\Classes\\.md");
      extension.SetValue("", "Other.Markdown");
      using RegistryKey other = extension.CreateSubKey("OpenWithProgids");
      other.SetValue("Other.Markdown", "");
      using RegistryKey choice = fixture.Root.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\.md\\UserChoice");
      choice.SetValue("ProgId", "Other.Markdown");
      choice.SetValue("Hash", "unchanged-user-hash");
      string exe = Path.Combine(Path.GetTempPath(), "viewer spaces & café", "current", "MarkdownViewr.Windows.exe");
      var stable = new FileTypeRegistration(fixture.Root, exe, "stable");
      var dev = new FileTypeRegistration(fixture.Root, exe, "dev");
      Assert.NotEqual(stable.ProgId, dev.ProgId, StringComparer.Ordinal);
      stable.Register();
      dev.Register();
      AssertRegistered(fixture.Root, stable.ProgId, exe);
      AssertRegistered(fixture.Root, dev.ProgId, exe);
      stable.Register();
      stable.Unregister();
      stable.Unregister();
      Assert.Null(fixture.Root.OpenSubKey("Software\\Classes\\" + stable.ProgId));
      AssertRegistered(fixture.Root, dev.ProgId, exe);
      Assert.Equal("Other.Markdown", extension.GetValue(""));
      Assert.Equal("Other.Markdown", choice.GetValue("ProgId"));
      Assert.Equal("unchanged-user-hash", choice.GetValue("Hash"));
      Assert.Contains(dev.ProgId, other.GetValueNames(), StringComparer.Ordinal);
      Assert.Contains("Other.Markdown", other.GetValueNames(), StringComparer.Ordinal);
      Assert.DoesNotContain(stable.ProgId, other.GetValueNames(), StringComparer.Ordinal);
      dev.Unregister();
      Assert.Equal(new[] { "Other.Markdown" }, other.GetValueNames());
   }



   /// <summary>Advertising an installation is not choosing it; an unavailable query and a retargeted command cannot report a selected default.</summary>
   [Fact]
   public void DefaultMarkdownStatus_EffectiveSelection_RequiresExactRegisteredOwner()
   {
      using var fixture = new RegistryFixture();
      var registration = new FileTypeRegistration(fixture.Root, Path.Combine(Path.GetTempPath(), "current", "MarkdownViewr.Windows.exe"), "stable");
      Assert.False(registration.IsDefaultMarkdownViewer(registration.ProgId));
      registration.Register();
      Assert.False(registration.IsDefaultMarkdownViewer("Other.Markdown"));
      Assert.False(registration.IsDefaultMarkdownViewer(""));
      Assert.Null(registration.IsDefaultMarkdownViewer(null));
      Assert.True(registration.IsDefaultMarkdownViewer(registration.ProgId));
      Assert.True(registration.IsDefaultMarkdownViewer(registration.ProgId.ToLowerInvariant()));
      var dev = new FileTypeRegistration(fixture.Root, Path.Combine(Path.GetTempPath(), "current", "MarkdownViewr.Windows.exe"), "dev");
      dev.Register();
      Assert.False(registration.IsDefaultMarkdownViewer(dev.ProgId));
      using RegistryKey command = fixture.Root.OpenSubKey("Software\\Classes\\" + registration.ProgId + "\\shell\\open\\command", writable: true)!;
      command.SetValue("", "another deliberately selected command");
      Assert.False(registration.IsDefaultMarkdownViewer(registration.ProgId));
   }



   /// <summary>An external retarget of an owned ProgID is preserved rather than erased on uninstall.</summary>
   [Fact]
   public void Unregister_RetargetedCommand_LeavesRegistrationUntouched()
   {
      using var fixture = new RegistryFixture();
      var registration = new FileTypeRegistration(fixture.Root, Path.Combine(Path.GetTempPath(), "current", "MarkdownViewr.Windows.exe"), "stable");
      registration.Register();
      using RegistryKey command = fixture.Root.OpenSubKey("Software\\Classes\\" + registration.ProgId + "\\shell\\open\\command", writable: true)!;
      command.SetValue("", "another deliberately selected command");
      registration.Unregister();
      Assert.Equal("another deliberately selected command", command.GetValue(""));
      using RegistryKey applications = fixture.Root.OpenSubKey("Software\\RegisteredApplications")!;
      Assert.NotNull(applications.GetValue(registration.ProgId));
   }



   /// <summary>Paths or channel names cannot inject registry keys/command-line quoting; separate installations have separate ownership.</summary>
   [Fact]
   public void Registration_InvalidAndDistinctInstallations_EnforcesIdentityBoundary()
   {
      using var fixture = new RegistryFixture();
      Assert.Throws<ArgumentException>(() => new FileTypeRegistration(fixture.Root, "relative.exe", "stable"));
      Assert.Throws<ArgumentException>(() => new FileTypeRegistration(fixture.Root, "C:\\bad\"path.exe", "stable"));
      Assert.Throws<ArgumentException>(() => new FileTypeRegistration(fixture.Root, "C:\\good.exe", "unknown"));
      var first = new FileTypeRegistration(fixture.Root, Path.Combine(Path.GetTempPath(), "first", "current", "MarkdownViewr.Windows.exe"), "stable");
      var second = new FileTypeRegistration(fixture.Root, Path.Combine(Path.GetTempPath(), "second", "current", "MarkdownViewr.Windows.exe"), "stable");
      Assert.NotEqual(first.ProgId, second.ProgId, StringComparer.Ordinal);
      first.Register();
      second.Register();
      first.Unregister();
      AssertRegistered(fixture.Root, second.ProgId, Path.Combine(Path.GetTempPath(), "second", "current", "MarkdownViewr.Windows.exe"));
   }



   /// <summary>The deep link targets the actual registered value, preserves channel identity and never selects a default itself.</summary>
   [Fact]
   public void DefaultAppsUri_RegisteredInstall_UsesExactRegisteredApplication()
   {
      using var fixture = new RegistryFixture();
      string executable = Path.Combine(Path.GetTempPath(), "viewer spaces & café", "current", "MarkdownViewr.Windows.exe");
      var stable = new FileTypeRegistration(fixture.Root, executable, "stable");
      var dev = new FileTypeRegistration(fixture.Root, executable, "dev");
      Assert.Null(stable.GetDefaultAppsUri(supportsAppPage: true));
      stable.Register();
      dev.Register();
      Uri stableTarget = Assert.IsType<Uri>(stable.GetDefaultAppsUri(supportsAppPage: true));
      Uri devTarget = Assert.IsType<Uri>(dev.GetDefaultAppsUri(supportsAppPage: true));
      Assert.Equal("ms-settings", stableTarget.Scheme);
      Assert.Equal("defaultapps", stableTarget.AbsolutePath);
      Assert.Equal("?registeredAppUser=" + Uri.EscapeDataString(stable.ProgId), stableTarget.Query);
      Assert.Equal("?registeredAppUser=" + Uri.EscapeDataString(dev.ProgId), devTarget.Query);
      Assert.NotEqual(stableTarget, devTarget);
      Assert.Equal(new Uri("ms-settings:defaultapps"), stable.GetDefaultAppsUri(supportsAppPage: false));
      using RegistryKey applications = fixture.Root.OpenSubKey("Software\\RegisteredApplications")!;
      Assert.Equal(2, applications.ValueCount);
      using RegistryKey extension = fixture.Root.OpenSubKey("Software\\Classes\\.md")!;
      Assert.DoesNotContain("", extension.GetValueNames(), StringComparer.Ordinal);
      Assert.Null(fixture.Root.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\.md\\UserChoice"));
      stable.Unregister();
      Assert.Null(stable.GetDefaultAppsUri(supportsAppPage: true));
      Assert.Equal(devTarget, dev.GetDefaultAppsUri(supportsAppPage: true));
   }



   /// <summary>A missing, retargeted or altered capabilities entry must not route the user to another installation's settings.</summary>
   [Fact]
   public void DefaultAppsUri_ChangedOwnership_DeclinesRoute()
   {
      using var fixture = new RegistryFixture();
      var registration = new FileTypeRegistration(fixture.Root, Path.Combine(Path.GetTempPath(), "current", "MarkdownViewr.Windows.exe"), "stable");
      registration.Register();
      using RegistryKey applications = fixture.Root.OpenSubKey("Software\\RegisteredApplications", writable: true)!;
      object expected = applications.GetValue(registration.ProgId)!;
      applications.SetValue(registration.ProgId, "Software\\Other\\Capabilities");
      Assert.Null(registration.GetDefaultAppsUri(supportsAppPage: true));
      applications.DeleteValue(registration.ProgId);
      Assert.Null(registration.GetDefaultAppsUri(supportsAppPage: false));
      applications.SetValue(registration.ProgId, expected);
      using RegistryKey command = fixture.Root.OpenSubKey("Software\\Classes\\" + registration.ProgId + "\\shell\\open\\command", writable: true)!;
      command.SetValue("", "another deliberately selected command");
      Assert.Null(registration.GetDefaultAppsUri(supportsAppPage: true));
      command.DeleteValue("");
      Assert.Null(registration.GetDefaultAppsUri(supportsAppPage: false));
   }



   /// <summary>Readbacks cover the actual command/icon and every advertised type, plus native Default Apps capability discovery.</summary>
   private static void AssertRegistered(RegistryKey root, string identity, string executable)
   {
      using RegistryKey program = root.OpenSubKey("Software\\Classes\\" + identity)!;
      Assert.Equal(RegistryValueKind.None, program.GetValueKind("AllowSilentDefaultTakeOver"));
      Assert.Empty(Assert.IsType<byte[]>(program.GetValue("AllowSilentDefaultTakeOver")));
      using RegistryKey command = program.OpenSubKey("shell\\open\\command")!;
      Assert.Equal("\"" + executable + "\" \"%1\"", command.GetValue(""));
      using RegistryKey icon = program.OpenSubKey("DefaultIcon")!;
      Assert.Equal("\"" + executable + "\",0", icon.GetValue(""));
      using RegistryKey applications = root.OpenSubKey("Software\\RegisteredApplications")!;
      string capabilitiesPath = Assert.IsType<string>(applications.GetValue(identity));
      using RegistryKey capabilities = root.OpenSubKey(capabilitiesPath + "\\FileAssociations")!;
      foreach(string extension in new[] { ".md", ".markdown", ".mdown", ".mkd", ".txt" })
      {
         Assert.Equal(identity, capabilities.GetValue(extension));
         using RegistryKey openWith = root.OpenSubKey("Software\\Classes\\" + extension + "\\OpenWithProgids")!;
         Assert.Contains(identity, openWith.GetValueNames(), StringComparer.Ordinal);
      }
   }
}
