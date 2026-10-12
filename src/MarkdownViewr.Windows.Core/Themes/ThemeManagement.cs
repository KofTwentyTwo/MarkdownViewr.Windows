/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core.Themes;

/// <summary>Copied native theme-list drafts retain reference order, disabled choices and restorable built-ins.</summary>
public static class ThemeManagement
{
   /// <summary>Corrupt list names cannot create unbounded menus or change display identities.</summary>
   public static string[] SanitizeNames(IEnumerable<string?>? names) => (names ?? [])
      .Where(name => !string.IsNullOrWhiteSpace(name) && name.Length <= 80 && !name.Any(char.IsControl))
      .Select(name => name!).Distinct(StringComparer.Ordinal).Take(512).ToArray();



   /// <summary>Disabled choices remain in the manager, while deleted built-ins stay hidden until restoration.</summary>
   public static IReadOnlyList<ReaderTheme> All(AppSettings settings)
   {
      IReadOnlyList<ReaderTheme> combined = ThemeCatalog.Combine(settings.UserThemes.Select(ReaderTheme.ParseCompatible));
      return combined.Where(theme => !settings.DeletedBuiltInThemes.Contains(theme.Name, StringComparer.Ordinal))
         .OrderBy(theme => Array.IndexOf(settings.ThemeOrder, theme.Name) is int index && index >= 0 ? index : int.MaxValue)
         .ThenBy(theme => theme.Name, StringComparer.Ordinal).ToArray();
   }



   /// <summary>Reading menus and cycling use only enabled choices in the saved order.</summary>
   public static IReadOnlyList<ReaderTheme> Enabled(AppSettings settings) => All(settings)
      .Where(theme => !settings.DisabledThemes.Contains(theme.Name, StringComparer.Ordinal)).ToArray();



   /// <summary>Removing an active choice falls back to the first enabled theme; an empty list retains a safe bundled palette.</summary>
   public static ReaderTheme Active(AppSettings settings)
   {
      IReadOnlyList<ReaderTheme> enabled = Enabled(settings);
      return enabled.FirstOrDefault(theme => string.Equals(theme.Name, settings.DocumentTheme, StringComparison.Ordinal))
         ?? (enabled.Count > 0 ? enabled[0] : ThemeCatalog.Find(null));
   }



   /// <summary>Previous/next wrap over enabled choices; an empty list has no cycle action.</summary>
   public static ReaderTheme? Cycle(AppSettings settings, bool forward)
   {
      ReaderTheme[] enabled = Enabled(settings).ToArray();
      if(enabled.Length == 0)
      {
         return null;
      }
      int index = Array.FindIndex(enabled, theme => string.Equals(theme.Name, settings.DocumentTheme, StringComparison.Ordinal));
      int current = Math.Max(0, index);
      return enabled[(current + (forward ? 1 : enabled.Length - 1)) % enabled.Length];
   }



   /// <summary>Moves one visible choice without mutating the caller's saved arrays; disabled themes retain their place.</summary>
   public static AppSettings Move(AppSettings settings, string name, bool up)
   {
      AppSettings draft = SettingsChanges.Copy(settings);
      string[] order = All(settings).Select(theme => theme.Name).ToArray();
      int index = Array.IndexOf(order, name);
      int next = index + (up ? -1 : 1);
      if(index >= 0 && next >= 0 && next < order.Length)
      {
         (order[index], order[next]) = (order[next], order[index]);
         draft.ThemeOrder = order;
      }
      return draft;
   }



   /// <summary>Enabled changes retain theme data and allow an intentionally empty reading menu.</summary>
   public static AppSettings SetEnabled(AppSettings settings, string name, bool enabled)
   {
      AppSettings draft = SettingsChanges.Copy(settings);
      if(All(settings).Any(theme => string.Equals(theme.Name, name, StringComparison.Ordinal)))
      {
         draft.DisabledThemes = enabled ? draft.DisabledThemes.Where(item => !string.Equals(item, name, StringComparison.Ordinal)).ToArray()
            : draft.DisabledThemes.Append(name).Distinct(StringComparer.Ordinal).ToArray();
         draft.DocumentTheme = Active(draft).Name;
      }
      return draft;
   }



   /// <summary>Deletes user data and hides a same-named built-in; restoration never requires deleting files outside settings.</summary>
   public static AppSettings Delete(AppSettings settings, string name)
   {
      AppSettings draft = SettingsChanges.Copy(settings);
      draft.UserThemes = draft.UserThemes.Where(json => !string.Equals(ReaderTheme.ParseCompatible(json).Name, name, StringComparison.Ordinal)).ToArray();
      draft.DisabledThemes = draft.DisabledThemes.Where(item => !string.Equals(item, name, StringComparison.Ordinal)).ToArray();
      draft.ThemeOrder = draft.ThemeOrder.Where(item => !string.Equals(item, name, StringComparison.Ordinal)).ToArray();
      if(ThemeCatalog.BuiltIn.Any(theme => string.Equals(theme.Name, name, StringComparison.Ordinal)))
      {
         draft.DeletedBuiltInThemes = draft.DeletedBuiltInThemes.Append(name).Distinct(StringComparer.Ordinal).ToArray();
      }
      draft.DocumentTheme = Active(draft).Name;
      return draft;
   }



