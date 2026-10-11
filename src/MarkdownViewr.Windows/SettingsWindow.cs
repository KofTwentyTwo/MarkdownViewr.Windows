/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.WinUI;
using MarkdownViewr.Windows.Core.Themes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;


namespace MarkdownViewr.Windows;

/// <summary>A normal resizable WinUI settings window with adaptive navigation and independently scrolling native pages.</summary>
internal sealed partial class SettingsWindow : Window
{
   private readonly Grid _root = new();
   private readonly NavigationView _navigation = new() { PaneDisplayMode = NavigationViewPaneDisplayMode.Auto, ExpandedModeThresholdWidth = 840, CompactModeThresholdWidth = 640, IsSettingsVisible = false, IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed, OpenPaneLength = 220, AlwaysShowHeader = true };
   private readonly Dictionary<string, (string Title, UIElement Page)> _pages = new(StringComparer.Ordinal);
   private readonly ScrollViewer _pageScroll = new() { Padding = new Thickness(24, 12, 24, 24), HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
   private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(24, 8, 24, 12) };
   private readonly ReadingAppearanceEditor _appearance;
   private readonly Func<ReadingAppearance, Task<bool>> _saveAppearance;
   private readonly Func<string, Task> _runAction;
   private string _savedCss;
   private bool _busy;
   private bool _closed;
   private bool _updating;



