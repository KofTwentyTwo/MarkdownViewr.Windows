/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit;
using KofTwentyTwo.AppKit.Logging;
using KofTwentyTwo.AppKit.Settings;
using Velopack.Locators;


namespace MarkdownViewr.Windows;

/// <summary>Composes host services from the actual installed channel before windows, activation or logs access them.</summary>
internal static class AppIdentity
{
   /// <summary>Prevent beforefieldinit: edition services must initialize on first access after Velopack's startup hooks run.</summary>
   static AppIdentity()
   {
   }



   /// <summary>Velopack runs first; development packages have a distinct title, data variable and credential prefix.</summary>
   public static AppInfo Info { get; } = Core.AppIdentity.ForChannel(VelopackLocator.Current.Channel);



   /// <summary>The installed edition's settings/log root honors only its own data-directory override.</summary>
   public static AppPaths Paths { get; } = new(Info);



   /// <summary>Each edition owns its own persisted preferences and recent document list.</summary>
   public static SettingsStore<AppSettings> SettingsStore { get; } = new(Paths.SettingsFile, AppSettingsContext.Default.AppSettings);



   /// <summary>Credits link to the original macOS project while Windows support remains here.</summary>
   public static Uri OriginalRepositoryUrl => Core.AppIdentity.OriginalRepositoryUrl;



   /// <summary>The upstream author's public site.</summary>
   public static Uri OriginalAuthorWebsite => Core.AppIdentity.OriginalAuthorWebsite;



   /// <summary>Both editions report Windows bugs and requests to the same repository.</summary>
   public static Uri IssuesUrl => Core.AppIdentity.IssuesUrl;



   /// <summary>Both editions use the Windows repository's discussions.</summary>
   public static Uri SupportUrl => Core.AppIdentity.SupportUrl;



   /// <summary>Daily activity files use the edition's own path and identity prefix.</summary>
   public static IActivityLog CreateLog() => FileActivityLog.ForApp(Info, Paths);
}
