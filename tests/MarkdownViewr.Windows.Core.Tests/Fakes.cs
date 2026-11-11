/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>A per-test temporary directory, deleted on dispose.</summary>
internal sealed class TempDirectory : IDisposable
{
   /// <summary>Creates the directory.</summary>
   public TempDirectory()
   {
      Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "markdownviewr-windows-tests", Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(Path);
   }



   /// <summary>The directory.</summary>
   public string Path { get; }



   /// <summary>A file path inside the directory.</summary>
   public string File(string name) => System.IO.Path.Combine(Path, name);



   /// <summary>
   /// Deletes the directory, and the shared parent once no other test is using it. A handle left
   /// open by a failed test keeps its folder: cleanup must not replace the failure being reported.
   /// </summary>
   public void Dispose()
   {
      try
      {
         Directory.Delete(Path, recursive: true);
         Directory.Delete(System.IO.Path.GetDirectoryName(Path)!);
      }
      catch(IOException)
      {
         // The folder is in use, or the parent still holds another test's folder.
      }
   }
}
