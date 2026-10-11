/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Settings;
using KofTwentyTwo.AppKit.Updates;
using KofTwentyTwo.AppKit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;


namespace MarkdownViewr.Windows;

/// <summary>
/// The main window: the AppKit shell (splash, About, activity log, update checks, theme)
/// around the app's content. Handlers here only route to AppKit or to view models in Core.
/// </summary>
public sealed partial class MainWindow : Window, IDisposable
{
   private readonly AppSettings _settings = AppIdentity.SettingsStore.Load();
   private readonly UpdateCoordinator _updates;
   private readonly DocumentBrowser _browser;
   private Task? _browserInitialization;
   private string? _documentPath;
   private bool _loaded;
   private bool _closed;
   private readonly Core.Documents.DocumentLoadScheduler _loads = new();
   private DocumentWatcher? _watcher;
   private IReadOnlyList<Core.Themes.ReaderTheme> _themes = [];



   /// <summary>Sizes and brands the window, creates the document host, shows the splash, and schedules the quiet startup update check.</summary>
   public MainWindow()
   {
      InitializeComponent();
      _themes = Core.Themes.ThemeCatalog.Combine(_settings.UserThemes.Select(Core.Themes.ReaderTheme.Parse));
      _browser = new DocumentBrowser(DocumentView, message => StatusText.Text = message, ActiveDocumentTheme());
      CreateDocumentThemeMenu();
      InitializeOutline();
      InitializeSource();
      InitializeFind();
      InitializeKeyboard();
      InitializeZoom();
      InitializePrinting();
      RefreshEditorCommands();
      InitializeInputDiagnostics();
      (_settings.FrontmatterKind switch { Core.Documents.FrontmatterMode.Metadata => FrontmatterMetadata, Core.Documents.FrontmatterMode.Markdown => FrontmatterMarkdown, _ => FrontmatterHide }).IsChecked = true;
      Title = AppIdentity.Info.DisplayName;
      FitInitialWindow();
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
      Closed += (_, _) => Dispose();

      if(_settings.CheckForUpdatesOnStartup)
      {
         // The constructor runs on the UI thread, so the check resumes there to show dialogs.
         _ = CheckForUpdatesAfterStartupAsync();
      }
   }



