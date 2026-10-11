/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Themes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>A copied native theme-list draft cannot alter saved choices until the owner presses Save.</summary>
internal sealed class ThemeManagementDialog
{
   private AppSettings _draft;
   private readonly ListBox _list = new() { Name = "ThemeList", Height = 360 };
   private readonly CheckBox _enabled = new() { Name = "ThemeListEnabled", Content = "Enabled in theme menu and cycling" };
   private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
   private readonly Grid _content;
   private SettingsWindow? _host;
   private string? _selected;
   private bool _loading;
   private readonly Microsoft.UI.WindowId _owner;
   private ManagerAction _action;
   private ReaderTheme? _requestedTheme;



   /// <summary>The manager includes disabled choices and keeps deletion/restoration inside the cancel-safe draft.</summary>
   public ThemeManagementDialog(AppSettings settings, Microsoft.UI.WindowId owner)
   {
      _draft = SettingsChanges.Copy(settings);
      _owner = owner;
      var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
      actions.Children.Add(ActionButton("ThemeListUp", "Move up", () => ChangeSelected(up: true)));
      actions.Children.Add(ActionButton("ThemeListDown", "Move down", () => ChangeSelected(up: false)));
      actions.Children.Add(ActionButton("ThemeListDelete", "Delete", DeleteSelected));
      var panel = new StackPanel { Spacing = 8 };
      panel.Children.Add(new TextBlock { Text = "Disabled themes stay editable. Deleted built-ins can be restored. Save commits these changes; Cancel discards them.", TextWrapping = TextWrapping.Wrap });
      panel.Children.Add(_enabled);
      var editActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
      editActions.Children.Add(ActionButton("ThemeListNew", "New", () => RequestEdit(ThemeManagement.NewTheme(_draft))));
      editActions.Children.Add(ActionButton("ThemeListCopy", "Copy", () => EditSelected(copy: true)));
      editActions.Children.Add(ActionButton("ThemeListEdit", "Edit", () => EditSelected(copy: false)));
      editActions.Children.Add(ActionButton("ThemeListDefault", "Use selected", UseSelected));
      panel.Children.Add(editActions);
      var transfers = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
      transfers.Children.Add(ActionButton("ThemeListImport", "Import...", () => RequestTransfer(ManagerAction.Import)));
      transfers.Children.Add(ActionButton("ThemeListExport", "Export...", () => RequestTransfer(ManagerAction.Export)));
      panel.Children.Add(transfers);
      panel.Children.Add(actions);
      panel.Children.Add(ActionButton("ThemeListRestore", "Restore built-ins", Restore));
      panel.Children.Add(_status);
      _content = new Grid { ColumnSpacing = 16, RowSpacing = 16 };
      _content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
      _content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
      _content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      _content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      _content.Children.Add(_list);
      Grid.SetColumn(panel, 1);
      _content.Children.Add(panel);
      _content.SizeChanged += (_, _) =>
      {
         bool narrow = _content.ActualWidth < 600;
         _content.ColumnDefinitions[0].Width = narrow ? new GridLength(1, GridUnitType.Star) : new GridLength(220);
         _content.ColumnDefinitions[1].Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
         Grid.SetColumn(panel, narrow ? 0 : 1);
         Grid.SetRow(panel, narrow ? 1 : 0);
      };
      _list.SelectionChanged += (_, _) => Select();
      _enabled.Checked += (_, _) => SetEnabled(true);
      _enabled.Unchecked += (_, _) => SetEnabled(false);
      Refresh(ThemeManagement.Active(_draft).Name);
   }