   /// <summary>Settings is a separate window; routine layout saves immediately and advanced CSS has an explicit Apply action.</summary>
   public SettingsWindow(Core.AppSettings current, Func<ReadingAppearance, Task<bool>> saveAppearance, Func<bool, int, bool, bool, bool, bool> saveStartup, Func<bool, bool, bool> saveContents, Func<Core.Printing.PrintOptions, bool> savePrint, IEnumerable<ReaderTheme> themes, Func<string, Task> runAction)
   {
      Title = AppIdentity.Info.DisplayName + " — Settings";
      SystemBackdrop = new MicaBackdrop();
      _appearance = new ReadingAppearanceEditor(current.ReadingAppearance);
      _savedCss = current.GlobalCss;
      _saveStartup = saveStartup;
      _saveContents = saveContents;
      _savePrint = savePrint;
      _savedPrint = current.Print;
      _printEditor = new PrintOptionsEditor(current.Print, themes);
      _saveAppearance = saveAppearance;
      _runAction = runAction;
      _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
      _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      Grid.SetRow(_editActions, 1);
      _root.Children.Add(_editActions);
      _root.Children.Add(_navigation);
      Grid.SetRow(_status, 2);
      AutomationProperties.SetAutomationId(_root, "SettingsWindowRoot");
      AutomationProperties.SetName(_root, "Settings");
      AutomationProperties.SetAutomationId(_status, "SettingsStatus");
      AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite);
      _root.Children.Add(_status);
      Content = _root;
      AddPage("SettingsAppearanceTab", "General", Symbol.Home, AppearancePage(current));
      AddPage("SettingsThemesTab", "Document themes", Symbol.Library, ThemesPage());
      AddPage("SettingsEditorsTab", "Editors", Symbol.Edit, EditorsPage());
      AddPage("SettingsPrintingTab", "Printing", Symbol.Print, PrintingPage());
      AddPage("SettingsAdvancedTab", "Advanced", Symbol.Setting, AdvancedPage(current));
      _navigation.Content = _pageScroll;
      _navigation.SelectionChanged += (_, args) => SelectPage((string)((NavigationViewItem)args.SelectedItem).Tag);
      _navigation.SelectedItem = _navigation.MenuItems[0];
      _appearance.LayoutChanged += async (_, _) => await SaveAppearanceAsync(includeCss: false);
      _navigation.BackRequested += (_, _) => EndEdit(ContentDialogResult.None);
      Closed += (_, _) =>
      {
         _closed = true;
         EndEdit(ContentDialogResult.None);
      };
      this.SetAppIcon(AppIdentity.Info);
      FitWindow();
   }



   /// <summary>Uses normal Windows resizing while keeping navigation and settings controls within the monitor work area.</summary>
   private void FitWindow()
   {
      double scale = this.DpiScale();
      global::Windows.Graphics.RectInt32 area = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary).WorkArea;
      AppWindow.Resize(new global::Windows.Graphics.SizeInt32(Math.Min((int)(960 * scale), area.Width), Math.Min((int)(720 * scale), area.Height)));
      if(AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
      {
         presenter.PreferredMinimumWidth = Math.Min((int)(640 * scale), area.Width);
         presenter.PreferredMinimumHeight = Math.Min((int)(420 * scale), area.Height);
      }
   }



   /// <summary>The current settings XamlRoot owns theme/editor dialogs, rather than the reading window beneath it.</summary>
   public XamlRoot DialogRoot => _root.XamlRoot;



   /// <summary>Reader broadcasts refresh saved fields and CSS baselines while retaining unsubmitted CSS drafts.</summary>
   public void UpdateAppearance(ReadingAppearance appearance)
   {
      if(_closed)
      {
         return;
      }
      _updating = true;
      try
      {
         bool preserveCss = !string.Equals(_appearance.CssDraft, _savedCss, StringComparison.Ordinal);
         _appearance.Update(appearance, preserveCss);
         _savedCss = appearance.GlobalCss;
      }
      finally
      {
         _updating = false;
      }
   }



   /// <summary>Pages use native NavigationView items and ordinary scrolling, with no nested tab or custom modal-shell behavior.</summary>
   private void AddPage(string identity, string title, Symbol icon, UIElement content)
   {
      var item = new NavigationViewItem { Content = title, Icon = new SymbolIcon(icon), Tag = identity };
      AutomationProperties.SetAutomationId(item, identity);
      if(string.Equals(identity, "SettingsThemesTab", StringComparison.Ordinal))
      {
         item.Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE790" };
      }
      _navigation.MenuItems.Add(item);
      _pages.Add(identity, (title, content));
   }



   /// <summary>Category switches retain deliberate CSS drafts and keyboard-accessible fields within the same live window.</summary>
   private void SelectPage(string identity)
   {
      (string title, UIElement page) = _pages[identity];
      _navigation.Header = title;
      _pageScroll.Content = null;
      _pageScroll.Content = page;
      _pageScroll.ChangeView(null, 0, null, disableAnimation: true);
   }



   /// <summary>Native layout controls save their validated changes; unsubmitted CSS stays a separate draft.</summary>
   private StackPanel AppearancePage(Core.AppSettings current)
   {
      var page = new StackPanel { Spacing = 16, MaxWidth = 720, HorizontalAlignment = HorizontalAlignment.Stretch };
      AddDefaultFileTypes(page);
      AddContentsPreferences(page, current);
      page.Children.Add(_appearance.Fields);
      var apply = new Button { Content = "Apply custom CSS", HorizontalAlignment = HorizontalAlignment.Left };
      AutomationProperties.SetAutomationId(apply, "SettingsApplyCss");
      apply.Click += async (_, _) => await SaveAppearanceAsync(includeCss: true);
      page.Children.Add(apply);
      return page;
   }



   /// <summary>A validated save reports persistence failure and never applies an unsaved document appearance.</summary>
   private async Task SaveAppearanceAsync(bool includeCss)
   {
      if(_closed || _busy || _updating)
      {
         return;
      }
      ReadingAppearance? draft = includeCss ? _appearance.Read() : _appearance.ReadLayout(_savedCss);
      if(draft is null)
      {
         return;
      }
      _busy = true;
      _appearance.SetEnabled(false);
      try
      {
         if(await _saveAppearance(draft) && !_closed)
         {
            _status.Text = "Saved.";
         }
         else if(!_closed)
         {
            _status.Text = "Could not save the setting. Please try again.";
         }
      }
      catch(Exception exception)
      {
         ReportError(exception);
      }
      finally
      {
         _busy = false;
         if(!_closed)
         {
            _appearance.SetEnabled(true);
         }
      }
   }



   /// <summary>Errors disclose no document paths or authored CSS, and stay visible in the owned settings window.</summary>
   private void ReportError(Exception exception)
   {
      if(!_closed)
      {
         _status.Text = "The setting could not be changed. Please try again.";
         App.Log.Warning(FormattableString.Invariant($"Settings could not be completed ({exception.GetType().Name}, 0x{exception.HResult:X8})."));
      }
   }
}