   /// <summary>Reference restoration keeps built-in edits as uniquely named custom copies, restores originals and enables every choice.</summary>
   public static AppSettings RestoreBuiltIns(AppSettings settings)
   {
      AppSettings draft = SettingsChanges.Copy(settings);
      var builtIns = ThemeCatalog.BuiltIn.Select(theme => theme.Name).ToHashSet(StringComparer.Ordinal);
      ReaderTheme[] users = settings.UserThemes.Select(ReaderTheme.ParseCompatible).GroupBy(theme => theme.Name, StringComparer.Ordinal).Select(group => group.Last()).ToArray();
      var taken = builtIns.Concat(users.Select(theme => theme.Name)).ToHashSet(StringComparer.Ordinal);
      var renamed = new Dictionary<string, string>(StringComparer.Ordinal);
      draft.UserThemes = users.Select(theme =>
      {
         if(!builtIns.Contains(theme.Name))
         {
            return theme.Json;
         }
         string name = UniqueName(theme.Name, taken);
         taken.Add(name);
         renamed[theme.Name] = name;
         return theme.Rename(name).Json;
      }).ToArray();
      draft.DocumentTheme = renamed.GetValueOrDefault(draft.DocumentTheme, draft.DocumentTheme);
      draft.ThemeOrder = draft.ThemeOrder.Select(name => renamed.GetValueOrDefault(name, name)).ToArray();
      draft.DisabledThemes = [];
      draft.DeletedBuiltInThemes = [];
      return draft;
   }



   /// <summary>Imports a validated copy with explicit conflict semantics; newer schemas cannot be persisted without deliberate confirmation.</summary>
   public static AppSettings Import(AppSettings settings, ReaderTheme theme, ThemeImportResolution resolution = ThemeImportResolution.KeepBoth, bool approveNewerSchema = false)
   {
      ArgumentNullException.ThrowIfNull(theme);
      if(!Enum.IsDefined(resolution))
      {
         throw new ArgumentOutOfRangeException(nameof(resolution));
      }
      if(theme.SchemaVersion > 1 && !approveNewerSchema)
      {
         throw new InvalidOperationException("Confirm importing this newer theme schema before saving it.");
      }
      AppSettings draft = SettingsChanges.Copy(settings);
      var names = All(settings).Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
      if(names.Contains(theme.Name))
      {
         if(resolution == ThemeImportResolution.Skip)
         {
            return draft;
         }
         if(resolution == ThemeImportResolution.KeepBoth)
         {
            theme = theme.Rename(UniqueName(theme.Name, names));
         }
      }
      draft.UserThemes = draft.UserThemes.Where(json => !string.Equals(ReaderTheme.ParseCompatible(json).Name, theme.Name, StringComparison.Ordinal)).Append(theme.Json).ToArray();
      draft.DeletedBuiltInThemes = draft.DeletedBuiltInThemes.Where(name => !string.Equals(name, theme.Name, StringComparison.Ordinal)).ToArray();
      return draft;
   }



   /// <summary>Editing in place may override its built-in; renaming cannot silently replace a different existing theme.</summary>
   public static void ValidateEditName(string originalName, string editedName, IEnumerable<string> existingNames)
   {
      ArgumentNullException.ThrowIfNull(existingNames);
      if(!string.Equals(originalName, editedName, StringComparison.Ordinal) && existingNames.Contains(editedName, StringComparer.Ordinal))
      {
         throw new System.Text.Json.JsonException("A theme with this name already exists. Choose another name.");
      }
   }



   /// <summary>Saves a copied editor draft and deliberately selects/enables it without hiding another theme under a renamed collision.</summary>
   public static AppSettings SaveEdited(AppSettings settings, ReaderTheme original, ReaderTheme edited)
   {
      ArgumentNullException.ThrowIfNull(original);
      ArgumentNullException.ThrowIfNull(edited);
      ValidateEditName(original.Name, edited.Name, All(settings).Select(theme => theme.Name));
      AppSettings draft = Import(settings, edited, ThemeImportResolution.Overwrite, approveNewerSchema: original.SchemaVersion == edited.SchemaVersion && original.SchemaVersion > 1);
      draft.DocumentTheme = edited.Name;
      draft.DisabledThemes = draft.DisabledThemes.Where(name => !string.Equals(name, edited.Name, StringComparison.Ordinal)).ToArray();
      return draft;
   }




   /// <summary>A new native draft starts from the safe default palette and a name independent of existing choices.</summary>
   public static ReaderTheme NewTheme(AppSettings settings)
      => ThemeCatalog.Find(null).Rename(UniqueName("New theme", All(settings).Select(theme => theme.Name).ToHashSet(StringComparer.Ordinal)));



   /// <summary>Copy preserves a selected theme, including a disabled choice, with a unique bounded display name.</summary>
   public static ReaderTheme CopyTheme(AppSettings settings, ReaderTheme theme)
   {
      ArgumentNullException.ThrowIfNull(theme);
      string name = theme.Name[..Math.Min(theme.Name.Length, 75)] + " Copy";
      return theme.Rename(UniqueName(name, All(settings).Select(item => item.Name).ToHashSet(StringComparer.Ordinal)));
   }




   /// <summary>Bounded names support deterministic keep-both suffixes without losing original theme fields.</summary>
   private static string UniqueName(string name, HashSet<string> taken)
   {
      if(!taken.Contains(name))
      {
         return name;
      }
      for(int number = 2; ; number++)
      {
         string suffix = " (" + number.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
         string candidate = name[..Math.Min(name.Length, 80 - suffix.Length)] + suffix;
         if(!taken.Contains(candidate))
         {
            return candidate;
         }
      }
   }
}
