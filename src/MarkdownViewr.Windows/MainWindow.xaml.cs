/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Settings;
using KofTwentyTwo.AppKit.Updates;
using KofTwentyTwo.AppKit.WinUI;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;


namespace MarkdownViewr.Windows;

/// <summary>
/// The main window: the AppKit shell (splash, About, activity log, update checks, theme)
/// around the app's content. Handlers here only route to AppKit or to view models in Core.
/// </summary>
public sealed partial class MainWindow : Window
{
   private readonly AppSettings _settings = AppIdentity.SettingsStore.Load();
   private readonly UpdateCoordinator _updates;
   private readonly DocumentBrowser _browser;
   private Task? _browserInitialization;
   private string? _documentPath;
   private bool _loaded;



   /// <summary>Sizes and brands the window, creates the document host, shows the splash, and schedules the quiet startup update check.</summary>
   public MainWindow()
   {
      InitializeComponent();
      _browser = new DocumentBrowser(DocumentView, message => StatusText.Text = message);
      Title = AppIdentity.Info.DisplayName;
      this.ResizeForDpi(1100, 760);
      this.SetMinimumSize(640, 420);
      this.SetAppIcon(AppIdentity.Info);

      _updates = new UpdateCoordinator(
         new VelopackUpdateService(AppIdentity.Info.RepositoryUrl!),
         new ContentDialogPrompter(() => Content.XamlRoot),
         AppIdentity.Info.DisplayName,
         App.Log);

      SplashToggle.IsChecked = _settings.ShowSplashScreen;
      UpdateCheckToggle.IsChecked = _settings.CheckForUpdatesOnStartup;
      (_settings.ThemeKind switch { AppTheme.Light => ThemeLight, AppTheme.Dark => ThemeDark, _ => ThemeSystem }).IsChecked = true;

      SplashOverlay.ShowOver(RootGrid, AppIdentity.Info, _settings);
      Closed += (_, _) =>
      {
         DocumentView.Close();
         LogWindow.CloseCurrent();
      };

      if(_settings.CheckForUpdatesOnStartup)
      {
         // The constructor runs on the UI thread, so the check resumes there to show dialogs.
         _ = CheckForUpdatesAfterStartupAsync();
      }
   }



   /// <summary>
   /// Waits for the splash to clear (dialogs need the window's XamlRoot), then runs the
   /// quiet startup update check, which only speaks up when an update exists.
   /// </summary>
   private async Task CheckForUpdatesAfterStartupAsync()
   {
      await Task.Delay(_settings.ShowSplashScreen ? _settings.SplashMilliseconds + 500 : 1000);
      await _updates.CheckQuietlyAsync();
   }



   /// <summary>Initializes the browser once and opens a document supplied as a literal process argument.</summary>
   private async void DocumentView_Loaded(object sender, RoutedEventArgs e)
   {
      if(_loaded)
      {
         return;
      }
      _loaded = true;
      _browserInitialization ??= _browser.InitializeAsync();
      try
      {
         await _browserInitialization;
         StatusText.Text = "Choose File → Open to read a Markdown document.";
         string[] arguments = Environment.GetCommandLineArgs();
         if(arguments.Length == 2)
         {
            await OpenDocumentAsync(arguments[1]);
         }
      }
      catch(Exception exception) when(exception is IOException or System.Runtime.InteropServices.COMException)
      {
         StatusText.Text = "The document surface could not start. Check that the Microsoft Edge WebView2 Runtime is installed.";
         App.Log.Error("The document surface could not start.", exception);
      }
   }



   /// <summary>Shows the Windows picker in this window; cancellation leaves the current document intact.</summary>
   private async void Open_Click(object sender, RoutedEventArgs e)
   {
      var picker = new FileOpenPicker();
      WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
      picker.FileTypeFilter.Add(".md");
      picker.FileTypeFilter.Add(".markdown");
      picker.FileTypeFilter.Add(".txt");
      global::Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
      if(file is not null)
      {
         await OpenDocumentAsync(file.Path);
      }
   }



   /// <summary>Reopens the selected file explicitly; automatic save watching will build on this path.</summary>
   private async void Reload_Click(object sender, RoutedEventArgs e)
   {
      if(_documentPath is not null)
      {
         await OpenDocumentAsync(_documentPath);
      }
   }



   /// <summary>Reads and parses outside the UI thread; a failed open preserves the previous document.</summary>
   private async Task OpenDocumentAsync(string path)
   {
      OpenMenuItem.IsEnabled = false;
      ReloadMenuItem.IsEnabled = false;
      try
      {
         _browserInitialization ??= _browser.InitializeAsync();
         await _browserInitialization;
         (DocumentFiles files, string html) = await Task.Run(() =>
         {
            var document = DocumentFiles.Open(path);
            return (document, new Core.Documents.MarkdownRenderer().Render(document.Markdown));
         });
         _browser.Show(files, html, Path.GetFileName(path));
         _documentPath = Path.GetFullPath(path);
         Title = Path.GetFileName(path) + " — " + AppIdentity.Info.DisplayName;
         StatusText.Text = "Opened " + Path.GetFileName(path);
         App.Log.Info("Opened a Markdown document.");
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or ArgumentException or System.Runtime.InteropServices.COMException)
      {
         StatusText.Text = "Could not open this document: " + exception.Message;
         App.Log.Warning("Could not open a Markdown document.");
      }
      finally
      {
         OpenMenuItem.IsEnabled = true;
         ReloadMenuItem.IsEnabled = _documentPath is not null;
      }
   }



   /// <summary>File, Exit.</summary>
   private void Exit_Click(object sender, RoutedEventArgs e) => Close();



   /// <summary>View, theme: applies the chosen theme to the window and saves it.</summary>
   private void Theme_Click(object sender, RoutedEventArgs e)
   {
      _settings.Theme = (string)((FrameworkElement)sender).Tag;
      WinUIShell.ApplyTheme(RootGrid, _settings.ThemeKind);
      Save();
   }



   /// <summary>View, Show splash at startup: saves the preference.</summary>
   private void Splash_Click(object sender, RoutedEventArgs e)
   {
      _settings.ShowSplashScreen = SplashToggle.IsChecked;
      Save();
   }



   /// <summary>View, Check for updates at startup: saves the preference.</summary>
   private void UpdateCheck_Click(object sender, RoutedEventArgs e)
   {
      _settings.CheckForUpdatesOnStartup = UpdateCheckToggle.IsChecked;
      Save();
   }



   /// <summary>Help, Check for updates: runs the interactive update flow.</summary>
   private async void CheckForUpdates_Click(object sender, RoutedEventArgs e) => await _updates.CheckInteractivelyAsync();



   /// <summary>Help, Activity log: opens the live log window, or brings the open one forward.</summary>
   private void ActivityLog_Click(object sender, RoutedEventArgs e) => LogWindow.ShowSingle(App.Log, AppIdentity.Info);



   /// <summary>Help, About: shows the About dialog.</summary>
   private async void About_Click(object sender, RoutedEventArgs e) => await AboutDialog.ShowAsync(AppIdentity.Info, Content.XamlRoot);



   /// <summary>Saves the settings and reports a failed save in the status line.</summary>
   private void Save()
   {
      if(!AppIdentity.SettingsStore.Save(_settings))
      {
         StatusText.Text = "Could not save settings.";
      }
   }
}
