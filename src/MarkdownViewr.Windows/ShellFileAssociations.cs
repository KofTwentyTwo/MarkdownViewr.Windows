/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.InteropServices;


namespace MarkdownViewr.Windows;

/// <summary>Queries Windows' effective user-selected ProgID without inspecting or changing protected UserChoice data.</summary>
internal static partial class ShellFileAssociations
{
   private const int AssocStrProgId = 20;
   private const int NoAssociation = unchecked((int)0x80070483);
   private const int MaximumCharacters = 1024;



   /// <summary>Windows 10+ supplies ASSOCSTR_PROGID; a missing association differs from an unavailable/oversized query.</summary>
   internal static unsafe string? QueryProgId(string extension)
   {
      char[] buffer = new char[MaximumCharacters];
      uint length = MaximumCharacters;
      int result;
      fixed(char* output = buffer)
      {
         result = QueryString(0, AssocStrProgId, extension, null, output, ref length);
      }
      if(result == NoAssociation)
      {
         return "";
      }
      return result == 0 && length is > 0 and <= MaximumCharacters && buffer[length - 1] == '\0'
         ? new string(buffer, 0, (int)length - 1) : null;
   }



   /// <summary>A fixed bounded Unicode query reads the effective association; no default-setting API is exposed.</summary>
   [LibraryImport("shlwapi.dll", EntryPoint = "AssocQueryStringW", StringMarshalling = StringMarshalling.Utf16)]
   private static unsafe partial int QueryString(uint flags, int value, string association, string? extra, char* output, ref uint characters);
}
