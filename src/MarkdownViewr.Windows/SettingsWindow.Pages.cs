/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>Configuration pages use normal Windows controls; each explicit theme/editor edit retains its own Save and Cancel.</summary>
internal sealed partial class SettingsWindow
{
   /// <summary>Theme editing, transfer and management are grouped together instead of crowded into the reader's View menu.</summary>
   private StackPanel ThemesPage()
   {
      var page = new StackPanel { Spacing = 16, MaxWidth = 720 };
      page.Children.Add(new TextBlock { Text = "Customize document fonts, colors and spacing, or manage your saved themes.", TextWrapping = TextWrapping.Wrap });
      page.Children.Add(Action("EditDocumentThemeMenu", "Edit current theme", "Fonts, colors, sizes and custom CSS"));
      page.Children.Add(Action("ManageDocumentThemesMenu", "Manage themes", "Create, copy, reorder, enable or delete themes"));
      var transfer = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
      transfer.Children.Add(Action("ImportDocumentThemesMenu", "Import", "Read themes from JSON files"));
      transfer.Children.Add(Action("ExportDocumentThemesMenu", "Export", "Save a theme as JSON"));
      page.Children.Add(transfer);
      page.Children.Add(Action("RestoreDocumentThemesMenu", "Restore built-in themes", "Keep edited themes as custom copies"));
      return page;
   }



   /// <summary>External-editor configuration is discoverable as a normal page with one deliberate edit action.</summary>
   private StackPanel EditorsPage()
   {
      var page = new StackPanel { Spacing = 16, MaxWidth = 720 };
      page.Children.Add(new TextBlock { Text = "Choose the editors used by Open in editor. Editors can open the document or its containing folder.", TextWrapping = TextWrapping.Wrap });
      page.Children.Add(Action("ConfigureEditorMenuItem", "Configure editors", "Add, remove and reorder editors"));
      return page;
   }



   /// <summary>Native labeled buttons show ordinary explanatory text rather than custom settings widgets.</summary>
   private Button Action(string identity, string label, string description)
   {
      var text = new StackPanel { Spacing = 4 };
      text.Children.Add(new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap });
      text.Children.Add(new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap, Opacity = 0.7 });
      var button = new Button { Content = text, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(16, 12, 16, 12) };
      AutomationProperties.SetAutomationId(button, identity);
      AutomationProperties.SetName(button, label);
      button.Click += async (_, _) =>
      {
         if(_closed || _busy)
         {
            return;
         }
         _busy = true;
         try
         {
            await _runAction(identity);
         }
         catch(Exception exception)
         {
            ReportError(exception);
         }
         finally
         {
            _busy = false;
         }
      };
      return button;
   }
}
