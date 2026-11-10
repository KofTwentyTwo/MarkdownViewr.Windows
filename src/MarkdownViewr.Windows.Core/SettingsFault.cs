/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core;

/// <summary>Why the settings file could not be used as stored; reported without paths or operating-system message text.</summary>
public enum SettingsFault
{
   /// <summary>The file exists but does not hold valid settings; defaults are used and the next save keeps the invalid file aside.</summary>
   Corrupt,

   /// <summary>The file could not be read after bounded retries; it is left untouched and nothing is written over it.</summary>
   Unreadable,

   /// <summary>Another process kept the settings lock for every attempt; the change was not saved.</summary>
   Busy,

   /// <summary>The change could not be written; the previous file remains in place.</summary>
   WriteFailed,
}
