/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>Edits document appearance through native controls; invalid input leaves the editor open.</summary>
internal static class ThemeEditor
{
   /// <summary>Returns a validated edited theme, or null after cancellation.</summary>
   public static async Task<Core.Themes.ReaderTheme?> ShowAsync(XamlRoot xamlRoot, Core.Themes.ReaderTheme theme)
   {
      JsonNode source = JsonNode.Parse(theme.Json)!;
      TextBox name = Field("Name (a new name creates a custom theme)", theme.Name, "ThemeEditorName", 80);
      TextBox font = Field("Code font", source["fonts"]!["code"]!.GetValue<string>(), "ThemeEditorCodeFont", 80);
      var size = new NumberBox { Header = "Code font size", Minimum = 6, Maximum = 96, Value = source["sizes"]!["codeFontSize"]!.GetValue<double>() };
      Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(size, "ThemeEditorCodeSize");
      TextBox css = Field("Custom CSS", source["customCSS"]?.GetValue<string>() ?? "", "ThemeEditorCss", 128 * 1024);
      css.AcceptsReturn = true;
      css.Height = 160;
      css.TextWrapping = TextWrapping.Wrap;
      css.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas");
      var error = new TextBlock { TextWrapping = TextWrapping.Wrap };
      Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(error, "ThemeEditorError");
      var fields = new StackPanel { Spacing = 12 };
      fields.Children.Add(name);
      fields.Children.Add(font);
      fields.Children.Add(size);
      Dictionary<string, TextBox> colors = AddColors(fields, source["colors"]!.AsObject());
      fields.Children.Add(css);
      fields.Children.Add(error);
      var dialog = new ContentDialog
      {
         XamlRoot = xamlRoot,
         Title = "Edit document theme",
         PrimaryButtonText = "Save theme",
         CloseButtonText = "Cancel",
         DefaultButton = ContentDialogButton.Primary,
         Content = new ScrollViewer { Content = fields, Height = Math.Clamp(xamlRoot.Size.Height - 220, 120, 440), Width = 480, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled },
      };
      Core.Themes.ReaderTheme? edited = null;
      dialog.PrimaryButtonClick += (_, args) =>
      {
         try
         {
            edited = ReadEdited(source, name.Text, font.Text, size.Value, css.Text, colors);
         }
         catch(Exception exception) when(exception is JsonException or ArgumentException)
         {
            error.Text = exception.Message;
            args.Cancel = true;
            error.StartBringIntoView();
         }
      };
      _ = await dialog.ShowAsync();
      return edited;
   }



   /// <summary>Updates only the edited fields and runs the same schema/CSS validation as imported themes.</summary>
   private static Core.Themes.ReaderTheme ReadEdited(JsonNode source, string name, string font, double size, string css, Dictionary<string, TextBox> colors)
   {
      if(!double.IsFinite(size))
      {
         throw new JsonException("Enter a code font size between 6 and 96.");
      }
      source["name"] = name.Trim();
      source["fonts"]!["code"] = font.Trim();
      source["sizes"]!["codeFontSize"] = size;
      source["customCSS"] = css;
      foreach((string key, TextBox value) in colors)
      {
         source["colors"]![key] = value.Text.Trim();
      }
      return Core.Themes.ReaderTheme.Parse(source.ToJsonString());
   }



   /// <summary>Keeps every palette field editable, including all heading and highlight colors.</summary>
   private static Dictionary<string, TextBox> AddColors(StackPanel fields, JsonObject source)
   {
      var colors = new Dictionary<string, TextBox>(StringComparer.Ordinal);
      foreach((string key, JsonNode? value) in source)
      {
         string? label = key switch
         {
            "background" => "Background",
            "text" => "Text",
            "heading1" => "Heading 1",
            "heading2" => "Heading 2",
            "heading3" => "Heading 3",
            "heading4" => "Heading 4",
            "heading5" => "Heading 5",
            "heading6" => "Heading 6",
            "link" => "Links",
            "codeBackground" => "Code background",
            "codeText" => "Code text",
            "blockquoteBorder" => "Quote border",
            "blockquoteBackground" => "Quote background",
            "highlightBackground" => "Selection background",
            "highlightText" => "Selection text",
            _ => null,
         };
         if(label is null)
         {
            continue;
         }
         TextBox input = Field(label + " (#RRGGBB)", value!.GetValue<string>(), "ThemeEditorColor:" + key, 7);
         colors.Add(key, input);
         fields.Children.Add(input);
      }
      return colors;
   }



   /// <summary>Creates a labeled text control with a stable accessibility identifier.</summary>
   private static TextBox Field(string label, string value, string id, int maximumLength)
   {
      var input = new TextBox { Header = label, Text = value, MaxLength = maximumLength };
      Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(input, id);
      return input;
   }
}
