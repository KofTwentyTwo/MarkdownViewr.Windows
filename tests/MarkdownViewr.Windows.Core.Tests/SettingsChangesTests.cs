/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Editors;
using MarkdownViewr.Windows.Core.Printing;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Independent document windows cannot overwrite sibling changes by saving an older preference snapshot.</summary>
public sealed class SettingsChangesTests
{
   /// <summary>Legacy remembered panel visibility does not opt new windows into showing panels, while explicit defaults survive source-generated persistence.</summary>
   [Fact]
   public void Copy_NewWindowDefaults_AreIndependentOfRememberedPanels()
   {
      var legacy = new AppSettings { ShowOutline = true, ShowSource = true };
      AppSettings restored = SettingsChanges.Copy(legacy);
      Assert.False(restored.ShowOutlineByDefault);
      Assert.False(restored.ShowSourceByDefault);
      restored.ShowOutlineByDefault = true;
      restored.ShowSourceByDefault = true;
      AppSettings selected = SettingsChanges.Copy(restored);
      Assert.True(selected.ShowOutlineByDefault);
      Assert.True(selected.ShowSourceByDefault);
      Assert.False(legacy.ShowOutlineByDefault);
      Assert.False(legacy.ShowSourceByDefault);
   }



   /// <summary>A local panel toggle cannot overwrite newer startup defaults chosen in another Settings window.</summary>
   [Fact]
   public void Merge_LocalPanelToggle_PreservesNewerStartupDefaults()
   {
      var baseline = new AppSettings();
      AppSettings local = SettingsChanges.Copy(baseline);
      local.ShowSource = true;
      local.ShowOutline = true;
      var saved = new AppSettings { ShowOutlineByDefault = true, ShowSourceByDefault = true };
      AppSettings merged = SettingsChanges.Merge(saved, baseline, local);
      Assert.True(merged.ShowOutline);
      Assert.True(merged.ShowSource);
      Assert.True(merged.ShowOutlineByDefault);
      Assert.True(merged.ShowSourceByDefault);
   }



   /// <summary>A window's source change preserves newer history, theme, editor and print choices from its sibling.</summary>
   [Fact]
   public void Merge_StaleWindow_PreservesUnchangedSiblingPreferences()
   {
      var before = new AppSettings();
      AppSettings edited = SettingsChanges.Copy(before);
      edited.ShowSource = true;
      edited.SourceWidth = 280;
      var current = new AppSettings
      {
         DocumentTheme = "Nord",
         RecentFiles = [Path.Combine(Path.GetTempPath(), "second.md")],
         ConfiguredEditors = [new EditorConfiguration(Guid.NewGuid(), "Sibling", "C:\\editor.exe")],
         Print = new PrintOptions("Plain HTML", PaddingPixels: 12),
      };
      AppSettings result = SettingsChanges.Merge(current, before, edited);
      Assert.True(result.ShowSource);
      Assert.Equal(280, result.SourceWidth);
      Assert.Equal("Nord", result.DocumentTheme);
      Assert.Equal(current.RecentFiles, result.RecentFiles);
      Assert.Equal(current.ConfiguredEditors, result.ConfiguredEditors);
      Assert.Equal(current.Print, result.Print);
      Assert.False(current.ShowSource);
   }



   /// <summary>An explicit current-window edit wins for that field, while a cleared history cannot reappear.</summary>
   [Fact]
   public void Merge_ExplicitEdit_AppliesOnlyChangedProperties()
   {
      var before = new AppSettings { DocumentTheme = "Nord", RecentFiles = [Path.Combine(Path.GetTempPath(), "old.md")] };
      AppSettings edited = SettingsChanges.Copy(before);
      edited.DocumentTheme = "Dracula";
      edited.UserThemes = ["owned fixture"];
      edited.Print = null!;
      var current = new AppSettings { DocumentTheme = "Light", RecentFiles = [] };
      AppSettings result = SettingsChanges.Merge(current, before, edited);
      Assert.Equal("Dracula", result.DocumentTheme);
      Assert.Equal(["owned fixture"], result.UserThemes);
      Assert.Empty(result.RecentFiles);
      Assert.Null(result.Print);
   }



   /// <summary>Nested arrays are copied; mutation of a working draft cannot alter its baseline.</summary>
   [Fact]
   public void Copy_MutableArrays_RemainsIndependent()
   {
      var before = new AppSettings { RecentFiles = [Path.Combine(Path.GetTempPath(), "original.md")], UserThemes = ["original"] };
      AppSettings copy = SettingsChanges.Copy(before);
      copy.RecentFiles[0] = "changed";
      copy.UserThemes[0] = "changed";
      Assert.EndsWith("original.md", before.RecentFiles[0], StringComparison.Ordinal);
      Assert.Equal("original", before.UserThemes[0]);
   }
}
