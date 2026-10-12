/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>Owns native Markdown preferences; document content cannot change rendering options.</summary>
public sealed partial class MainWindow
{
   /// <summary>Loads independent saved toggles without modifying a sibling window's rendering snapshot.</summary>
   private void InitializeMarkdownExtensions()
   {
      HighlightExtension.IsChecked = _settings.RenderHighlight;
      SuperscriptExtension.IsChecked = _settings.RenderSuperscript;
      SubscriptExtension.IsChecked = _settings.RenderSubscript;
      UnderlineExtension.IsChecked = _settings.RenderUnderline;
   }



   /// <summary>Persists one deliberately changed option and rerenders through the ordinary guarded reload path.</summary>
   private async void MarkdownExtension_Click(object sender, RoutedEventArgs e)
   {
      var item = (ToggleMenuFlyoutItem)sender;
      switch((string)item.Tag)
      {
         case "Highlight":
            _settings.RenderHighlight = item.IsChecked;
            break;
         case "Superscript":
            _settings.RenderSuperscript = item.IsChecked;
            break;
         case "Subscript":
            _settings.RenderSubscript = item.IsChecked;
            break;
         case "Underline":
            _settings.RenderUnderline = item.IsChecked;
            break;
      }
      Save();
      if(_documentPath is not null)
      {
         await OpenDocumentAsync(_documentPath, reload: true);
      }
   }
}
