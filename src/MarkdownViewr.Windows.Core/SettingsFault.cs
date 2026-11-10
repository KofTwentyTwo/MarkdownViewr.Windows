/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core;

/// <summary>Why the settings file could not be used as stored; reported without paths or operating-system message text.</summary>
public enum SettingsFault
{
   /// <summary>The file does not hold settings at all; defaults are used and the next save keeps a copy of the file aside.</summary>
   Corrupt,
   /// <summary>Some stored properties were invalid and read as their defaults; the next save keeps a copy of the original aside.</summary>
   Repaired,
   /// <summary>The file could not be read, or another program was writing it; it is left untouched and nothing is written over it.</summary>
   Unreadable,
   /// <summary>Another instance kept the settings lock for every attempt; the change was not saved.</summary>
   Busy,
   /// <summary>The change could not be written; the stored file is unchanged.</summary>
   WriteFailed,
   /// <summary>The invalid original could not be copied aside; the change was saved without the copy.</summary>
   NotKeptAside,
}
