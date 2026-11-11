/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit;
using KofTwentyTwo.AppKit.Logging;
using KofTwentyTwo.AppKit.Settings;


namespace MarkdownViewr.Windows.Core;

/// <summary>
/// The application's identity, declared once and shared by the window, the command line,
/// and the tests. AppKit derives the data folder (%LOCALAPPDATA%\markdownviewr-windows), the data-folder
/// override variable, the log file names, and the credential prefix from <see cref="Info"/>.
/// </summary>
public static class AppIdentity
{
   /// <summary>Who the app is: id, names, links, credits, and brand.</summary>
   public static AppInfo Info { get; } = new()
   {
      Id = "markdownviewr-windows",
      DisplayName = "MarkdownViewr",
      Tagline = "A Windows Markdown reader",
      Description = "A Windows Markdown viewer with live reload, themes and document navigation.",
      RepositoryUrl = new Uri("https://git.kof22.com/KofTwentyTwo/MarkdownViewr.Windows"),
      Author = "James Maes",
      Copyright = "Copyright (c) 2026 James Maes",
      // Keep the gradient in step with the mark (regenerate both with AppKit's BrandTool).
      Brand = new AppBrand
      {
         GradientStart = "#1A2B4D",
         GradientEnd = "#0C1836",
         IconSvgPath = "Assets/Brand/app-icon.svg",
         IconIcoPath = "Assets/Brand/app.ico",
      },
      Attributions = AboutComponents.Runtime.Select(component => new Attribution(component.Name, component.License)).ToArray(),
   };



   /// <summary>Installed package channels select a separate development identity; loose builds retain the stable identity.</summary>
   public static AppInfo ForChannel(string? channel) => string.Equals(channel, "dev", StringComparison.Ordinal)
      ? Info with { Id = "markdownviewr-windows-dev", DisplayName = "MarkdownViewr (dev)" }
      : Info;



   /// <summary>Where the app keeps its per-user data; honors the data-folder override variable.</summary>
   public static AppPaths Paths { get; } = new(Info);



   /// <summary>Darin Kelkhoff's original macOS viewer, credited without changing Windows support destinations.</summary>
   public static Uri OriginalRepositoryUrl { get; } = new("https://github.com/darinkelkhoff/markdownViewr");



   /// <summary>The original author's website, verified against his GitHub profile.</summary>
   public static Uri OriginalAuthorWebsite { get; } = new("https://kelkhoff.com/");



   /// <summary>The app's bug/feature templates, independent of any upstream project credited in About.</summary>
   public static Uri IssuesUrl { get; } = RepositoryPage("/issues/new/choose");



   /// <summary>Questions already asked and problems already reported in the app's own repository.</summary>
   public static Uri SupportUrl { get; } = RepositoryPage("/issues");



   /// <summary>The newest stable release, with its installers and portable archive.</summary>
   public static Uri LatestReleaseUrl { get; } = RepositoryPage("/releases/latest");



   /// <summary>The installer of the newest stable release. The file name is the one the release packaging gives the stable edition's Setup.</summary>
   public static Uri InstallerUrl { get; } = RepositoryPage("/releases/download/latest/" + Info.Id + "-stable-Setup.exe");



   /// <summary>The user guide as published on the main branch.</summary>
   public static Uri UserGuideUrl { get; } = RepositoryPage("/src/branch/main/docs/user-guide.md");



   /// <summary>The app's own license as published on the main branch.</summary>
   public static Uri LicenseUrl { get; } = RepositoryPage("/src/branch/main/LICENSE");



   /// <summary>A page of the app's repository, given its path below the repository root.</summary>
   private static Uri RepositoryPage(string path) => new(Info.RepositoryUrl!.AbsoluteUri.TrimEnd('/') + path);



   /// <summary>Loads and saves <see cref="AppSettings"/> at <see cref="AppPaths.SettingsFile"/>.</summary>
   public static SettingsStore<AppSettings> SettingsStore { get; } = new(Paths.SettingsFile, AppSettingsContext.Default.AppSettings);



   /// <summary>Creates the app's daily activity log under <see cref="AppPaths.LogsDirectory"/>.</summary>
   public static IActivityLog CreateLog() => FileActivityLog.ForApp(Info, Paths);
}
