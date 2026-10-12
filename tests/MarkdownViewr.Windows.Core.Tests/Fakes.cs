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



   /// <summary>Deletes the directory; a handle left open by a failed test is left for the OS.</summary>
   public void Dispose()
   {
      try
      {
         Directory.Delete(Path, recursive: true);
      }
      catch(IOException)
      {
         // Cleanup only; the test outcome is already decided.
      }
   }
}
