/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using System.Text;
using System.Text.Json;


namespace MarkdownViewr.Windows.Core.Themes;

/// <summary>Validates Darin's version-one theme format before generating passive document CSS.</summary>
public sealed class ReaderTheme
{
   private const int MaximumCharacters = 128 * 1024;
   private static readonly string[] s_colors = ["background", "text", "heading1", "heading2", "heading3", "heading4", "heading5", "heading6", "link", "codeBackground", "codeText", "blockquoteBorder", "blockquoteBackground", "highlightBackground", "highlightText"];
   private static readonly string[] s_sizes = ["baseFontSize", "h1Size", "h2Size", "h3Size", "h4Size", "h5Size", "h6Size", "codeFontSize", "lineHeight"];



   /// <summary>Retains only the validated display name and generated CSS.</summary>
   private ReaderTheme(string name, string styles, string json)
   {
      Name = name;
      Styles = styles;
      Json = json;
   }



   /// <summary>The display name, never an HTML or filesystem identifier.</summary>
   public string Name { get; }



   /// <summary>CSS safe for an inline style element; document resource policy still applies.</summary>
   public string Styles { get; }



   /// <summary>The validated source retained for editing without losing palette fields.</summary>
   public string Json { get; }



   /// <summary>Scales only validated font sizes; colors, fonts, line height and custom CSS retain their original semantics.</summary>
   public string GetStyles(double zoomScale)
   {
      if(!double.IsFinite(zoomScale) || zoomScale is < 0.3 or > 5)
      {
         throw new ArgumentOutOfRangeException(nameof(zoomScale));
      }
      using var document = JsonDocument.Parse(Json);
      return BuildStyles(document.RootElement, zoomScale);
   }



