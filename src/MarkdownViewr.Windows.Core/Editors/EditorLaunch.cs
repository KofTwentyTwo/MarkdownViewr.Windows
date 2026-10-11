/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Diagnostics;


namespace MarkdownViewr.Windows.Core.Editors;

/// <summary>Builds an explicit editor process without interpreting a document path as a shell command.</summary>
public static class EditorLaunch
{
   /// <summary>Only a bounded, fully qualified Windows executable can be persisted as the chosen editor.</summary>
   public static bool IsExecutablePath(string? path)
      => path is { Length: > 0 and <= 32767 } && !path.Any(char.IsControl) && Path.IsPathFullyQualified(path) && string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase);



   /// <summary>Supplies exactly one literal file or parent-folder argument; availability is checked by the Windows adapter.</summary>
   public static ProcessStartInfo Create(string executable, string document, bool opensFolder = false)
   {
      if(!IsExecutablePath(executable))
      {
         throw new ArgumentException("Choose an editor executable using its full path.", nameof(executable));
      }
      if(string.IsNullOrEmpty(document) || document.Length > 32767 || document.Any(char.IsControl) || !Path.IsPathFullyQualified(document) || Path.GetFileName(document).Length == 0)
      {
         throw new ArgumentException("The document must have a full file path.", nameof(document));
      }
      var start = new ProcessStartInfo
      {
         FileName = executable,
         UseShellExecute = false,
         WorkingDirectory = Path.GetDirectoryName(document),
      };
      start.ArgumentList.Add(opensFolder ? Path.GetDirectoryName(document)! : document);
      return start;
   }
}