   /// <summary>Keeps the full initial window and status line inside the current monitor's working area.</summary>
   private void FitInitialWindow()
   {
      global::Windows.Graphics.RectInt32 area = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary).WorkArea;
      double scale = this.DpiScale();
      int width = Math.Min((int)(1100 * scale), area.Width);
      int height = Math.Min((int)(760 * scale), area.Height);
      if(AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
      {
         presenter.PreferredMinimumWidth = Math.Min((int)(640 * scale), area.Width);
         presenter.PreferredMinimumHeight = Math.Min((int)(420 * scale), area.Height);
      }
      AppWindow.MoveAndResize(new global::Windows.Graphics.RectInt32
      {
         X = area.X + (area.Width - width) / 2,
         Y = area.Y + (area.Height - height) / 2,
         Width = width,
         Height = height,
      });
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
         if(_closed)
         {
            return;
         }
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



   /// <summary>Reopens the selected file explicitly and retains the current scroll position.</summary>
   private async void Reload_Click(object sender, RoutedEventArgs e)
   {
      if(_documentPath is not null)
      {
         await OpenDocumentAsync(_documentPath, reload: true);
      }
   }



   /// <summary>Reads and parses outside the UI thread; a failed open preserves the previous document.</summary>
   private async Task OpenDocumentAsync(string path, bool reload = false)
   {
      if(_closed)
      {
         return;
      }
      if(!_loads.TryBegin(path, reload))
      {
         return;
      }
      OpenMenuItem.IsEnabled = false;
      ReloadMenuItem.IsEnabled = false;
      FrontmatterMenu.IsEnabled = false;
      try
      {
         _browserInitialization ??= _browser.InitializeAsync();
         await _browserInitialization;
         Core.Documents.DocumentViewport viewport = reload ? await _browser.CaptureViewportAsync() : default;
         (DocumentFiles files, Core.Documents.DocumentOutline outline) = await ReadDocumentAsync(path, _settings.FrontmatterKind);
         if(_closed)
         {
            return;
         }
         ShowDocument(files, outline, path, viewport, reload);
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or ArgumentException or System.Runtime.InteropServices.COMException)
      {
         App.Log.Warning(FormattableString.Invariant($"Could not open a Markdown document. Reload={reload}; exception={exception.GetType().Name}; HRESULT=0x{exception.HResult:X8}; UIThread={DispatcherQueue.HasThreadAccess}."));
         if(!_closed)
         {
            StatusText.Text = reload && exception is FileNotFoundException
               ? "The document is missing. Showing the last version; it will reload when restored."
               : "Could not open this document: " + exception.Message;
         }

      }
      finally
      {
         (string Path, bool Reload)? next = _loads.Complete(_documentPath);
         if(!_closed)
         {
            if(next is { } request)
            {
               await OpenDocumentAsync(request.Path, request.Reload);
            }
            else
            {
               OpenMenuItem.IsEnabled = true;
               FrontmatterMenu.IsEnabled = true;
               ReloadMenuItem.IsEnabled = _documentPath is not null;
            }
         }
      }
   }



   /// <summary>Directory watching survives atomic replacement and file recreation; callbacks only enqueue UI work.</summary>
   private void StartWatching()
   {
      _watcher?.Dispose();
      _watcher = null;
      string watchedPath = _documentPath!;
      try
      {
         _watcher = new DocumentWatcher(
            _documentPath!,
            () => DispatcherQueue.TryEnqueue(() =>
            {
               if(string.Equals(_documentPath, watchedPath, StringComparison.Ordinal))
               {
                  _ = OpenDocumentAsync(watchedPath, reload: true);
               }
            }),
            () => DispatcherQueue.TryEnqueue(() =>
            {
               if(!_closed)
               {
                  StatusText.Text = "Live reload encountered a filesystem error. Use File → Reload if changes stop appearing.";
                  App.Log.Warning("The document watcher reported a filesystem error.");
               }
            }));
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or ArgumentException)
      {
         StatusText.Text = "Live reload is unavailable for this folder. Use File → Reload.";
         App.Log.Warning("Could not start the document watcher.");
      }
   }



   /// <summary>Retries only transient missing/sharing violations from an editor's replace/save operation.</summary>
   private static async Task<(DocumentFiles Files, Core.Documents.DocumentOutline Outline)> ReadDocumentAsync(string path, Core.Documents.FrontmatterMode frontmatter)
   {
      for(int attempt = 0; ; attempt++)
      {
         try
         {
            return await Task.Run(() =>
            {
               var document = DocumentFiles.Open(path);
               return (document, new Core.Documents.DocumentOutline(document.Markdown, frontmatter));
            });
         }
         catch(IOException exception) when(attempt < 3 && (exception.HResult & 0xFFFF) is 2 or 3 or 32 or 33)
         {
            await Task.Delay(200);
         }
      }
   }



   /// <summary>Commits a fully parsed snapshot and changes the watcher only when the selected document changes.</summary>
   private void ShowDocument(DocumentFiles files, Core.Documents.DocumentOutline outline, string path, Core.Documents.DocumentViewport viewport, bool reload)
   {
      _browser.Show(files, outline.Html, Path.GetFileName(path), viewport, outline.Headings);
      _find = null;
      _findGeneration++;
      _documentKeyboard?.Reset();
      _sourceMarkdown = files.Markdown;
      _outline = outline;
      _sourceDocument = new Core.Documents.DocumentSourceText(_sourceMarkdown, outline);
      RefreshSource();
      RefreshOutline();
      _documentPath = Path.GetFullPath(path);
      RefreshEditorCommands();
      Title = Path.GetFileName(path) + " — " + AppIdentity.Info.DisplayName;
      StatusText.Text = (reload ? "Reloaded " : "Opened ") + Path.GetFileName(path);
      App.Log.Info(reload ? "Reloaded a Markdown document." : "Opened a Markdown document.");
      if(!reload)
      {
         StartWatching();
      }
   }



   /// <summary>Closing the window releases its browser and directory subscription before any queued work can run.</summary>
   public void Dispose()
   {
      if(!_closed)
      {
         _closed = true;
         _findGeneration++;
         _findDelay?.Stop();
         _watcher?.Dispose();
         _watcher = null;
         _browser.Close();
         LogWindow.CloseCurrent();
      }
      GC.SuppressFinalize(this);
   }



   /// <summary>File, Exit.</summary>
   private void Exit_Click(object sender, RoutedEventArgs e) => Close();



   /// <summary>Builds the original document theme choices without mixing them with the AppKit shell theme.</summary>
   private void CreateDocumentThemeMenu()
   {
      DocumentThemesMenu.Items.Clear();
      string selected = ActiveDocumentTheme().Name;
      foreach(Core.Themes.ReaderTheme theme in _themes)
      {
         var item = new RadioMenuFlyoutItem { Text = theme.Name, Tag = theme, GroupName = "DocumentTheme", IsChecked = string.Equals(selected, theme.Name, StringComparison.Ordinal) };
         Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(item, "DocumentTheme:" + theme.Name);
         item.Click += DocumentTheme_Click;
         DocumentThemesMenu.Items.Add(item);
      }
   }



   /// <summary>Persists the document palette and redisplays the current snapshot at its reading position.</summary>
   private async void DocumentTheme_Click(object sender, RoutedEventArgs e)
   {
      var theme = (Core.Themes.ReaderTheme)((FrameworkElement)sender).Tag;
      _settings.DocumentTheme = theme.Name;
      Save();
      await ApplyDocumentThemeAsync(theme);
   }



   /// <summary>Persists frontmatter appearance and rereads the active snapshot at its reading position.</summary>
   private async void Frontmatter_Click(object sender, RoutedEventArgs e)
   {
      _settings.Frontmatter = (string)((FrameworkElement)sender).Tag;
      Save();
      if(_documentPath is not null)
      {
         await OpenDocumentAsync(_documentPath, reload: true);
      }
   }



   /// <summary>Resolves user overrides and safely falls back after a saved custom theme is removed.</summary>
   private Core.Themes.ReaderTheme ActiveDocumentTheme()
      => _themes.FirstOrDefault(theme => string.Equals(theme.Name, _settings.DocumentTheme, StringComparison.Ordinal))
         ?? Core.Themes.ThemeCatalog.Find(null);



   /// <summary>A same-name edit overrides the original; a new name creates a custom theme.</summary>
   private async void EditDocumentTheme_Click(object sender, RoutedEventArgs e)
   {
      Core.Themes.ReaderTheme? edited = await ThemeEditor.ShowAsync(Content.XamlRoot, ActiveDocumentTheme());
      if(edited is null || _closed)
      {
         return;
      }
      _settings.UserThemes = _settings.UserThemes
         .Where(json => !string.Equals(Core.Themes.ReaderTheme.Parse(json).Name, edited.Name, StringComparison.Ordinal))
         .Append(edited.Json).ToArray();
      _settings.DocumentTheme = edited.Name;
      Save();
      RefreshDocumentThemes();
      await ApplyDocumentThemeAsync(edited);
   }



   /// <summary>Restores original palettes and removes their overrides, retaining separately named custom themes.</summary>
   private async void RestoreDocumentThemes_Click(object sender, RoutedEventArgs e)
   {
      var dialog = new ContentDialog { XamlRoot = Content.XamlRoot, Title = "Restore built-in themes?", Content = "Edits to the 20 built-in themes will be removed. Your separately named custom themes will be kept.", PrimaryButtonText = "Restore themes", CloseButtonText = "Cancel" };
      if(await dialog.ShowAsync() != ContentDialogResult.Primary || _closed)
      {
         return;
      }
      var builtIn = Core.Themes.ThemeCatalog.BuiltIn.Select(theme => theme.Name).ToHashSet(StringComparer.Ordinal);
      _settings.UserThemes = _settings.UserThemes.Where(json => !builtIn.Contains(Core.Themes.ReaderTheme.Parse(json).Name)).ToArray();
      Save();
      RefreshDocumentThemes();
      await ApplyDocumentThemeAsync(ActiveDocumentTheme());
   }



   /// <summary>Rebuilds the visible catalog after a successful preference edit.</summary>
   private void RefreshDocumentThemes()
   {
      _themes = Core.Themes.ThemeCatalog.Combine(_settings.UserThemes.Select(Core.Themes.ReaderTheme.Parse));
      CreateDocumentThemeMenu();
   }



   /// <summary>Applies a theme without allowing browser errors to escape a native event handler.</summary>
   private async Task ApplyDocumentThemeAsync(Core.Themes.ReaderTheme theme)
   {
      try
      {
         _browserInitialization ??= _browser.InitializeAsync();
         await _browserInitialization;
         await _browser.ApplyThemeAsync(theme);
      }
      catch(System.Runtime.InteropServices.COMException exception)
      {
         if(!_closed)
         {
            StatusText.Text = "The document theme could not be displayed.";
            App.Log.Error("The document theme could not be displayed.", exception);
         }
      }
   }


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
