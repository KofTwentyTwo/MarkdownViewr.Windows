/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Editors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>Edits a copied native editor list; cancellation cannot mutate saved choices.</summary>
internal sealed class EditorConfigurationDialog
{
   private readonly List<EditorConfiguration> _editors;
   private readonly ListBox _list = new() { Name = "EditorList", Height = 100 };
   private readonly TextBox _name = new() { Name = "EditorName", Header = "Editor name", MaxLength = 80 };
   private readonly TextBox _path = new() { Name = "EditorPath", Header = "Editor executable path", MaxLength = 32767, MinWidth = 320 };
   private readonly CheckBox _folder = new() { Name = "EditorFolder", Content = "Open the document's folder" };
   private readonly TextBlock _error = new() { Name = "EditorError", TextWrapping = TextWrapping.Wrap };
   private readonly ContentDialog _dialog;
   private int _selected = -1;
   private Guid _identity = Guid.NewGuid();
   private bool _loading;
   private bool _dirty;



   /// <summary>Restores stable identities and native controls without requiring saved executables to remain installed.</summary>
   public EditorConfigurationDialog(IEnumerable<EditorConfiguration> editors)
   {
      _editors = editors.ToList();
      _dialog = new ContentDialog
      {
         Title = "Configure editors",
         Content = BuildContent(),
         PrimaryButtonText = "Save",
         CloseButtonText = "Cancel",
         DefaultButton = ContentDialogButton.Primary,
      };
      _dialog.PrimaryButtonClick += (_, args) => args.Cancel = !ApplyCurrent();
      _list.SelectionChanged += (_, _) => ChangeSelection();
      _name.TextChanged += (_, _) => _dirty |= !_loading;
      _path.TextChanged += (_, _) => _dirty |= !_loading;
      _folder.Checked += (_, _) => _dirty |= !_loading;
      _folder.Unchecked += (_, _) => _dirty |= !_loading;
      LoadEditor(_editors.Count > 0 ? 0 : -1);
   }



   /// <summary>Shows the editor manager on the owning window and returns only explicitly committed choices.</summary>
   public async Task<EditorConfiguration[]?> ShowAsync(XamlRoot root)
   {
      _dialog.XamlRoot = root;
      return await _dialog.ShowAsync() == ContentDialogResult.Primary ? _editors.ToArray() : null;
   }



   /// <summary>Provides named list actions and literal file/folder controls without exposing command-line flags.</summary>
   private StackPanel BuildContent()
   {
      var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
      actions.Children.Add(Button("EditorAdd", "Add", AddEditor));
      actions.Children.Add(Button("EditorRemove", "Remove", RemoveEditor));
      actions.Children.Add(Button("EditorUp", "Move up", () => MoveEditor(-1)));
      actions.Children.Add(Button("EditorDown", "Move down", () => MoveEditor(1)));
      var panel = new StackPanel { Spacing = 8 };
      panel.Children.Add(new TextBlock { Text = "Choose installed .exe editors. Each receives one literal document or folder argument. Save keeps the list; Cancel discards changes.", TextWrapping = TextWrapping.Wrap });
      panel.Children.Add(_list);
      panel.Children.Add(actions);
      panel.Children.Add(_name);
      panel.Children.Add(_path);
      panel.Children.Add(_folder);
      panel.Children.Add(_error);
      return panel;
   }



   /// <summary>Identifies native manager actions for accessibility and actual hosted acceptance.</summary>
   private static Button Button(string identity, string label, Action action)
   {
      var button = new Button { Name = identity, Content = label };
      button.Click += (_, _) => action();
      return button;
   }



   /// <summary>Retains edited fields before changing selection; invalid input keeps the current entry visible.</summary>
   private void ChangeSelection()
   {
      if(_loading)
      {
         return;
      }
      int next = _list.SelectedIndex;
      if(_dirty && !ApplyCurrent())
      {
         RefreshList();
         return;
      }
      LoadEditor(next);
   }



   /// <summary>Projects saved order and folder labels into a native list with stable accessible identities.</summary>
   private void RefreshList()
   {
      _loading = true;
      _list.Items.Clear();
      foreach(EditorConfiguration editor in _editors)
      {
         var item = new ListBoxItem { Content = editor.Name + (editor.OpensFolder ? " (folder)" : "") };
         AutomationProperties.SetAutomationId(item, "EditorChoice:" + editor.Id);
         _list.Items.Add(item);
      }
      _list.SelectedIndex = _selected;
      _loading = false;
   }



   /// <summary>Loads one copied entry or an empty new choice without marking programmatic restoration as an edit.</summary>
   private void LoadEditor(int index, bool empty = false)
   {
      _loading = true;
      _selected = index;
      EditorConfiguration? editor = index >= 0 ? _editors[index] : null;
      _identity = editor?.Id ?? Guid.NewGuid();
      _name.Text = editor?.Name ?? (empty ? "" : "Editor");
      _path.Text = editor?.Executable ?? "";
      _folder.IsChecked = editor?.OpensFolder ?? false;
      _dirty = false;
      _error.Text = "";
      _loading = false;
      RefreshList();
   }



   /// <summary>Validates changed entries before draft/save while allowing unchanged missing choices to be removed later.</summary>
   private bool ApplyCurrent()
   {
      if(!_dirty && _selected >= 0 || _selected < 0 && _path.Text.Length == 0 && (_editors.Count > 0 || _name.Text.Length == 0))
      {
         return true;
      }
      var editor = new EditorConfiguration(_identity, _name.Text.Trim(), _path.Text, _folder.IsChecked == true);
      if(!EditorLaunch.IsExecutablePath(editor.Executable) || !File.Exists(editor.Executable))
      {
         _error.Text = "Enter the full path of an existing .exe file.";
         return false;
      }
      if(!EditorConfiguration.IsValid(editor))
      {
         _error.Text = "Enter an editor name of 1–80 characters without control characters.";
         return false;
      }
      if(_selected >= 0)
      {
         _editors[_selected] = editor;
      }
      else if(_editors.Count < EditorConfiguration.MaximumEditors)
      {
         _selected = _editors.Count;
         _editors.Add(editor);
      }
      else
      {
         _error.Text = "Keep no more than 32 editors.";
         return false;
      }
      _dirty = false;
      RefreshList();
      return true;
   }



   /// <summary>Adds a new draft after retaining the current choice; no preference is written until Save.</summary>
   private void AddEditor()
   {
      if((_path.Text.Length == 0 && _selected < 0 || ApplyCurrent()) && _editors.Count < EditorConfiguration.MaximumEditors)
      {
         LoadEditor(-1);
      }
   }



   /// <summary>Removes only the chosen identity, including an uninstalled editor; the last removal can be saved as an empty list.</summary>
   private void RemoveEditor()
   {
      if(_selected < 0)
      {
         return;
      }
      _editors.RemoveAt(_selected);
      LoadEditor(_editors.Count > 0 ? Math.Min(_selected, _editors.Count - 1) : -1, empty: true);
   }



   /// <summary>Moves a retained choice by one row, preserving its ID and file/folder mode.</summary>
   private void MoveEditor(int offset)
   {
      int target = _selected + offset;
      if(_selected < 0 || target < 0 || target >= _editors.Count || !ApplyCurrent())
      {
         return;
      }
      (_editors[_selected], _editors[target]) = (_editors[target], _editors[_selected]);
      LoadEditor(target);
   }
}
