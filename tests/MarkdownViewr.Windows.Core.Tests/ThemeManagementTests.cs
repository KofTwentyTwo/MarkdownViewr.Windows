/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;
using System.Text.Json.Nodes;
using MarkdownViewr.Windows.Core.Navigation;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Theme list drafts preserve reference ordering, disabled/deleted state and edited palettes during restoration.</summary>
public sealed class ThemeManagementTests
{
   /// <summary>Older settings retain all bundled choices and the original default.</summary>
   [Fact]
   public void All_DefaultOrOlderJson_RetainsCatalog()
   {
      AppSettings settings = JsonSerializer.Deserialize("{}", AppSettingsContext.Default.AppSettings)!;
      settings.Sanitize();
      Assert.Equal(20, ThemeManagement.All(settings).Count);
      Assert.Equal(20, ThemeManagement.Enabled(settings).Count);
      Assert.Equal("Catppuccin Mocha", ThemeManagement.Active(settings).Name);
      Assert.Empty(settings.ThemeOrder);
      Assert.Empty(settings.DisabledThemes);
      Assert.Empty(settings.DeletedBuiltInThemes);
   }



   /// <summary>Overrides retain exact JSON, disabled choices stay in the manager and unknown order entries do not hide themes.</summary>
   [Fact]
   public void All_OrderOverridesDisabledDeleted_ProjectsVisibleChoices()
   {
      ReaderTheme nord = Rename(ThemeCatalog.Find("Nord"), "Nord", "p { color: #123456; }");
      var settings = new AppSettings { UserThemes = [nord.Json], ThemeOrder = ["Missing", "Nord", "GitHub Light"], DisabledThemes = ["Nord"], DeletedBuiltInThemes = ["Dracula"] };
      IReadOnlyList<ReaderTheme> all = ThemeManagement.All(settings);
      Assert.Equal("Nord", all[0].Name);
      Assert.Equal(nord.Json, all[0].Json);
      Assert.Equal("GitHub Light", all[1].Name);
      Assert.DoesNotContain(all, theme => string.Equals(theme.Name, "Dracula", StringComparison.Ordinal));
      Assert.DoesNotContain(ThemeManagement.Enabled(settings), theme => string.Equals(theme.Name, "Nord", StringComparison.Ordinal));
      Assert.Equal("GitHub Light", ThemeManagement.Active(new AppSettings { DocumentTheme = "Missing", ThemeOrder = ["GitHub Light"] }).Name);
   }



   /// <summary>Moves wrap neither boundary and retain copied arrays for cancelled drafts.</summary>
   [Theory]
   [InlineData("Catppuccin Latte", true, "Catppuccin Latte")]
   [InlineData("Zenburn", false, "Catppuccin Latte")]
   [InlineData("Missing", true, "Catppuccin Latte")]
   [InlineData("Catppuccin Latte", false, "Catppuccin Mocha")]
   [InlineData("Dracula", true, "Catppuccin Latte")]
   public void Move_BoundaryOrVisibleChoice_PreservesOriginal(string name, bool up, string expected)
   {
      var settings = new AppSettings();
      AppSettings draft = ThemeManagement.Move(settings, name, up);
      Assert.Empty(settings.ThemeOrder);
      string first = ThemeManagement.All(draft)[0].Name;
      Assert.Equal(expected, first);
      Assert.NotSame(settings.UserThemes, draft.UserThemes);
   }



   /// <summary>Disabled choices remain in order and cycling wraps over enabled choices in either direction.</summary>
   [Fact]
   public void Cycle_EnabledOrder_WrapsAndSkipsDisabled()
   {
      var settings = new AppSettings { ThemeOrder = ["Nord", "GitHub Light", "Catppuccin Mocha"], DocumentTheme = "Nord", DisabledThemes = ThemeCatalog.BuiltIn.Where(theme => theme.Name is not ("Nord" or "GitHub Light")).Select(theme => theme.Name).ToArray() };
      Assert.Equal("GitHub Light", ThemeManagement.Cycle(settings, forward: true)!.Name);
      Assert.Equal("GitHub Light", ThemeManagement.Cycle(settings, forward: false)!.Name);
      settings.DocumentTheme = "GitHub Light";
      Assert.Equal("Nord", ThemeManagement.Cycle(settings, forward: true)!.Name);
      settings.DocumentTheme = "Missing";
      Assert.Equal("GitHub Light", ThemeManagement.Cycle(settings, forward: true)!.Name);
      AppSettings disabled = ThemeManagement.SetEnabled(settings, "Nord", enabled: false);
      Assert.Contains("Nord", disabled.DisabledThemes, StringComparer.Ordinal);
      Assert.Equal("GitHub Light", disabled.DocumentTheme);
      Assert.Equal("GitHub Light", ThemeManagement.Cycle(disabled, forward: false)!.Name);
      Assert.DoesNotContain("Nord", settings.DisabledThemes, StringComparer.Ordinal);
      AppSettings enabled = ThemeManagement.SetEnabled(disabled, "Nord", enabled: true);
      Assert.DoesNotContain("Nord", enabled.DisabledThemes, StringComparer.Ordinal);
      Assert.Equal(settings.ThemeOrder, enabled.ThemeOrder);
      Assert.Equal(enabled.DisabledThemes, ThemeManagement.SetEnabled(enabled, "Unknown", enabled: false).DisabledThemes);
   }



