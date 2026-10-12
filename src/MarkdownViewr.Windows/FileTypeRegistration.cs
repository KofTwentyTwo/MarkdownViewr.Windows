/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;


namespace MarkdownViewr.Windows;

/// <summary>Per-user Open With registrations have channel/install ownership and never change Windows default selections.</summary>
internal sealed class FileTypeRegistration
{
   private static readonly string[] s_extensions = [".md", ".markdown", ".mdown", ".mkd", ".txt"];
   private readonly RegistryKey _user;
   private readonly string _command;
   private readonly string _capabilities;
   private readonly string _name;
   private readonly string _icon;



   /// <summary>Quoted literal installed paths remain valid in folders containing spaces; each install/channel owns its own ProgID.</summary>
   internal FileTypeRegistration(RegistryKey user, string executable, string channel)
   {
      ArgumentNullException.ThrowIfNull(user);
      if(channel is not ("stable" or "dev") || !Path.IsPathFullyQualified(executable) || executable.Any(character => char.IsControl(character) || character == '"'))
      {
         throw new ArgumentException("A supported channel and absolute installed executable path are required.", nameof(executable));
      }
      executable = Path.GetFullPath(executable);
      _user = user;
      _name = string.Equals(channel, "dev", StringComparison.Ordinal) ? "MarkdownViewr (Development)" : "MarkdownViewr";
      string identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(executable.ToUpperInvariant())))[..24];
      ProgId = "KofTwentyTwo.MarkdownViewr." + channel + "." + identity;
      _capabilities = "Software\\KofTwentyTwo\\MarkdownViewr\\FileAssociations\\" + ProgId + "\\Capabilities";
      _command = "\"" + executable + "\" \"%1\"";
      _icon = "\"" + executable + "\",0";
   }



   /// <summary>The stable owner identity is safe as a registry value/subkey name and does not include personal paths.</summary>
   internal string ProgId { get; }



   /// <summary>Returns the OS settings route only for this unchanged registration; choosing defaults remains a Windows user action.</summary>
   internal Uri? GetDefaultAppsUri(bool supportsAppPage)
   {
      using RegistryKey? applications = _user.OpenSubKey("Software\\RegisteredApplications");
      using RegistryKey? command = _user.OpenSubKey("Software\\Classes\\" + ProgId + "\\shell\\open\\command");
      if(!string.Equals(applications?.GetValue(ProgId) as string, _capabilities, StringComparison.Ordinal) ||
         !string.Equals(command?.GetValue("") as string, _command, StringComparison.Ordinal))
      {
         return null;
      }
      return new Uri(supportsAppPage ? "ms-settings:defaultapps?registeredAppUser=" + Uri.EscapeDataString(ProgId) : "ms-settings:defaultapps");
   }



   /// <summary>Only this unchanged registered installation is the default; an unavailable native query remains unknown.</summary>
   internal bool? IsDefaultMarkdownViewer(string? effectiveProgId)
   {
      if(GetDefaultAppsUri(supportsAppPage: false) is null)
      {
         return false;
      }
      return effectiveProgId is null ? null : string.Equals(effectiveProgId, ProgId, StringComparison.OrdinalIgnoreCase);
   }



   /// <summary>Advertises the viewer to Open With and Default Apps without setting extension defaults or touching UserChoice.</summary>
   internal void Register()
   {
      using RegistryKey program = _user.CreateSubKey("Software\\Classes\\" + ProgId);
      program.SetValue("", _name + " document");
      program.SetValue("FriendlyAppName", _name);
      program.SetValue("AllowSilentDefaultTakeOver", Array.Empty<byte>(), RegistryValueKind.None);
      using(RegistryKey icon = program.CreateSubKey("DefaultIcon"))
      {
         icon.SetValue("", _icon);
      }
      using(RegistryKey open = program.CreateSubKey("shell\\open"))
      {
         open.SetValue("MultiSelectModel", "Document");
         using RegistryKey command = open.CreateSubKey("command");
         command.SetValue("", _command);
      }
      using RegistryKey capabilities = _user.CreateSubKey(_capabilities);
      capabilities.SetValue("ApplicationName", _name);
      capabilities.SetValue("ApplicationDescription", "Read Markdown and plain-text documents.");
      capabilities.SetValue("ApplicationIcon", _icon);
      using RegistryKey types = capabilities.CreateSubKey("FileAssociations");
      foreach(string extension in s_extensions)
      {
         types.SetValue(extension, ProgId);
         using RegistryKey openWith = _user.CreateSubKey("Software\\Classes\\" + extension + "\\OpenWithProgids");
         openWith.SetValue(ProgId, "");
      }
      using RegistryKey applications = _user.CreateSubKey("Software\\RegisteredApplications");
      applications.SetValue(ProgId, _capabilities);
   }



   /// <summary>Removes only an unchanged registration owned by this exact executable/channel; sibling and user entries survive.</summary>
   internal void Unregister()
   {
      string programPath = "Software\\Classes\\" + ProgId;
      using(RegistryKey? command = _user.OpenSubKey(programPath + "\\shell\\open\\command"))
      {
         if(!string.Equals(command?.GetValue("") as string, _command, StringComparison.Ordinal))
         {
            return;
         }
      }
      foreach(string extension in s_extensions)
      {
         using RegistryKey? openWith = _user.OpenSubKey("Software\\Classes\\" + extension + "\\OpenWithProgids", writable: true);
         openWith?.DeleteValue(ProgId, throwOnMissingValue: false);
      }
      using(RegistryKey? applications = _user.OpenSubKey("Software\\RegisteredApplications", writable: true))
      {
         if(string.Equals(applications?.GetValue(ProgId) as string, _capabilities, StringComparison.Ordinal))
         {
            applications!.DeleteValue(ProgId, throwOnMissingValue: false);
         }
      }
      _user.DeleteSubKeyTree(programPath, throwOnMissingSubKey: false);
      _user.DeleteSubKeyTree("Software\\KofTwentyTwo\\MarkdownViewr\\FileAssociations\\" + ProgId, throwOnMissingSubKey: false);
   }
}
