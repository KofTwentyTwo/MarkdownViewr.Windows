/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Editors;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Proves bounded named editor settings, stable migration/order and one literal parent-folder argument.</summary>
public sealed class EditorConfigurationTests
{
   /// <summary>Corrupt labels, paths and identities cannot become a native launch choice.</summary>
   [Theory]
   [InlineData("")]
   [InlineData("   ")]
   [InlineData("bad\nname")]
   public void Sanitize_InvalidNames_RejectsChoice(string name)
   {
      var editor = new EditorConfiguration(Guid.NewGuid(), name, @"C:\Editor\editor.exe");
      Assert.False(EditorConfiguration.IsValid(editor));
      Assert.Empty(EditorConfiguration.Sanitize([editor]));
   }



   /// <summary>Valid order and folder modes survive sanitation; duplicate/empty identities and invalid choices are discarded.</summary>
   [Fact]
   public void Sanitize_StableIdentityAndOrder_PreservesValidChoices()
   {
      var first = new EditorConfiguration(Guid.NewGuid(), " File editor ", @"C:\Editor\editor.exe");
      var second = new EditorConfiguration(Guid.NewGuid(), "Folder editor", @"C:\Editor\other.exe", true);
      EditorConfiguration[] choices = EditorConfiguration.Sanitize([null, first, first, second,
         first with { Id = Guid.Empty }, first with { Id = Guid.NewGuid(), Name = new string('x', 81) },
         first with { Id = Guid.NewGuid(), Executable = "editor.cmd" }, first with { Id = Guid.NewGuid(), Name = null! }]);
      Assert.Equal(new[] { first.Id, second.Id }, choices.Select(editor => editor.Id));
      Assert.Equal("File editor", choices[0].Name);
      Assert.True(choices[1].OpensFolder);
      Assert.Empty(EditorConfiguration.Sanitize(null));
      Assert.False(EditorConfiguration.IsValid(null));
   }



   /// <summary>Large saved lists are bounded before being exposed as native commands.</summary>
   [Fact]
   public void Sanitize_TooManyEditors_BoundsList()
   {
      EditorConfiguration[] choices = EditorConfiguration.Sanitize(Enumerable.Range(0, 40).Select(index => new EditorConfiguration(Guid.NewGuid(), "Editor " + index, @"C:\Editor\editor.exe")));
      Assert.Equal(EditorConfiguration.MaximumEditors, choices.Length);
      Assert.Equal("Editor 31", choices[^1].Name);
   }



   /// <summary>Existing single-path settings migrate once; repeated sanitation preserves IDs and deletion does not resurrect a choice.</summary>
   [Fact]
   public void Sanitize_LegacyPath_MigratesStableChoiceAndAllowsRemoval()
   {
      var settings = new AppSettings { ExternalEditor = @"C:\Editor\editor.exe", ConfiguredEditors = null! };
      settings.Sanitize();
      EditorConfiguration choice = Assert.Single(settings.ConfiguredEditors);
      Assert.NotEqual(Guid.Empty, choice.Id);
      Assert.Equal("Editor", choice.Name);
      Assert.False(choice.OpensFolder);
      Assert.Equal(settings.ExternalEditor, choice.Executable);
      settings.Sanitize();
      Assert.Equal(choice.Id, Assert.Single(settings.ConfiguredEditors).Id);
      settings.ConfiguredEditors = [];
      settings.ExternalEditor = "";
      settings.Sanitize();
      Assert.Empty(settings.ConfiguredEditors);
      Assert.Empty(settings.ExternalEditor);
   }



   /// <summary>Atomic AppKit persistence retains named IDs, order and folder mode using the trimmed serializer.</summary>
   [Fact]
   public void SaveAndLoad_EditorList_PreservesIdentityOrderAndMode()
   {
      using var temporary = new TempDirectory();
      var store = new KofTwentyTwo.AppKit.Settings.SettingsStore<AppSettings>(temporary.File("settings.json"), AppSettingsContext.Default.AppSettings);
      var first = new EditorConfiguration(Guid.NewGuid(), "Folder editor", @"C:\Editor\folder.exe", true);
      var second = new EditorConfiguration(Guid.NewGuid(), "File editor", @"C:\Editor\file.exe");
      Assert.True(store.Save(new AppSettings { ConfiguredEditors = [first, second], ExternalEditor = @"C:\Other\old.exe" }));
      AppSettings loaded = store.Load();
      Assert.Equal(new[] { first, second }, loaded.ConfiguredEditors);
      Assert.Equal(first.Executable, loaded.ExternalEditor);
      loaded.ConfiguredEditors = [];
      loaded.ExternalEditor = "";
      Assert.True(store.Save(loaded));
      Assert.Empty(store.Load().ConfiguredEditors);
   }



   /// <summary>Folder mode supplies one literal parent directory, preserving punctuation and Unicode without shell execution.</summary>
   [Theory]
   [InlineData(@"C:\日本語 & $HOME\notes.md", @"C:\日本語 & $HOME")]
   [InlineData(@"C:\notes.md", @"C:\")]
   [InlineData(@"\\server\share\notes.md", @"\\server\share")]
   public void Create_FolderMode_PassesOneLiteralParent(string document, string parent)
   {
      System.Diagnostics.ProcessStartInfo start = EditorLaunch.Create(@"C:\Editor\editor.exe", document, opensFolder: true);
      Assert.Equal(parent, Assert.Single(start.ArgumentList));
      Assert.Equal(parent, start.WorkingDirectory);
      Assert.False(start.UseShellExecute);
      Assert.Empty(start.Arguments);
   }



   /// <summary>A root directory cannot masquerade as a file when deriving its parent.</summary>
   [Theory]
   [InlineData(@"C:\")]
   [InlineData(@"\\server\share\")]
   public void Create_DirectoryRoot_RejectsDocument(string document)
      => Assert.Throws<ArgumentException>(() => EditorLaunch.Create(@"C:\Editor\editor.exe", document, opensFolder: true));
}
