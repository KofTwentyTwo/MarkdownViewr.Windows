/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;


namespace MarkdownViewr.Windows.Core.Themes;

/// <summary>Validates Darin's version-one theme format before generating passive document CSS.</summary>
public sealed class ReaderTheme
{
   /// <summary>Bounds schema input before a native file read allocates the complete JSON text.</summary>
   public const int MaximumCharacters = 128 * 1024;
   private static readonly string[] s_colors = ["background", "text", "heading1", "heading2", "heading3", "heading4", "heading5", "heading6", "link", "codeBackground", "codeText", "blockquoteBorder", "blockquoteBackground", "highlightBackground", "highlightText"];
   private static readonly string[] s_sizes = ["baseFontSize", "h1Size", "h2Size", "h3Size", "h4Size", "h5Size", "h6Size", "codeFontSize", "lineHeight"];
   private static readonly string s_baseStyles = LoadBaseStyles();



   /// <summary>Retains only the validated display name and generated CSS.</summary>
   private ReaderTheme(string name, string styles, string json, int schemaVersion)
   {
      Name = name;
      Styles = styles;
      Json = json;
      SchemaVersion = schemaVersion;
   }



   /// <summary>The display name, never an HTML or filesystem identifier.</summary>
   public string Name { get; }



   /// <summary>CSS safe for an inline style element; document resource policy still applies.</summary>
   public string Styles { get; }



   /// <summary>The validated source retained for editing without losing palette fields.</summary>
   public string Json { get; }



   /// <summary>A compatible newer schema requires deliberate native confirmation before import, while its version is retained on export.</summary>
   public int SchemaVersion { get; }



   /// <summary>Copies a validated theme under another display name without dropping metadata or changing its schema version.</summary>
   public ReaderTheme Rename(string name)
   {
      JsonNode source = JsonNode.Parse(Json)!;
      source["name"] = name;
      return ParseCompatible(source.ToJsonString());
   }



   /// <summary>Edits a copy of the original schema, preserving unrelated metadata and rejecting invalid controls before persistence.</summary>
   public ReaderTheme Edit(string name, IReadOnlyDictionary<string, string> fonts, IReadOnlyDictionary<string, double> sizes, IReadOnlyDictionary<string, string> colors, string customCss)
   {
      ArgumentNullException.ThrowIfNull(name);
      ArgumentNullException.ThrowIfNull(fonts);
      ArgumentNullException.ThrowIfNull(sizes);
      ArgumentNullException.ThrowIfNull(colors);
      ArgumentNullException.ThrowIfNull(customCss);
      JsonNode source = JsonNode.Parse(Json)!;
      source["name"] = name.Trim();
      source["customCSS"] = customCss;
      foreach((string key, string value) in fonts)
      {
         if(key is not ("body" or "heading" or "code"))
         {
            throw new ArgumentException("Unknown theme font field.", nameof(fonts));
         }
         source["fonts"]![key] = value?.Trim();
      }
      foreach((string key, double value) in sizes)
      {
         if(!s_sizes.Contains(key, StringComparer.Ordinal))
         {
            throw new ArgumentException("Unknown theme size field.", nameof(sizes));
         }
         if(!double.IsFinite(value))
         {
            throw new JsonException("Enter finite font sizes between 6 and 96, and line height between 0.5 and 4.");
         }
         source["sizes"]![key] = value;
      }
      foreach((string key, string value) in colors)
      {
         if(!s_colors.Contains(key, StringComparer.Ordinal))
         {
            throw new ArgumentException("Unknown theme color field.", nameof(colors));
         }
         source["colors"]![key] = value?.Trim();
      }
      return ParseCompatible(source.ToJsonString());
   }



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



   /// <summary>Applies reference global/theme/width precedence only to reading styles, leaving print styles and theme JSON independent.</summary>
   public string GetReadingStyles(ReadingAppearance appearance, double zoomScale)
   {
      ArgumentNullException.ThrowIfNull(appearance);
      appearance.Validate();
      if(!double.IsFinite(zoomScale) || zoomScale is < 0.3 or > 5)
      {
         throw new ArgumentOutOfRangeException(nameof(zoomScale));
      }
      using var document = JsonDocument.Parse(Json);
      return BuildStyles(document.RootElement, zoomScale, appearance);
   }



