/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core.Themes;

/// <summary>Loads the original bundled themes from assembly resources, independent of the install directory.</summary>
public static class ThemeCatalog
{
   private static readonly ReaderTheme[] s_builtIn = LoadBuiltIn();



   /// <summary>The bundled themes sorted by display name.</summary>
   public static IReadOnlyList<ReaderTheme> BuiltIn { get; } = Array.AsReadOnly(s_builtIn);



   /// <summary>Restores a saved theme by name, falling back to Darin's default after removal or invalid settings.</summary>
   public static ReaderTheme Find(string? name)
      => s_builtIn.FirstOrDefault(theme => string.Equals(theme.Name, name, StringComparison.Ordinal))
         ?? s_builtIn.Single(theme => string.Equals(theme.Name, "Catppuccin Mocha", StringComparison.Ordinal));



   /// <summary>Parses the shipped JSON with the same validation used for user themes.</summary>
   private static ReaderTheme[] LoadBuiltIn()
   {
      System.Reflection.Assembly assembly = typeof(ThemeCatalog).Assembly;
      return assembly.GetManifestResourceNames()
         .Where(name => name.Contains(".Themes.BuiltIn.", StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal))
         .Select(name =>
         {
            using Stream stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            return ReaderTheme.Parse(reader.ReadToEnd());
         })
         .OrderBy(theme => theme.Name, StringComparer.Ordinal)
         .ToArray();
   }
}
