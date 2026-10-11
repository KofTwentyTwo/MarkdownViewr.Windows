/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.InteropServices;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Reads the actual Windows Unicode clipboard without writing fixture values or introducing rich-text formats.</summary>
internal static partial class NativeClipboard
{
   /// <summary>Returns null while another owner holds the clipboard; callers poll the specific real copy result.</summary>
   public static string? ReadText()
   {
      if(!OpenClipboard(0))
      {
         return null;
      }
      try
      {
         nint memory = GetClipboardData(13);
         if(memory == 0)
         {
            return null;
         }
         nint pointer = GlobalLock(memory);
         if(pointer == 0)
         {
            return null;
         }
         try
         {
            return Marshal.PtrToStringUni(pointer);
         }
         finally
         {
            GlobalUnlock(memory);
         }
      }
      finally
      {
         CloseClipboard();
      }
   }



   /// <summary>Checks native registered format presence so syntax colors cannot contaminate plain source copying.</summary>
   public static bool HasRichText() => IsClipboardFormatAvailable(RegisterClipboardFormat("Rich Text Format")) || IsClipboardFormatAvailable(RegisterClipboardFormat("HTML Format"));



   /// <summary>Opens only the current clipboard for a bounded native read.</summary>
   [LibraryImport("user32.dll")]
   [return: MarshalAs(UnmanagedType.Bool)]
   private static partial bool OpenClipboard(nint owner);



   /// <summary>Releases the clipboard after every read, including absent data and errors.</summary>
   [LibraryImport("user32.dll")]
   [return: MarshalAs(UnmanagedType.Bool)]
   private static partial bool CloseClipboard();



   /// <summary>Retrieves existing Unicode text data without replacing it.</summary>
   [LibraryImport("user32.dll")]
   private static partial nint GetClipboardData(uint format);



   /// <summary>Locks native clipboard memory only while the clipboard is open.</summary>
   [LibraryImport("kernel32.dll")]
   private static partial nint GlobalLock(nint memory);



   /// <summary>Releases the native memory lock after marshaling the selected source.</summary>
   [LibraryImport("kernel32.dll")]
   [return: MarshalAs(UnmanagedType.Bool)]
   private static partial bool GlobalUnlock(nint memory);



   /// <summary>Resolves Windows format identifiers without creating clipboard data.</summary>
   [LibraryImport("user32.dll", EntryPoint = "RegisterClipboardFormatW", StringMarshalling = StringMarshalling.Utf16)]
   private static partial uint RegisterClipboardFormat(string name);



   /// <summary>Observes whether a rich format is actually present in the current clipboard.</summary>
   [LibraryImport("user32.dll")]
   [return: MarshalAs(UnmanagedType.Bool)]
   private static partial bool IsClipboardFormatAvailable(uint format);
}
