/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Logging;
using KofTwentyTwo.AppKit.WinUI;
using Microsoft.UI.Xaml;


namespace MarkdownViewr.Windows;

/// <summary>The application: installs AppKit's crash net and the saved theme, then opens independent document windows.</summary>
public partial class App : Application
{
   /// <summary>Installs the crash net and applies the saved theme; WinUI allows setting the application theme only here, before any window exists.</summary>
   public App()
   {
      InitializeComponent();
      WinUIShell.InstallCrashNet(this, () => Log);
      WinUIShell.ApplyTheme(this, AppIdentity.SettingsStore.Load().ThemeKind);
   }



   /// <summary>The app-wide activity log.</summary>
   internal static IActivityLog Log { get; } = AppIdentity.CreateLog();



   /// <summary>Logs the start and gives each literal startup path its own native document window.</summary>
   protected override void OnLaunched(LaunchActivatedEventArgs args)
   {
      Log.Info($"{AppIdentity.Info.DisplayName} started.");
      string[] paths = Environment.GetCommandLineArgs().Skip(1).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
      if(paths.Length == 0)
      {
         NewDocumentWindow();
      }
      else
      {
         foreach(string path in paths)
         {
            NewDocumentWindow(path);
         }
      }
   }
}