   /// <summary>Accepts the original schema, including themes without an explicit version number.</summary>
   public static ReaderTheme Parse(string json) => ParseSchema(json, allowNewer: false);



   /// <summary>Validates every known field of saved themes or pending imports; native import still requires explicit newer-version confirmation.</summary>
   public static ReaderTheme ParseCompatible(string json) => ParseSchema(json, allowNewer: true);



   /// <summary>Compatibility never bypasses JSON depth, field, font, color, size or CSS checks.</summary>
   private static ReaderTheme ParseSchema(string json, bool allowNewer)
   {
      ArgumentNullException.ThrowIfNull(json);
      if(json.Length > MaximumCharacters)
      {
         throw new JsonException("The theme exceeds the 128 Ki-character limit.");
      }
      using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
      JsonElement root = document.RootElement;
      RequireObject(root);
      int schemaVersion = 1;
      if(root.TryGetProperty("schemaVersion", out JsonElement version) && (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out schemaVersion) || schemaVersion < 1 || !allowNewer && schemaVersion != 1))
      {
         throw new JsonException("The theme schema version is invalid or unsupported.");
      }
      string name = ReadString(root, "name");
      if(string.IsNullOrWhiteSpace(name) || name.Length > 80 || name.Any(char.IsControl))
      {
         throw new JsonException("Theme names must contain 1–80 printable characters.");
      }
      string styles = BuildStyles(root);
      return new ReaderTheme(name, styles, json, schemaVersion);
   }



   /// <summary>Constructs variables only from validated colors, finite sizes and escaped font families.</summary>
   private static string BuildStyles(JsonElement root, double zoomScale = 1, ReadingAppearance? appearance = null)
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
      styles.Append("--bg:var(--background);--h1:var(--heading1);--h2:var(--heading2);--h3:var(--heading3);--h4:var(--heading4);--h5:var(--heading5);--h6:var(--heading6);");
      styles.Append("--code-bg:var(--codeBackground);--code-text:var(--codeText);--blockquote-border:var(--blockquoteBorder);--blockquote-bg:var(--blockquoteBackground);");
      styles.Append("--highlight-bg:var(--highlightBackground);--highlight-text:var(--highlightText);--body-font:var(--bodyFont);--heading-font:var(--headingFont);--code-font:var(--codeFont);");
      styles.Append("--base-font-size:var(--baseFontSize);--h1-size:var(--h1Size);--h2-size:var(--h2Size);--h3-size:var(--h3Size);--h4-size:var(--h4Size);--h5-size:var(--h5Size);--h6-size:var(--h6Size);");
      styles.Append("--code-font-size:var(--codeFontSize);--line-height:var(--lineHeight);--zoom:").Append(zoomScale.ToString("R", CultureInfo.InvariantCulture));
      styles.Append(";}").Append(s_baseStyles);
      if(appearance is not null)
      {
         styles.Append(appearance.GlobalCss.Replace("<", "\\3c ", StringComparison.Ordinal));
      }
      if(root.TryGetProperty("customCSS", out _))
      {
         // CSS can style the page, but it cannot close the HTML style element.
         styles.Append(ReadString(root, "customCSS").Replace("<", "\\3c ", StringComparison.Ordinal));
      }
      if(appearance?.LimitWidth == true)
      {
         styles.Append("#content-inner{max-width:calc(").Append(appearance.WidthPixels.ToString("R", CultureInfo.InvariantCulture)).Append("px * var(--zoom));margin:auto;}");
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



   /// <summary>Embeds the pinned reference document rules without network access or a runtime asset-path dependency.</summary>
   private static string LoadBaseStyles()
   {
      using Stream stream = typeof(ReaderTheme).Assembly.GetManifestResourceStream("MarkdownViewr.Windows.Core.Themes.Document.css")!;
      using var reader = new StreamReader(stream);
      return reader.ReadToEnd();
   }
}
