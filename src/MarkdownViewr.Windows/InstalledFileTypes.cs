/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.InteropServices;
using Microsoft.Win32;
using Velopack.Locators;


namespace MarkdownViewr.Windows;

/// <summary>Only real per-user installed builds register file types; builds and portable previews leave Windows associations alone.</summary>
internal static partial class InstalledFileTypes
{
   /// <summary>Refreshes registrations at installation/update using the stable current directory rather than version folders.</summary>
   internal static void Register() => Change(uninstall: false);



   /// <summary>Uninstall removes only that installation/channel's unchanged registrations.</summary>
   internal static void Unregister() => Change(uninstall: true);



   /// <summary>Only an installed, unchanged registration can open its default-selection page; older Windows uses the general page.</summary>
   internal static Uri? GetDefaultAppsUri()
   {
      FileTypeRegistration? registration = CurrentRegistration();
      // Windows 11 23H2 guarantees the app-specific route. Earlier builds retain general Default Apps guidance.
      return registration?.GetDefaultAppsUri(OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22631));
   }



   /// <summary>Velopack fast callbacks run before XAML; this path uses only the registry and bounded local metadata.</summary>
   private static void Change(bool uninstall)
   {
      FileTypeRegistration? registration = CurrentRegistration();
      if(registration is null)
      {
         return;
      }
      if(uninstall)
      {
         registration.Unregister();
      }
      else
      {
         registration.Register();
      }
      NotifyAssociationChanged(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
   }



   /// <summary>Derives channel and stable executable from the actual installer locator; portable copies never acquire registrations.</summary>
   private static FileTypeRegistration? CurrentRegistration()
   {
      IVelopackLocator locator = VelopackLocator.Current;
      if(locator.CurrentlyInstalledVersion is null || locator.IsPortable || string.IsNullOrEmpty(locator.RootAppDir))
      {
         return null;
      }
      string executable = Path.Combine(locator.RootAppDir, "current", "MarkdownViewr.Windows.exe");
      if(!File.Exists(executable))
      {
         throw new IOException("The installed application executable was not found.");
      }
      return new FileTypeRegistration(Registry.CurrentUser, executable, locator.Channel ?? "stable");
   }



   /// <summary>Refreshes Explorer's cached associations after an owned registration changes.</summary>
   [LibraryImport("shell32.dll", EntryPoint = "SHChangeNotify")]
   private static partial void NotifyAssociationChanged(uint events, uint flags, IntPtr item1, IntPtr item2);
}