   /// <summary>Accepts the original schema, including themes without an explicit version number.</summary>
   public static ReaderTheme Parse(string json)
   {
      ArgumentNullException.ThrowIfNull(json);
      if(json.Length > MaximumCharacters)
      {
         throw new JsonException("The theme exceeds the 128 Ki-character limit.");
      }
      using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
      JsonElement root = document.RootElement;
      RequireObject(root);
      if(root.TryGetProperty("schemaVersion", out JsonElement version) && (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int number) || number != 1))
      {
         throw new JsonException("Only theme schema version 1 is supported.");
      }
      string name = ReadString(root, "name");
      if(string.IsNullOrWhiteSpace(name) || name.Length > 80 || name.Any(char.IsControl))
      {
         throw new JsonException("Theme names must contain 1–80 printable characters.");
      }
      string styles = BuildStyles(root);
      return new ReaderTheme(name, styles, json);
   }



   /// <summary>Constructs variables only from validated colors, finite sizes and escaped font families.</summary>
   private static string BuildStyles(JsonElement root, double zoomScale = 1)
   {
      var styles = new StringBuilder(":root {");
      JsonElement colors = ReadObject(root, "colors");
      foreach(string field in s_colors)
      {
         string value = ReadString(colors, field);
         if(value.Length != 7 || value[0] != '#' || !value.AsSpan(1).ToString().All(char.IsAsciiHexDigit))
         {
            throw new JsonException("Theme colors must use six-digit hexadecimal notation.");
         }
         styles.Append("--").Append(field).Append(':').Append(value).Append(';');
      }
      AddSizes(styles, ReadObject(root, "sizes"), zoomScale);
      JsonElement fonts = ReadObject(root, "fonts");
      foreach(string field in new[] { "body", "heading", "code" })
      {
         styles.Append("--").Append(field).Append("Font:").Append(FontFamily(ReadString(fonts, field))).Append(';');
      }
      styles.Append('}').Append(BaseStyles);
      if(root.TryGetProperty("customCSS", out _))
      {
         // CSS can style the page, but it cannot close the HTML style element.
         styles.Append(ReadString(root, "customCSS").Replace("<", "\\3c ", StringComparison.Ordinal));
      }
      return styles.ToString();
   }



   /// <summary>Bounds font sizes and line height, rejecting non-finite or non-numeric values.</summary>
   private static void AddSizes(StringBuilder styles, JsonElement sizes, double zoomScale)
   {
      foreach(string field in s_sizes)
      {
         bool lineHeight = string.Equals(field, "lineHeight", StringComparison.Ordinal);
         if(!sizes.TryGetProperty(field, out JsonElement element) || element.ValueKind != JsonValueKind.Number ||
            !element.TryGetDouble(out double value) || !double.IsFinite(value) || value < (lineHeight ? 0.5 : 6) || value > (lineHeight ? 4 : 96))
         {
            throw new JsonException("Theme sizes must be finite and within the supported reading range.");
         }
         double scaled = lineHeight ? value : value * zoomScale;
         styles.Append("--").Append(field).Append(':').Append(scaled.ToString("R", CultureInfo.InvariantCulture));
         styles.Append(lineHeight ? ";" : "px;");
      }
   }



   /// <summary>Maps macOS defaults to Windows and escapes every custom font character as a CSS code point.</summary>
   private static string FontFamily(string value)
   {
      if(string.IsNullOrWhiteSpace(value) || value.Length > 80 || value.Any(char.IsControl))
      {
         throw new JsonException("Font names must contain 1–80 printable characters.");
      }
      return value switch
      {
         "System" => "system-ui, sans-serif",
         "SF Mono" => "Consolas, 'Cascadia Code', monospace",
         _ => "'" + string.Concat(value.EnumerateRunes().Select(rune => "\\" + rune.Value.ToString("X", CultureInfo.InvariantCulture) + " ")) + "'",
      };
   }



   /// <summary>Reports malformed required strings consistently as theme input errors.</summary>
   private static string ReadString(JsonElement parent, string name)
   {
      if(!parent.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.String)
      {
         throw new JsonException("The theme is missing a required string: " + name);
      }
      return value.GetString()!;
   }



   /// <summary>Requires each schema group to be an object.</summary>
   private static JsonElement ReadObject(JsonElement parent, string name)
   {
      if(!parent.TryGetProperty(name, out JsonElement value))
      {
         throw new JsonException("The theme is missing a required object: " + name);
      }
      RequireObject(value);
      return value;
   }



   /// <summary>Prevents wrong-shaped JSON from reaching property accessors.</summary>
   private static void RequireObject(JsonElement element)
   {
      if(element.ValueKind != JsonValueKind.Object)
      {
         throw new JsonException("A theme schema group must be an object.");
      }
   }



   private const string BaseStyles = """
      html { background: var(--background); color: var(--text); }
      body { max-width: 900px; margin: 0 auto; padding: 32px; overflow-wrap: anywhere;
             font: var(--baseFontSize)/var(--lineHeight) var(--bodyFont); }
      h1,h2,h3,h4,h5,h6 { font-family: var(--headingFont); line-height: 1.25; margin-top: 1.5em; }
      h1 { color: var(--heading1); font-size: var(--h1Size); }
      h2 { color: var(--heading2); font-size: var(--h2Size); }
      h3 { color: var(--heading3); font-size: var(--h3Size); }
      h4 { color: var(--heading4); font-size: var(--h4Size); }
      h5 { color: var(--heading5); font-size: var(--h5Size); }
      h6 { color: var(--heading6); font-size: var(--h6Size); }
      pre { padding: 16px; overflow: auto; background: var(--codeBackground); border-radius: 6px; }
      code,kbd { color: var(--codeText); font: var(--codeFontSize)/var(--lineHeight) var(--codeFont); }
      table { border-collapse: collapse; } th,td { border: 1px solid var(--blockquoteBorder); padding: 6px 12px; }
      blockquote { margin-left: 0; padding: 8px 20px; border-left: 4px solid var(--blockquoteBorder); background: var(--blockquoteBackground); }
      img { max-width: 100%; height: auto; } a { color: var(--link); }
      ::selection { background: var(--highlightBackground); color: var(--highlightText); }
      """;
}
