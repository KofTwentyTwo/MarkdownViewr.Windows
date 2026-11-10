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
   /// <summary>Valid values were kept and invalid ones dropped; a save keeps the original file aside.</summary>
   Repaired,
   /// <summary>Not settings at all; defaults apply and a save keeps the invalid file aside.</summary>
   Corrupt,
   /// <summary>The content is unknown, so it must not be replaced.</summary>
   Unreadable,
}
