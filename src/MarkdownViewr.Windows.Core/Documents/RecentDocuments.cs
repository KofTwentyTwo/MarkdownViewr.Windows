/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Keeps a bounded private history of successfully opened absolute Windows paths.</summary>
public static class RecentDocuments
{
   /// <summary>Limits the menu and persisted history without probing files or disclosing paths in logs.</summary>
   public const int MaximumCount = 20;



   /// <summary>Repairs corrupt history and preserves most-recent order, using Windows path comparison.</summary>
   public static string[] Sanitize(IEnumerable<string?>? paths)
      => (paths ?? []).Where(IsValid).Select(path => path!).Distinct(StringComparer.OrdinalIgnoreCase).Take(MaximumCount).ToArray();



   /// <summary>Promotes only a path whose native document open has already succeeded.</summary>
   public static string[] Record(IEnumerable<string?>? paths, string path)
      => Sanitize(new[] { path }.Concat(paths ?? []));



   /// <summary>Saved history must not carry relative paths, URLs, control characters or unbounded strings.</summary>
   private static bool IsValid(string? path)
      => !string.IsNullOrWhiteSpace(path) && path.Length <= 32767 && !path.Any(char.IsControl) && Path.IsPathFullyQualified(path);
}
