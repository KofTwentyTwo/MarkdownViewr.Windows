/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core;

/// <summary>
/// The Windows file-name rules this application relies on, written out instead of asked of the
/// running platform: the characters a name cannot contain and the names that address a device.
/// </summary>
public static class WindowsFileNames
{
   /// <summary>Characters that cannot appear in a Windows file or folder name.</summary>
   public const string InvalidCharacters = "<>:\"/\\|?*";

   private static readonly HashSet<string> s_devices = new(StringComparer.OrdinalIgnoreCase)
   {
      "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$",
      "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM¹", "COM²", "COM³",
      "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "LPT¹", "LPT²", "LPT³",
   };



   /// <summary>
   /// Whether a single name addresses a device in any letter case. Windows ignores everything after
   /// the first dot and any spaces before it, so <c>nul.txt</c> and <c>COM1 .log</c> are devices too.
   /// </summary>
   public static bool IsReservedDeviceName(string name)
   {
      ArgumentNullException.ThrowIfNull(name);
      int dot = name.IndexOf('.', StringComparison.Ordinal);
      return s_devices.Contains((dot < 0 ? name : name[..dot]).TrimEnd(' '));
   }
}
