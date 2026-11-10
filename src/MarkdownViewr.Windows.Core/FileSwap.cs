/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;


namespace MarkdownViewr.Windows.Core;

/// <summary>
/// Replaces a file by renaming another over it in a single step. The destination is never
/// absent, not even for an instant, and the rename works while the destination is open through
/// handles that allow deletion; those handles go on reading the content they opened.
/// <see cref="File.Move(string, string, bool)"/> refuses when any handle is open on the
/// destination, and <see cref="File.Replace(string, string, string?)"/> is two renames with the
/// destination missing between them, so neither can do this.
/// </summary>
internal static partial class FileSwap
{
   private const uint DeleteAccess = 0x00010000;
   private const uint ShareAll = 0x00000007;
   private const uint OpenExisting = 3;
   private const int FileRenameInfoEx = 22;
   private const uint ReplaceIfExists = 0x1;
   private const uint PosixSemantics = 0x2;
   private const int AccessDenied = 5;



   /// <summary>
   /// Renames <paramref name="source"/> over <paramref name="destination"/>. Returns false when the
   /// volume cannot rename over an open file, as file systems other than NTFS may not. Throws
   /// <see cref="IOException"/> with the Windows error as its HRESULT when the rename is refused,
   /// for example by a handle on the destination that does not allow deletion, and
   /// <see cref="UnauthorizedAccessException"/> when access is denied.
   /// </summary>
   public static bool TryRenameOver(string source, string destination)
   {
      using SafeFileHandle handle = CreateFile(source, DeleteAccess, ShareAll, 0, OpenExisting, 0, 0);
      if(handle.IsInvalid)
      {
         throw Failure(Marshal.GetLastPInvokeError());
      }
      // FILE_RENAME_INFO: flags, root directory handle, name length in bytes, then the name itself.
      byte[] name = Encoding.Unicode.GetBytes(destination);
      int lengthOffset = 2 * IntPtr.Size;
      int nameOffset = lengthOffset + sizeof(int);
      byte[] rename = new byte[nameOffset + name.Length + sizeof(char)];
      BitConverter.TryWriteBytes(rename, ReplaceIfExists | PosixSemantics);
      BitConverter.TryWriteBytes(rename.AsSpan(lengthOffset), name.Length);
      name.CopyTo(rename, nameOffset);
      if(SetFileInformationByHandle(handle, FileRenameInfoEx, rename, rename.Length))
      {
         return true;
      }
      int error = Marshal.GetLastPInvokeError();
      return IsUnsupported(error) ? false : throw Failure(error);
   }



   /// <summary>The errors a volume answers with when it does not implement this kind of rename: invalid function, not supported, invalid parameter.</summary>
   internal static bool IsUnsupported(int error) => error is 1 or 50 or 87;



   /// <summary>The exception a managed file operation would throw for the same Windows error.</summary>
   private static Exception Failure(int error)
   {
      string message = new Win32Exception(error).Message;
      return error == AccessDenied ? new UnauthorizedAccessException(message) : new IOException(message, unchecked((int)0x80070000) | error);
   }



   /// <summary>Opens the file to be renamed with delete access, which a managed file stream cannot request.</summary>
   [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
   private static partial SafeFileHandle CreateFile(string path, uint access, uint share, nint security, uint disposition, uint flags, nint template);



   /// <summary>Applies the rename to the open file.</summary>
   [LibraryImport("kernel32.dll", SetLastError = true)]
   [return: MarshalAs(UnmanagedType.Bool)]
   private static partial bool SetFileInformationByHandle(SafeFileHandle file, int informationClass, byte[] information, int size);
}
