/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.Win32;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Registry adapters operate beneath an isolated UUID subtree, never the user's real Classes/default-app keys.</summary>
internal sealed class RegistryFixture : IDisposable
{
   private readonly string _path = "Software\\KofTwentyTwo\\MarkdownViewr.Windows.Tests\\" + Guid.NewGuid().ToString("N");



   /// <summary>Creates the owned isolated root using the same Windows registry implementation as production.</summary>
   internal RegistryFixture() => Root = Registry.CurrentUser.CreateSubKey(_path);



   /// <summary>The adapter uses relative registry keys beneath this fixture.</summary>
   internal RegistryKey Root { get; }



   /// <summary>Deletes only the fixed-prefix UUID subtree created by this fixture.</summary>
   public void Dispose()
   {
      Root.Dispose();
      Registry.CurrentUser.DeleteSubKeyTree(_path, throwOnMissingSubKey: false);
   }
}
