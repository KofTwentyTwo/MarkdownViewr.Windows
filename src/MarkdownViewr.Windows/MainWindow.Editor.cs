/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>Routes deliberate native editor commands through a literal process argument boundary.</summary>
public sealed partial class MainWindow
{
   /// <summary>One named editor is a direct action; multiple editors expose their saved order through native choices.</summary>
   private void RefreshEditorCommands()
   {
      Core.Editors.EditorConfiguration[] editors = _settings.ConfiguredEditors;
      OpenEditorMenuItem.Visibility = editors.Length > 1 ? Visibility.Collapsed : Visibility.Visible;
      OpenEditorMenuItem.IsEnabled = _documentPath is not null && editors.Length == 1;
      OpenEditorMenuItem.Text = editors.Length == 1 ? "Open in " + editors[0].Name + (editors[0].OpensFolder ? " (folder)" : "") : "Open in editor";
      OpenEditorChoices.Visibility = editors.Length > 1 ? Visibility.Visible : Visibility.Collapsed;
      OpenEditorChoices.IsEnabled = _documentPath is not null;
      OpenEditorChoices.Items.Clear();
      foreach(Core.Editors.EditorConfiguration editor in editors)
      {
         var item = new MenuFlyoutItem { Text = editor.Name + (editor.OpensFolder ? " (folder)" : ""), Tag = editor.Id };
         Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(item, "EditorLaunch:" + editor.Id);
         item.Click += OpenEditor_Click;
         OpenEditorChoices.Items.Add(item);
      }
      RefreshToolbarEditors();
   }



   /// <summary>Changes remain in the dialog's copied list until the user's validated Save.</summary>
   private async Task ConfigureEditorAsync()
   {
      Core.AppSettings before = Core.SettingsChanges.Copy(_settings);
      var editor = new EditorConfigurationDialog(before.ConfiguredEditors);
      Core.Editors.EditorConfiguration[]? choices = await editor.ShowAsync(ConfigurationPage);
      if(choices is null || _closed)
      {
         return;
      }
      Core.AppSettings draft = Core.SettingsChanges.Copy(before);
      draft.ConfiguredEditors = choices;
      draft.ExternalEditor = choices.FirstOrDefault()?.Executable ?? "";
      Core.AppSettings merged = Core.SettingsChanges.Merge(AppIdentity.SettingsStore.Load(), before, draft);
      if(!AppIdentity.SettingsStore.Save(merged))
      {
         StatusText.Text = "Could not save editor configuration.";
         return;
      }
      ((App)Application.Current).ApplyEditorSettings(merged.ConfiguredEditors);
      StatusText.Text = "Editors configured.";
   }



   /// <summary>A shared editor save updates both this reader and its save baseline before any later local preference change.</summary>
   internal void ApplyEditorSettings(Core.Editors.EditorConfiguration[] choices)
   {
      if(_closed)
      {
         return;
      }
      _settings.ConfiguredEditors = choices.ToArray();
      _lastSavedSettings.ConfiguredEditors = choices.ToArray();
      _settings.ExternalEditor = _lastSavedSettings.ExternalEditor = choices.FirstOrDefault()?.Executable ?? "";
      RefreshEditorCommands();
   }



   /// <summary>Launches only the selected executable and never interpolates Markdown or a shell command.</summary>
   private void OpenEditor_Click(object sender, RoutedEventArgs e)
   {
      if(_closed || _documentPath is null)
      {
         return;
      }
      Core.Editors.EditorConfiguration? editor = sender is FrameworkElement { Tag: Guid identity }
         ? _settings.ConfiguredEditors.FirstOrDefault(choice => choice.Id == identity)
         : _settings.ConfiguredEditors.FirstOrDefault();
      if(!Core.Editors.EditorConfiguration.IsValid(editor))
      {
         return;
      }
      if(!File.Exists(editor!.Executable))
      {
         StatusText.Text = "The configured editor is missing. Use File → Settings → Editors to choose an installed executable.";
         return;
      }
      if(!File.Exists(_documentPath))
      {
         StatusText.Text = "The document is missing. Restore it before opening the editor.";
         return;
      }
      try
      {
         using var process = Process.Start(Core.Editors.EditorLaunch.Create(editor.Executable, _documentPath, editor.OpensFolder));
         StatusText.Text = process is not null ? editor.OpensFolder ? "Opened document folder in the configured editor." : "Opened document in the configured editor." : "The configured editor did not start.";
         App.Log.Info("Requested a document launch in the configured editor.");
      }
      catch(Exception exception) when(exception is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
      {
         StatusText.Text = "Could not start the configured editor. Use File → Settings → Editors to check its executable.";
         App.Log.Warning("The configured editor could not be started.");
      }
   }
}