   /// <summary>An intentionally empty list has a safe fallback and no cycle command; re-enabling restores a choice.</summary>
   [Fact]
   public void SetEnabled_AllDisabled_RetainsSafeFallback()
   {
      var settings = new AppSettings { DisabledThemes = ThemeCatalog.BuiltIn.Select(theme => theme.Name).ToArray() };
      Assert.Empty(ThemeManagement.Enabled(settings));
      Assert.Equal("Catppuccin Mocha", ThemeManagement.Active(settings).Name);
      Assert.Null(ThemeManagement.Cycle(settings, forward: true));
      AppSettings enabled = ThemeManagement.SetEnabled(settings, "Nord", enabled: true);
      Assert.Equal("Nord", ThemeManagement.Active(enabled).Name);
      AppSettings disabled = ThemeManagement.SetEnabled(enabled, "Nord", enabled: false);
      Assert.Equal(20, disabled.DisabledThemes.Length);
      Assert.Equal(20, ThemeManagement.SetEnabled(disabled, "Nord", enabled: false).DisabledThemes.Length);
   }



   /// <summary>Delete removes an override/custom draft and only bundled names receive a restorable tombstone.</summary>
   [Theory]
   [InlineData("Nord", true)]
   [InlineData("Owned custom", false)]
   [InlineData("Unknown", false)]
   public void Delete_OverrideCustomOrUnknown_PreservesOriginalAndRestorability(string name, bool builtIn)
   {
      ReaderTheme theme = Rename(ThemeCatalog.Find("Nord"), name);
      var settings = new AppSettings { UserThemes = [theme.Json], DocumentTheme = name, ThemeOrder = [name, "GitHub Light"], DisabledThemes = [name] };
      AppSettings draft = ThemeManagement.Delete(settings, name);
      Assert.Empty(draft.UserThemes);
      Assert.DoesNotContain(name, draft.ThemeOrder, StringComparer.Ordinal);
      Assert.DoesNotContain(name, draft.DisabledThemes, StringComparer.Ordinal);
      Assert.Equal(builtIn, draft.DeletedBuiltInThemes.Contains(name, StringComparer.Ordinal));
      Assert.DoesNotContain(ThemeManagement.All(draft), item => string.Equals(item.Name, name, StringComparison.Ordinal));
      Assert.Equal("GitHub Light", draft.DocumentTheme);
      Assert.Single(settings.UserThemes);
      Assert.Single(settings.DisabledThemes);
      Assert.Empty(settings.DeletedBuiltInThemes);
      Assert.Equal(draft.DeletedBuiltInThemes, ThemeManagement.Delete(draft, name).DeletedBuiltInThemes);
   }



   /// <summary>Restoration preserves edits/metadata/CSS in collision-safe custom copies and follows the renamed active/order entries.</summary>
   [Fact]
   public void RestoreBuiltIns_OverridesAndCollisions_PreservesEditsAsCustomCopies()
   {
      ReaderTheme original = ThemeCatalog.Find("Nord");
      JsonNode source = JsonNode.Parse(original.Json)!;
      source["metadata"] = new JsonObject { ["owner"] = "fixture" };
      var edited = ReaderTheme.Parse(source.ToJsonString());
      ReaderTheme second = Rename(original, "Nord (2)");
      ReaderTheme third = Rename(original, "Nord (3)");
      var settings = new AppSettings { UserThemes = [edited.Json, second.Json, third.Json], DocumentTheme = "Nord", ThemeOrder = ["Nord", "GitHub Light"], DisabledThemes = ["Nord"], DeletedBuiltInThemes = ["Dracula"] };
      AppSettings restored = ThemeManagement.RestoreBuiltIns(settings);
      Assert.Equal("Nord (4)", restored.DocumentTheme);
      Assert.Equal(new[] { "Nord (4)", "GitHub Light" }, restored.ThemeOrder);
      Assert.Empty(restored.DisabledThemes);
      Assert.Empty(restored.DeletedBuiltInThemes);
      Assert.Equal(23, ThemeManagement.Enabled(restored).Count);
      ReaderTheme copy = ThemeManagement.Active(restored);
      using var json = JsonDocument.Parse(copy.Json);
      Assert.Equal("fixture", json.RootElement.GetProperty("metadata").GetProperty("owner").GetString());
      Assert.Contains(ThemeManagement.All(restored), theme => string.Equals(theme.Json, original.Json, StringComparison.Ordinal));
      Assert.Equal(3, settings.UserThemes.Length);
      Assert.Equal("Nord", settings.DocumentTheme);
      Assert.Equal(restored.UserThemes, ThemeManagement.RestoreBuiltIns(restored).UserThemes);
   }



