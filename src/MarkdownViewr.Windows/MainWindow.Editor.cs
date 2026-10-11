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
   /// <summary>A configured editor becomes available only after a document has been committed.</summary>
   private void RefreshEditorCommands()
      => OpenEditorMenuItem.IsEnabled = _documentPath is not null && Core.Editors.EditorLaunch.IsExecutablePath(_settings.ExternalEditor);



   /// <summary>Validates an installed executable before persisting the user's deliberate choice.</summary>
   private async void ConfigureEditor_Click(object sender, RoutedEventArgs e)
   {
      var path = new TextBox { Name = "EditorPath", Header = "Editor executable path", Text = _settings.ExternalEditor, MaxLength = 32767, MinWidth = 320 };
      var error = new TextBlock { Name = "EditorError", TextWrapping = TextWrapping.Wrap };
      var panel = new StackPanel { Spacing = 12 };
      panel.Children.Add(new TextBlock { Text = "Enter the full path of an installed Windows editor. It receives the current document as one file argument.", TextWrapping = TextWrapping.Wrap });
      panel.Children.Add(path);
      panel.Children.Add(error);
      var dialog = new ContentDialog
      {
         Title = "Configure editor",
         Content = panel,
         PrimaryButtonText = "Save",
         CloseButtonText = "Cancel",
         DefaultButton = ContentDialogButton.Primary,
         XamlRoot = Content.XamlRoot,
      };
      dialog.PrimaryButtonClick += (_, args) =>
      {
         if(!Core.Editors.EditorLaunch.IsExecutablePath(path.Text) || !File.Exists(path.Text))
         {
            args.Cancel = true;
            error.Text = "Enter the full path of an existing .exe file.";
         }
      };
      if(await dialog.ShowAsync() != ContentDialogResult.Primary || _closed)
      {
         return;
      }
      _settings.ExternalEditor = path.Text;
      Save();
      RefreshEditorCommands();
      StatusText.Text = "Editor configured.";
   }



   /// <summary>Launches only the selected executable and never interpolates Markdown or a shell command.</summary>
   private void OpenEditor_Click(object sender, RoutedEventArgs e)
   {
      if(_closed || _documentPath is null)
      {
         return;
      }
      if(!File.Exists(_settings.ExternalEditor))
      {
         StatusText.Text = "The configured editor is missing. Use File → Configure editor to choose an installed executable.";
         return;
      }
      if(!File.Exists(_documentPath))
      {
         StatusText.Text = "The document is missing. Restore it before opening the editor.";
         return;
      }
      try
      {
         using var process = Process.Start(Core.Editors.EditorLaunch.Create(_settings.ExternalEditor, _documentPath));
         StatusText.Text = process is not null ? "Opened document in the configured editor." : "The configured editor did not start.";
         App.Log.Info("Requested a document launch in the configured editor.");
      }
      catch(Exception exception) when(exception is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
      {
         StatusText.Text = "Could not start the configured editor. Use File → Configure editor to check its executable.";
         App.Log.Warning("The configured editor could not be started.");
      }
   }
}
