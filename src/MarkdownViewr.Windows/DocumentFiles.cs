/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;


namespace MarkdownViewr.Windows;

/// <summary>Reads a bounded document snapshot and serves images through verified open file handles.</summary>
internal sealed partial class DocumentFiles
{
   private const int MaximumImageBytes = 16 * 1024 * 1024;
   private readonly string _directory;
   private readonly string _physicalRoot;



   /// <summary>Retains the selected directory and the physical root of the document actually opened.</summary>
   private DocumentFiles(string path, string physicalPath, string markdown, DateTime modifiedUtc, long byteLength)
   {
      _directory = Path.GetDirectoryName(path)!;
      _physicalRoot = Path.GetDirectoryName(physicalPath)! + Path.DirectorySeparatorChar;
      Markdown = markdown;
      ModifiedUtc = modifiedUtc;
      ByteLength = byteLength;
   }



   /// <summary>The bounded snapshot parsed into the document surface.</summary>
   public string Markdown { get; }



   /// <summary>The modification timestamp of the opened file handle retained with this loaded snapshot.</summary>
   public DateTime ModifiedUtc { get; }



   /// <summary>The opened snapshot's byte length helps detect changes missed by directory notifications.</summary>
   public long ByteLength { get; }



   /// <summary>Loads at most the parser limit plus one character, using the same handle for identity and contents.</summary>
   public static DocumentFiles Open(string path)
   {
      string fullPath = Path.GetFullPath(path);
      using SafeFileHandle handle = File.OpenHandle(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
      string physicalPath = GetPhysicalPath(handle);
      using var stream = new FileStream(handle, FileAccess.Read);
      using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
      char[] buffer = new char[Core.Documents.MarkdownRenderer.MaximumCharacters + 1];
      int count = reader.ReadBlock(buffer, 0, buffer.Length);
      if(count > Core.Documents.MarkdownRenderer.MaximumCharacters)
      {
         throw new IOException("This document exceeds the 16 Mi-character limit.");
      }
      return new DocumentFiles(fullPath, physicalPath, new string(buffer, 0, count), File.GetLastWriteTimeUtc(handle), RandomAccess.GetLength(handle));
   }



   /// <summary>Checks the final image handle against the physical document root before reading any image bytes.</summary>
   public byte[] ReadImage(string relativePath)
   {
      using SafeFileHandle handle = File.OpenHandle(Path.Combine(_directory, relativePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
      if(!GetPhysicalPath(handle).StartsWith(_physicalRoot, StringComparison.Ordinal))
      {
         throw new IOException("This image resolves outside the document folder.");
      }
      using var stream = new FileStream(handle, FileAccess.Read);
      if(stream.Length > MaximumImageBytes)
      {
         throw new IOException("This image exceeds the 16 MiB limit.");
      }
      byte[] bytes = new byte[checked((int)stream.Length)];
      stream.ReadExactly(bytes);
      return bytes;
   }



   /// <summary>Uses Windows normalization on the open handle; aliases and reparse points are resolved before access.</summary>
   private static unsafe string GetPhysicalPath(SafeFileHandle handle)
   {
      const int Capacity = 32768;
      char* buffer = stackalloc char[Capacity];
      uint count = GetFinalPathNameByHandle(handle, buffer, Capacity, 0);
      if(count == 0 || count >= Capacity)
      {
         throw new IOException("Could not verify the opened file's physical location.", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
      }
      return new string(buffer, 0, checked((int)count));
   }



   /// <summary>Retrieves the normalized DOS path of an already opened file, including resolved reparse points.</summary>
   [LibraryImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", SetLastError = true)]
   private static unsafe partial uint GetFinalPathNameByHandle(SafeFileHandle handle, char* path, uint capacity, uint flags);
}