   /// <summary>Restoring without overrides only clears deletion/disable state and leaves custom selection intact.</summary>
   [Fact]
   public void RestoreBuiltIns_CustomSelectionAndNoOverrides_RetainsNames()
   {
      ReaderTheme custom = Rename(ThemeCatalog.Find(null), "Owned custom", "body { color: #123456; }");
      var settings = new AppSettings { UserThemes = [custom.Json], DocumentTheme = custom.Name, ThemeOrder = [custom.Name], DisabledThemes = ["Nord"], DeletedBuiltInThemes = ["Dracula"] };
      AppSettings restored = ThemeManagement.RestoreBuiltIns(settings);
      Assert.Equal(custom.Json, Assert.Single(restored.UserThemes));
      Assert.Equal(custom.Name, restored.DocumentTheme);
      Assert.Equal(custom.Name, Assert.Single(restored.ThemeOrder));
      Assert.Equal(20, ThemeManagement.Enabled(ThemeManagement.RestoreBuiltIns(new AppSettings())).Count);
   }



   /// <summary>Corrupt arrays repair independently with bounded, case-sensitive names and no control characters.</summary>
   [Fact]
   public void SanitizeNames_InvalidDuplicateOrOversized_RepairsSavedLists()
   {
      Assert.Empty(ThemeManagement.SanitizeNames(null));
      Assert.Equal(new[] { "Nord", "nord" }, ThemeManagement.SanitizeNames([null, "", " ", "Nord", "Nord", "nord", "bad\nname", new string('x', 81)]));
      Assert.Equal(512, ThemeManagement.SanitizeNames(Enumerable.Range(0, 600).Select(number => "Theme " + number.ToString(System.Globalization.CultureInfo.InvariantCulture))).Length);
      var settings = new AppSettings { ThemeOrder = ["Nord", "Nord"], DisabledThemes = null!, DeletedBuiltInThemes = ["bad\nname"] };
      settings.Sanitize();
      Assert.Equal("Nord", Assert.Single(settings.ThemeOrder));
      Assert.Empty(settings.DisabledThemes);
      Assert.Empty(settings.DeletedBuiltInThemes);
   }



   /// <summary>A cancelled copied manager and a later sibling-local save cannot restore old shared theme fields.</summary>
   [Fact]
   public void SettingsChanges_ThemeDraftAndSiblingMerge_PreservesIndependentFields()
   {
      var before = new AppSettings();
      AppSettings edited = ThemeManagement.SetEnabled(ThemeManagement.Move(before, "Nord", up: true), "Dracula", enabled: false);
      var current = new AppSettings { GlobalCss = "body { color: #123456; }", ShowSource = true };
      AppSettings merged = SettingsChanges.Merge(current, before, edited);
      Assert.True(merged.ShowSource);
      Assert.Equal(current.GlobalCss, merged.GlobalCss);
      Assert.Contains("Dracula", merged.DisabledThemes, StringComparer.Ordinal);
      Assert.Empty(before.DisabledThemes);
      AppSettings staleLocal = SettingsChanges.Copy(before);
      staleLocal.ShowOutline = true;
      AppSettings saved = SettingsChanges.Merge(merged, before, staleLocal);
      Assert.True(saved.ShowOutline);
      Assert.Equal(merged.ThemeOrder, saved.ThemeOrder);
      Assert.Equal(merged.DisabledThemes, saved.DisabledThemes);
      AppSettings roundTrip = JsonSerializer.Deserialize(JsonSerializer.Serialize(saved, AppSettingsContext.Default.AppSettings), AppSettingsContext.Default.AppSettings)!;
      Assert.Equal(saved.ThemeOrder, roundTrip.ThemeOrder);
      Assert.Equal(saved.DisabledThemes, roundTrip.DisabledThemes);
   }



   /// <summary>Windows reference cycling keys operate only on owned reading focus and exact Ctrl+Shift arrows.</summary>
   [Theory]
   [InlineData('\u2193', true, true, true, false, false, ReaderCommand.NextTheme)]
   [InlineData('\u2191', true, true, true, false, false, ReaderCommand.PreviousTheme)]
   [InlineData('\u2193', false, true, true, false, false, ReaderCommand.None)]
   [InlineData('\u2191', true, false, true, false, false, ReaderCommand.None)]
   [InlineData('\u2193', true, true, false, false, false, ReaderCommand.None)]
   [InlineData('\u2191', true, true, true, true, false, ReaderCommand.None)]
   [InlineData('\u2193', true, true, true, false, true, ReaderCommand.None)]
   public void Resolve_CycleKeyAndFocusGuards_LeavesOtherInputAlone(char character, bool focused, bool control, bool shift, bool alt, bool windows, ReaderCommand expected)
      => Assert.Equal(expected, new ReaderKeyboard().Resolve(new ReaderKey(character, control, shift, alt, windows), focused, 0));



   /// <summary>Fixture renaming uses the real validated format, retaining every field.</summary>
   private static ReaderTheme Rename(ReaderTheme theme, string name, string? css = null)
   {
      JsonNode json = JsonNode.Parse(theme.Json)!;
      json["name"] = name;
      if(css is not null)
      {
         json["customCSS"] = css;
      }
      return ReaderTheme.Parse(json.ToJsonString());
   }
}
