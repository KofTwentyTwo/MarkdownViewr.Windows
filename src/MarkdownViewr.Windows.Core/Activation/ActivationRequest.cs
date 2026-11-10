/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Security.Cryptography;
using System.Text;


namespace MarkdownViewr.Windows.Core.Activation;

/// <summary>
/// The documents one launch asks to open: bounded, literal, absolute paths. This is the whole
/// message a second launch sends to the running reader; it carries no commands.
/// </summary>
public sealed record ActivationRequest(string[] Paths, bool HasInvalidPath = false)
{
   /// <summary>The most documents one launch may open.</summary>
   public const int MaximumPaths = 128;

   /// <summary>The largest serialized request the running reader accepts.</summary>
   public const int MaximumPayloadBytes = 1024 * 1024;

   private const int MaximumPathCharacters = 32767;



   /// <summary>
   /// Resolves each argument against the launching process's folder, so a relative path keeps
   /// its meaning when the running reader has a different working folder. An argument that is
   /// not a usable path is dropped and flagged; the usable ones still open.
   /// </summary>
   public static ActivationRequest Create(IEnumerable<string> paths, string workingDirectory)
   {
      ArgumentNullException.ThrowIfNull(paths);
      string[] candidates = paths.Take(MaximumPaths + 1).ToArray();
      if(candidates.Length > MaximumPaths)
      {
         throw new ArgumentException("Too many document paths.", nameof(paths));
      }
      string?[] resolved = candidates.Select(path => Resolve(path, workingDirectory)).ToArray();
      return new ActivationRequest(resolved.OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), resolved.Contains(null));
   }



   /// <summary>Rejects a request that did not come from <see cref="Create"/>: a relative, empty, overlong or control-bearing path, or too many of them.</summary>
   public void Validate()
   {
      if(Paths is null || Paths.Length > MaximumPaths || !Paths.All(IsUsable))
      {
         throw new InvalidDataException("Invalid document activation paths.");
      }
   }



   /// <summary>
   /// Names the single-instance group: one per executable folder and data folder, so stable and
   /// development editions, loose builds and test profiles never forward to each other.
   /// </summary>
   public static string InstanceKey(string executableDirectory, string dataDirectory)
   {
      string identity = Path.TrimEndingDirectorySeparator(Path.GetFullPath(executableDirectory)).ToUpperInvariant() + "\0" + Path.TrimEndingDirectorySeparator(Path.GetFullPath(dataDirectory)).ToUpperInvariant();
      return "k22-markdownviewr-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
   }



   /// <summary>The absolute form of one argument, or null when it cannot name a document.</summary>
   private static string? Resolve(string? path, string workingDirectory)
   {
      if(string.IsNullOrWhiteSpace(path))
      {
         return null;
      }
      try
      {
         string full = Path.GetFullPath(path, workingDirectory);
         return IsUsable(full) ? full : null;
      }
      catch(Exception exception) when(exception is ArgumentException or NotSupportedException or IOException)
      {
         return null;
      }
   }



   /// <summary>A path the reader may be asked to open: absolute, bounded, and free of control characters.</summary>
   private static bool IsUsable(string? path)
      => !string.IsNullOrEmpty(path) && path.Length <= MaximumPathCharacters && Path.IsPathFullyQualified(path) && !path.Any(char.IsControl);
}
