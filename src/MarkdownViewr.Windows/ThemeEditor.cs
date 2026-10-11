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
   public static async Task<Core.Themes.ReaderTheme?> ShowAsync(SettingsWindow host, Core.Themes.ReaderTheme theme, IEnumerable<string>? existingNames = null)
   {
      JsonNode source = JsonNode.Parse(theme.Json)!;
      TextBox name = Field("Name (a new name creates a custom theme)", theme.Name, "ThemeEditorName", 80);
      TextBox css = Field("Custom CSS", source["customCSS"]?.GetValue<string>() ?? "", "ThemeEditorCss", 128 * 1024);
      css.AcceptsReturn = true;
      css.Height = 160;
      css.TextWrapping = TextWrapping.Wrap;
      css.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas");
      var error = new TextBlock { TextWrapping = TextWrapping.Wrap };
      Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(error, "ThemeEditorError");
      var fields = new StackPanel { Spacing = 12 };
      fields.Children.Add(name);
      Dictionary<string, TextBox> fonts = AddFonts(fields, source["fonts"]!.AsObject());
      Dictionary<string, NumberBox> sizes = AddSizes(fields, source["sizes"]!.AsObject());
      Dictionary<string, TextBox> colors = AddColors(fields, source["colors"]!.AsObject());
      fields.Children.Add(css);
      fields.Children.Add(error);
      Core.Themes.ReaderTheme? edited = null;
      bool Validate()
      {
         try
         {
            Core.Themes.ReaderTheme candidate = theme.Edit(name.Text, fonts.ToDictionary(pair => pair.Key, pair => pair.Value.Text, StringComparer.Ordinal),
               sizes.ToDictionary(pair => pair.Key, pair => pair.Value.Value, StringComparer.Ordinal),
               colors.ToDictionary(pair => pair.Key, pair => pair.Value.Text, StringComparer.Ordinal), css.Text);
            Core.Themes.ThemeManagement.ValidateEditName(theme.Name, candidate.Name, existingNames ?? []);
            edited = candidate;
            return true;
         }
         catch(Exception exception) when(exception is JsonException or ArgumentException)
         {
            error.Text = exception.Message;
            error.StartBringIntoView();
            return false;
         }
      }
      ContentDialogResult result = await host.ShowEditorAsync("Edit document theme", fields, "Save theme", Validate);
      return result == ContentDialogResult.Primary ? edited : null;
   }



   /// <summary>All three reference font families remain editable through stable native controls.</summary>
   private static Dictionary<string, TextBox> AddFonts(StackPanel fields, JsonObject source)
   {
      var fonts = new Dictionary<string, TextBox>(StringComparer.Ordinal);
      foreach((string key, string label, string id) in new[]
      {
         ("body", "Body font", "ThemeEditorBodyFont"),
         ("heading", "Heading font", "ThemeEditorHeadingFont"),
         ("code", "Code font", "ThemeEditorCodeFont"),
      })
      {
         TextBox input = Field(label, source[key]!.GetValue<string>(), id, 80);
         fonts.Add(key, input);
         fields.Children.Add(input);
      }
      return fonts;
   }



   /// <summary>Font-size and line-height controls use the same finite bounds as the saved schema.</summary>
   private static Dictionary<string, NumberBox> AddSizes(StackPanel fields, JsonObject source)
   {
      var sizes = new Dictionary<string, NumberBox>(StringComparer.Ordinal);
      foreach((string key, string label, string id) in new[]
      {
         ("baseFontSize", "Body font size", "ThemeEditorBodySize"),
         ("h1Size", "Heading 1 size", "ThemeEditorHeading1Size"),
         ("h2Size", "Heading 2 size", "ThemeEditorHeading2Size"),
         ("h3Size", "Heading 3 size", "ThemeEditorHeading3Size"),
         ("h4Size", "Heading 4 size", "ThemeEditorHeading4Size"),
         ("h5Size", "Heading 5 size", "ThemeEditorHeading5Size"),
         ("h6Size", "Heading 6 size", "ThemeEditorHeading6Size"),
         ("codeFontSize", "Code font size", "ThemeEditorCodeSize"),
         ("lineHeight", "Line height", "ThemeEditorLineHeight"),
      })
      {
         bool lineHeight = string.Equals(key, "lineHeight", StringComparison.Ordinal);
         var input = new NumberBox
         {
            Header = label,
            Minimum = lineHeight ? 0.5 : 6,
            Maximum = lineHeight ? 4 : 96,
            SmallChange = lineHeight ? 0.1 : 1,
            LargeChange = lineHeight ? 0.5 : 5,
            Value = source[key]!.GetValue<double>(),
         };
         Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(input, id);
         sizes.Add(key, input);
         fields.Children.Add(input);
      }
      return sizes;
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
            "highlightBackground" => "Highlight/selection background",
            "highlightText" => "Highlight/selection text",
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