   /// <summary>Only explicit Save returns the copied theme state to the owning window.</summary>
   public async Task<AppSettings?> ShowAsync(SettingsWindow host, Func<bool> isOpen)
   {
      _host = host;
      while(isOpen() && host.IsOpen)
      {
         _action = ManagerAction.None;
         if(await host.ShowEditorAsync("Manage document themes", _content, "Save themes") == ContentDialogResult.Primary)
         {
            return _draft;
         }
         if(!isOpen() || _action == ManagerAction.None)
         {
            return null;
         }
         try
         {
            await RunActionAsync(host.DialogRoot);
         }
         catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException or ObjectDisposedException or InvalidOperationException or ArgumentException)
         {
            _status.Text = "The selected theme action could not be completed. Your draft is retained.";
            App.Log.Warning(FormattableString.Invariant($"A native theme action could not be completed ({exception.GetType().Name}, 0x{exception.HResult:X8})."));
         }
         if(isOpen())
         {
            Refresh(_selected);
         }
      }
      return null;
   }



   /// <summary>Native action identities are available to accessibility and real acceptance tests.</summary>
   private static Button ActionButton(string name, string text, Action action)
   {
      var button = new Button { Name = name, Content = text };
      button.Click += (_, _) => action();
      return button;
   }



   /// <summary>Shows saved order and enabled state while retaining selection through moves.</summary>
   private void Refresh(string? selected)
   {
      _loading = true;
      _list.Items.Clear();
      foreach(ReaderTheme theme in ThemeManagement.All(_draft))
      {
         var item = new ListBoxItem { Content = theme.Name, Tag = theme.Name };
         AutomationProperties.SetAutomationId(item, "ThemeChoice:" + theme.Name);
         _list.Items.Add(item);
         if(string.Equals(selected, theme.Name, StringComparison.Ordinal))
         {
            _list.SelectedItem = item;
         }
      }
      if(_list.SelectedItem is null && _list.Items.Count > 0)
      {
         _list.SelectedIndex = 0;
      }
      _loading = false;
      Select();
   }



   /// <summary>Selection changes update only native draft controls.</summary>
   private void Select()
   {
      if(_loading)
      {
         return;
      }
      _loading = true;
      _selected = (_list.SelectedItem as ListBoxItem)?.Tag as string;
      _enabled.IsEnabled = _selected is not null;
      _enabled.IsChecked = _selected is not null && !_draft.DisabledThemes.Contains(_selected, StringComparer.Ordinal);
      _loading = false;
   }



   /// <summary>Enable/disable changes preserve JSON and all other draft fields.</summary>
   private void SetEnabled(bool enabled)
   {
      if(!_loading && _selected is not null)
      {
         _draft = ThemeManagement.SetEnabled(_draft, _selected, enabled);
      }
   }



   /// <summary>Reorders a selected visible entry, including a disabled theme.</summary>
   private void ChangeSelected(bool up)
   {
      if(_selected is not null)
      {
         _draft = ThemeManagement.Move(_draft, _selected, up);
         Refresh(_selected);
      }
   }



   /// <summary>Deletion remains reversible with Cancel; saved built-in deletion is restorable.</summary>
   private void DeleteSelected()
   {
      if(_selected is not null)
      {
         _draft = ThemeManagement.Delete(_draft, _selected);
         _status.Text = "Removed from this draft. Save commits the deletion; Cancel discards it.";
         Refresh(null);
      }
   }



   /// <summary>Restoration retains edited palettes as renamed custom themes and re-enables all choices.</summary>
   private void Restore()
   {
      _draft = ThemeManagement.RestoreBuiltIns(_draft);
      _status.Text = "Original built-ins restored in this draft. Edited built-ins are kept as renamed custom themes.";
      Refresh(_draft.DocumentTheme);
   }



   /// <summary>Each secondary dialog finishes before the manager reopens; cancellation keeps the copied draft intact.</summary>
   private async Task RunActionAsync(XamlRoot root)
   {
      if(_action == ManagerAction.Edit && _requestedTheme is { } original)
      {
         ReaderTheme? edited = await ThemeEditor.ShowAsync(_host!, original, ThemeManagement.All(_draft).Select(theme => theme.Name));
         if(edited is not null)
         {
            _draft = ThemeManagement.SaveEdited(_draft, original, edited);
            _selected = edited.Name;
         }
      }
      else if(_action == ManagerAction.Import)
      {
         ThemeTransfer.ImportOutcome? result = await ThemeTransfer.ImportAsync(_owner, root, _draft);
         if(result is not null)
         {
            _draft = result.Draft;
            _status.Text = FormattableString.Invariant($"Themes imported into this draft: {result.Imported}. Skipped: {result.Skipped}. Failed: {result.Failed}.");
         }
      }
      else if(_action == ManagerAction.Export && _requestedTheme is { } theme && await ThemeTransfer.ExportAsync(_owner, theme))
      {
         _status.Text = "The selected draft theme was exported. Save themes commits these settings; Cancel discards them.";
      }
   }



   /// <summary>New/copy/edit actions reuse the full native editor without nesting incompatible content dialogs.</summary>
   private void RequestEdit(ReaderTheme theme)
   {
      _requestedTheme = theme;
      _action = ManagerAction.Edit;
      _host!.EndEdit(ContentDialogResult.None);
   }



   /// <summary>Disabled choices remain editable and copyable without enabling them prematurely.</summary>
   private void EditSelected(bool copy)
   {
      ReaderTheme? theme = ThemeManagement.All(_draft).FirstOrDefault(theme => string.Equals(theme.Name, _selected, StringComparison.Ordinal));
      if(theme is not null)
      {
         RequestEdit(copy ? ThemeManagement.CopyTheme(_draft, theme) : theme);
      }
   }



   /// <summary>Choosing a default explicitly enables it; selection alone never changes the reading palette.</summary>
   private void UseSelected()
   {
      if(_selected is not null)
      {
         _draft = ThemeManagement.SetEnabled(_draft, _selected, enabled: true);
         _draft.DocumentTheme = _selected;
         _status.Text = "Selected as the default in this draft. Save themes applies it.";
         Refresh(_selected);
      }
   }



   /// <summary>Export can select a disabled theme, while import has its own owned multiple-file picker.</summary>
   private void RequestTransfer(ManagerAction action)
   {
      _requestedTheme = ThemeManagement.All(_draft).FirstOrDefault(theme => string.Equals(theme.Name, _selected, StringComparison.Ordinal));
      if(action == ManagerAction.Import || _requestedTheme is not null)
      {
         _action = action;
         _host!.EndEdit(ContentDialogResult.None);
      }
   }



   /// <summary>Only native action buttons can request the next manager operation.</summary>
   private enum ManagerAction
   {
      None,
      Edit,
      Import,
      Export,
   }

}
