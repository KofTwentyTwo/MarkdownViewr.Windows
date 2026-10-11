/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Logging;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Tests of the app's identity and the services derived from it.</summary>
public class AppIdentityTests
{
   /// <summary>Info: the id and display name are the app's.</summary>
   [Fact]
   public void Info_IdentifiesTheApp()
   {
      Assert.Equal("markdownviewr-windows", AppIdentity.Info.Id);
      Assert.Equal("MarkdownViewr", AppIdentity.Info.DisplayName);
      Assert.EndsWith("_DATA_DIR", AppIdentity.Info.DataDirectoryVariable, StringComparison.Ordinal);
      Assert.NotNull(AppIdentity.Info.RepositoryUrl);
      Assert.NotEmpty(AppIdentity.Info.Attributions);
      Assert.DoesNotContain("Darin", AppIdentity.Info.Description, StringComparison.Ordinal);
      Assert.Equal("https://github.com/darinkelkhoff/markdownViewr", AppIdentity.OriginalRepositoryUrl.AbsoluteUri);
      Assert.Equal("https://kelkhoff.com/", AppIdentity.OriginalAuthorWebsite.AbsoluteUri);
      Assert.DoesNotContain(AppIdentity.Info.Attributions, credit => credit.Name.Contains("Darin Kelkhoff", StringComparison.Ordinal));
      Assert.Equal(AppIdentity.Info.RepositoryUrl!.AbsoluteUri.TrimEnd('/') + "/issues/new/choose", AppIdentity.IssuesUrl.AbsoluteUri);
      Assert.Equal(AppIdentity.Info.RepositoryUrl.AbsoluteUri.TrimEnd('/') + "/discussions", AppIdentity.SupportUrl.AbsoluteUri);
   }



   /// <summary>Paths and SettingsStore: settings live in the app's data folder.</summary>
   [Fact]
   public void SettingsStore_UsesTheAppsSettingsFile()
   {
      Assert.Equal(AppIdentity.Paths.SettingsFile, AppIdentity.SettingsStore.Path);
      Assert.EndsWith("settings.json", AppIdentity.SettingsStore.Path, StringComparison.Ordinal);
   }



   /// <summary>CreateLog: writes daily files in the app's logs folder, named after the id.</summary>
   [Fact]
   public void CreateLog_WritesToTheAppsLogsFolder()
   {
      IActivityLog log = AppIdentity.CreateLog();
      Assert.Equal(AppIdentity.Paths.LogsDirectory, log.LogDirectory);
      Assert.StartsWith("markdownviewr-windows-", Path.GetFileName(log.CurrentLogFilePath), StringComparison.Ordinal);
   }
}



/// <summary>Tests of the app's settings.</summary>
public class AppSettingsTests
{
   /// <summary>Sanitize: invalid shell preferences fall back to their supported defaults.</summary>
   [Fact]
   public void Sanitize_RepairsShellSettings()
   {
      var settings = new AppSettings { Theme = "Purple" };
      settings.Sanitize();
      Assert.Equal("System", settings.Theme);
   }



   /// <summary>Round trip: the shell theme survives a save and a load.</summary>
   [Fact]
   public void SaveAndLoad_KeepsTheTheme()
   {
      using var temp = new TempDirectory();
      var store = new KofTwentyTwo.AppKit.Settings.SettingsStore<AppSettings>(temp.File("settings.json"), AppSettingsContext.Default.AppSettings);
      Assert.True(store.Save(new AppSettings { Theme = "Dark", DocumentTheme = "GitHub Light", Frontmatter = "Metadata", UserThemes = [Themes.ThemeCatalog.Find("GitHub Light").Json] }));
      Assert.Equal("Dark", store.Load().Theme);
      Assert.Equal("GitHub Light", store.Load().DocumentTheme);
      Assert.Equal(Documents.FrontmatterMode.Metadata, store.Load().FrontmatterKind);
      Assert.Equal(Themes.ThemeCatalog.Find("GitHub Light").Json, Assert.Single(store.Load().UserThemes));
   }
}
