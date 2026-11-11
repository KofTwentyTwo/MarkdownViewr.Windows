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
   /// <summary>A file with no content, as a program that clears a file before rewriting it leaves it; defaults apply and a save replaces it.</summary>
   Empty,
   /// <summary>Valid properties were kept and invalid ones dropped; a save keeps a copy of the original aside.</summary>
   Repaired,
   /// <summary>Not settings at all; defaults apply and a save keeps a copy of the file aside.</summary>
   Corrupt,
   /// <summary>The content is unknown, so it must not be replaced.</summary>
   Unreadable,
}
