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


   /// <summary>Shows the native heading sidebar when requested.</summary>
   public bool ShowOutline { get; set; }



   /// <summary>Maximum heading depth displayed in the sidebar.</summary>
   public int OutlineDepth { get; set; } = 3;



   /// <summary>Last native sidebar width in device-independent pixels.</summary>
   public double OutlineWidth { get; set; } = 220;


   /// <summary>Shows the literal, read-only Markdown snapshot beside the rendered document.</summary>
   public bool ShowSource { get; set; }



   /// <summary>Last source pane width in device-independent pixels.</summary>
   public double SourceWidth { get; set; } = 320;



   /// <summary>Legacy single-editor path, migrated into the list and kept as the first choice for compatible settings readers.</summary>
   public string ExternalEditor { get; set; } = "";



   /// <summary>Ordered named editor choices; the legacy path migrates into one entry on load.</summary>
   public Editors.EditorConfiguration[] ConfiguredEditors { get; set; } = [];



   /// <summary>Saved print choices are independent of the reading theme and window-local zoom.</summary>
   public Printing.PrintOptions Print { get; set; } = new();



   /// <inheritdoc/>
   public override void Sanitize()
   {
      base.Sanitize();
      Print = (Print ?? new Printing.PrintOptions()).Sanitize();
      ExternalEditor = Editors.EditorLaunch.IsExecutablePath(ExternalEditor) ? ExternalEditor : "";
      ConfiguredEditors = Editors.EditorConfiguration.Sanitize(ConfiguredEditors);
      if(ConfiguredEditors.Length == 0 && ExternalEditor.Length > 0)
      {
         ConfiguredEditors = [new Editors.EditorConfiguration(Guid.NewGuid(), "Editor", ExternalEditor)];
      }
      ExternalEditor = ConfiguredEditors.FirstOrDefault()?.Executable ?? "";
      SourceWidth = double.IsFinite(SourceWidth) ? Math.Clamp(SourceWidth, 160, 600) : 320;
      OutlineDepth = Math.Clamp(OutlineDepth, 1, 6);
      OutlineWidth = double.IsFinite(OutlineWidth) ? Math.Clamp(OutlineWidth, 140, 500) : 220;
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
