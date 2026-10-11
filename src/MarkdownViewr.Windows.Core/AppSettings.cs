/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;
using System.Text.Json.Serialization;
using KofTwentyTwo.AppKit.Settings;


namespace MarkdownViewr.Windows.Core;

/// <summary>Persistent AppKit shell and Markdown reading preferences.</summary>
public sealed class AppSettings : ShellSettings
{
   /// <summary>The selected document theme; independent of the Windows shell theme.</summary>
   public string DocumentTheme { get; set; } = "Catppuccin Mocha";


   /// <summary>Validated JSON themes, saved atomically with the AppKit preferences.</summary>
   public string[] UserThemes { get; set; } = [];



   /// <summary>The frontmatter display mode, saved independently of document contents.</summary>
   public string Frontmatter { get; set; } = "Hide";



   /// <summary>Repairs unknown saved mode values to the reference's default.</summary>
   [JsonIgnore]
   public Documents.FrontmatterMode FrontmatterKind => Enum.TryParse(Frontmatter, out Documents.FrontmatterMode mode) && Enum.IsDefined(mode) ? mode : Documents.FrontmatterMode.Hide;


   /// <inheritdoc/>
   public override void Sanitize()
   {
      base.Sanitize();
      Frontmatter = FrontmatterKind.ToString();
      if(string.IsNullOrWhiteSpace(DocumentTheme) || DocumentTheme.Length > 80 || DocumentTheme.Any(char.IsControl))
      {
         DocumentTheme = "Catppuccin Mocha";
      }
      UserThemes = (UserThemes ?? []).Where(IsValidTheme).ToArray();
   }



   /// <summary>A corrupt saved theme cannot prevent application startup or damage the bundled catalog.</summary>
   private static bool IsValidTheme(string json)
   {
      if(string.IsNullOrEmpty(json))
      {
         return false;
      }
      try
      {
         _ = Themes.ReaderTheme.Parse(json);
         return true;
      }
      catch(JsonException)
      {
         return false;
      }
   }
}
