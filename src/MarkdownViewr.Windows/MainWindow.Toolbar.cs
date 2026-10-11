/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;


namespace MarkdownViewr.Windows;

/// <summary>Matches the reference document toolbar while sharing native menu action paths and responsive overflow.</summary>
public sealed partial class MainWindow
{
   private readonly ToolTip _editorUnavailableTip = new();
   private readonly DispatcherTimer _editorHoverDelay = new() { Interval = TimeSpan.FromMilliseconds(500) };



   /// <summary>Fixed primary commands and reading actions share the same native menu state.</summary>
   private void InitializeToolbar()
   {
      RefreshToolbarReadingState();
      ReaderToolbar.Opening += (_, _) => RefreshToolbarEditors();
      _editorUnavailableTip.PlacementTarget = ToolbarEditorButton;
      AutomationProperties.SetAutomationId(_editorUnavailableTip, "EditorUnavailableTip");
      _editorHoverDelay.Tick += (_, _) =>
      {
         _editorHoverDelay.Stop();
         if(!_closed && !ToolbarEditorButton.IsEnabled)
         {
            _editorUnavailableTip.XamlRoot = ReaderToolbar.XamlRoot;
            _editorUnavailableTip.IsOpen = true;
         }
      };
      ReaderToolbar.PointerMoved += EditorUnavailablePointerMoved;
      ReaderToolbar.PointerExited += (_, _) => CloseEditorExplanation();
   }



   /// <summary>Menu changes and toolbar clicks reflect the same visibility/depth preferences and accessible toggle state.</summary>
   private void RefreshToolbarReadingState()
   {
      ToolbarOutlineButton.IsChecked = _settings.ShowOutline;
      ToolbarSourceButton.IsChecked = _settings.ShowSource;
      ToolTipService.SetToolTip(ToolbarOutlineButton, _settings.ShowOutline ? "Hide table of contents" : "Show table of contents");
      ToolTipService.SetToolTip(ToolbarSourceButton, _settings.ShowSource ? "Hide Markdown source" : "Show Markdown source");

   }



   /// <summary>The toolbar routes TOC visibility through the same saved menu operation.</summary>
   private void ToolbarOutline_Click(object sender, RoutedEventArgs e)
   {
      OutlineToggle.IsChecked = ToolbarOutlineButton.IsChecked == true;
      Outline_Click(sender, e);
   }



   /// <summary>Source toggling uses the shared snapshot,50/50 opening and find refresh path.</summary>
   private void ToolbarSource_Click(object sender, RoutedEventArgs e)
   {
      SourceToggle.IsChecked = ToolbarSourceButton.IsChecked == true;
      Source_Click(sender, e);
   }



   /// <summary>Only enabled choices appear, following the same saved order and current selection as View.</summary>
   private void RefreshToolbarThemes()
   {
      ToolbarThemeFlyout.Items.Clear();
      string selected = ActiveDocumentTheme().Name;
      ToolbarThemeButton.Label = selected;
      ToolbarThemeButton.IsEnabled = _themes.Count > 0;
      AutomationProperties.SetName(ToolbarThemeButton, "Document theme: " + selected);
      foreach(Core.Themes.ReaderTheme theme in _themes)
      {
         var item = new RadioMenuFlyoutItem { Text = theme.Name, Tag = theme, GroupName = "ToolbarTheme", IsChecked = string.Equals(selected, theme.Name, StringComparison.Ordinal) };
         AutomationProperties.SetAutomationId(item, "ToolbarTheme:" + theme.Name);
         item.Click += DocumentTheme_Click;
         ToolbarThemeFlyout.Items.Add(item);
      }
   }



   /// <summary>The reference launches one existing editor directly; multiple existing editors expose named file/folder choices.</summary>
   private void RefreshToolbarEditors()
   {
      Core.Editors.EditorConfiguration[] editors = _settings.ConfiguredEditors.Where(editor => File.Exists(editor.Executable)).ToArray();
      string? unavailable = _documentPath is null ? "Open a document before opening it in an editor."
         : !File.Exists(_documentPath) ? "The document is missing. Restore it before opening an editor."
         : _settings.ConfiguredEditors.Length == 0 ? "No editor is configured. Choose one in File → Settings → Editors."
         : editors.Length == 0 ? "The configured editor is unavailable. Choose an installed editor in File → Settings → Editors."
         : null;
      ToolbarEditorButton.IsEnabled = unavailable is null;
      ToolbarEditorButton.Label = editors.Length == 1 ? "Open in " + editors[0].Name : "Open in editor";
      ToolbarEditorButton.Tag = editors.Length == 1 ? editors[0].Id : null;
      AutomationProperties.SetName(ToolbarEditorButton, ToolbarEditorButton.Label);
      string explanation = unavailable ?? "Open the document in a configured editor.";
      ToolTipService.SetToolTip(ToolbarEditorButton, explanation);
      AutomationProperties.SetHelpText(ToolbarEditorButton, explanation);
      AutomationProperties.SetHelpText(OpenEditorMenuItem, explanation);
      ToolTipService.SetToolTip(OpenEditorMenuItem, explanation);
      _editorUnavailableTip.Content = explanation;
      CloseEditorExplanation();
      ToolbarEditorButton.Flyout = null;
      if(editors.Length > 1)
      {
         var choices = new MenuFlyout();
         foreach(Core.Editors.EditorConfiguration editor in editors)
         {
            var item = new MenuFlyoutItem { Text = editor.Name + (editor.OpensFolder ? " (folder)" : ""), Tag = editor.Id };
            AutomationProperties.SetAutomationId(item, "ToolbarEditor:" + editor.Id);
            item.Click += OpenEditor_Click;
            choices.Items.Add(item);
         }
         ToolbarEditorButton.Flyout = choices;
      }
   }



   /// <summary>The enabled toolbar receives hover even over a disabled child; native ToolTip explains its actual unavailable state.</summary>
   private void EditorUnavailablePointerMoved(object sender, PointerRoutedEventArgs args)
   {
      global::Windows.Foundation.Point point = args.GetCurrentPoint(ToolbarEditorButton).Position;
      if(!ToolbarEditorButton.IsEnabled && point.X >= 0 && point.Y >= 0 && point.X < ToolbarEditorButton.ActualWidth && point.Y < ToolbarEditorButton.ActualHeight)
      {
         if(!_editorHoverDelay.IsEnabled && !_editorUnavailableTip.IsOpen)
         {
            _editorHoverDelay.Start();
         }
      }
      else
      {
         CloseEditorExplanation();
      }
   }



   /// <summary>Leaving the disabled command or changing its state cancels pending hover and closes the owned tooltip.</summary>
   private void CloseEditorExplanation()
   {
      _editorHoverDelay.Stop();
      _editorUnavailableTip.IsOpen = false;
   }



   /// <summary>A direct editor action uses its stable identity; multiple choices are presented by the native flyout.</summary>
   private void ToolbarEditor_Click(object sender, RoutedEventArgs e)
   {
      if(ToolbarEditorButton.Tag is Guid)
      {
         OpenEditor_Click(sender, e);
      }
   }
}
