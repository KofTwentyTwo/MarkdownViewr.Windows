/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core;

/// <summary>What reading the settings file found; only <see cref="Unreadable"/> forbids writing over it.</summary>
internal enum SettingsRead
{
   /// <summary>Valid stored settings.</summary>
   Loaded,

   /// <summary>No file yet; defaults apply and the first save creates it.</summary>
   Missing,

   /// <summary>Invalid content; defaults apply and a save keeps the invalid file aside.</summary>
   Corrupt,

   /// <summary>The content is unknown, so it must not be replaced.</summary>
   Unreadable,
}
