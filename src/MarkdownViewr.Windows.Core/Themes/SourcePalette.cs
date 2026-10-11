/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using System.Text.Json;
using MarkdownViewr.Windows.Core.Documents;


namespace MarkdownViewr.Windows.Core.Themes;

/// <summary>Combines document accent colors with Windows light/dark source backgrounds and readable foreground contrast.</summary>
public sealed class SourcePalette
{
   private readonly Dictionary<SourceTokenKind, string> _colors = [];



   /// <summary>Validated document colors are adjusted only for the source's native background, never written back to theme data.</summary>
   public SourcePalette(ReaderTheme theme, bool dark)
   {
      ArgumentNullException.ThrowIfNull(theme);
      Background = dark ? "#1E1E1E" : "#FFFFFF";
      Foreground = dark ? "#D4D4D4" : "#1F1F1F";
      using var json = JsonDocument.Parse(theme.Json);
      JsonElement colors = json.RootElement.GetProperty("colors");
      foreach(SourceTokenKind kind in Enum.GetValues<SourceTokenKind>())
      {
         string key = kind switch
         {
            SourceTokenKind.Heading => "heading1",
            SourceTokenKind.Emphasis => "heading3",
            SourceTokenKind.Strong => "heading2",
            SourceTokenKind.Strikethrough => "heading6",
            SourceTokenKind.Image => "heading4",
            SourceTokenKind.List => "heading5",
            SourceTokenKind.Quote or SourceTokenKind.Html => "blockquoteBorder",
            SourceTokenKind.Code or SourceTokenKind.Frontmatter => "codeText",
            _ => "link",
         };
         _colors.Add(kind, Readable(colors.GetProperty(key).GetString()!));
      }
   }



   /// <summary>Shell mode defines the source background independently of the reader's document palette.</summary>
   public string Background { get; }



   /// <summary>Ordinary source remains readable when a document palette was authored for a different light/dark mode.</summary>
   public string Foreground { get; }



   /// <summary>Only known source kinds can request native colors.</summary>
   public string Color(SourceTokenKind kind) => _colors[kind];



   /// <summary>WCAG luminance measures native foreground/background contrast, including custom document palettes.</summary>
   public static double Contrast(string foreground, string background)
   {
      double first = Luminance(foreground);
      double second = Luminance(background);
      return (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
   }



   /// <summary>Bounded blending retains the document hue while ensuring at least4.5:1 contrast against the native background.</summary>
   private string Readable(string color)
   {
      string candidate = color;
      int step = 0;
      while(Contrast(candidate, Background) < 4.5)
      {
         // At step10 the known foreground guarantees contrast, even for a matching-background custom accent.
         double amount = ++step / 10d;
         byte[] channels = new byte[3];
         for(int channel = 0; channel < 3; channel++)
         {
            double original = Channel(color, channel);
            double target = Channel(Foreground, channel);
            channels[channel] = (byte)Math.Round(original + (target - original) * amount);
         }
         candidate = FormattableString.Invariant($"#{channels[0]:X2}{channels[1]:X2}{channels[2]:X2}");
      }
      return candidate;
   }



   /// <summary>Theme validation already restricts input to RGB hex colors; sRGB channels are converted to linear light.</summary>
   private static double Luminance(string color)
   {
      double value = 0;
      double[] weights = [0.2126, 0.7152, 0.0722];
      for(int channel = 0; channel < 3; channel++)
      {
         double component = Channel(color, channel) / 255d;
         double linear = component <= 0.04045 ? component / 12.92 : Math.Pow((component + 0.055) / 1.055, 2.4);
         value += linear * weights[channel];
      }
      return value;
   }



   /// <summary>Extracts only validated numeric RGB channels; authored CSS and font names are not interpreted.</summary>
   private static byte Channel(string color, int channel) => byte.Parse(color.AsSpan(1 + channel * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
}
