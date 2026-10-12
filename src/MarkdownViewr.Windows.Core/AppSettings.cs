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



   /// <summary>Visible theme order, including disabled choices managed separately from their JSON data.</summary>
   public string[] ThemeOrder { get; set; } = [];



   /// <summary>Disabled choices remain editable but are omitted from reading menus and cycling.</summary>
   public string[] DisabledThemes { get; set; } = [];



   /// <summary>Hidden bundled themes can be restored without damaging shipped resources.</summary>
   public string[] DeletedBuiltInThemes { get; set; } = [];



   /// <summary>The frontmatter display mode, saved independently of document contents.</summary>
   public string Frontmatter { get; set; } = "Hide";



   /// <summary>Repairs unknown saved mode values to the reference's default.</summary>
   [JsonIgnore]
   public Documents.FrontmatterMode FrontmatterKind => Enum.TryParse(Frontmatter, out Documents.FrontmatterMode mode) && Enum.IsDefined(mode) ? mode : Documents.FrontmatterMode.Hide;


   /// <summary>Renders double-equals highlights; disabled syntax remains literal.</summary>
   public bool RenderHighlight { get; set; } = true;



   /// <summary>Renders single-caret superscripts independently of other Markdown extensions.</summary>
   public bool RenderSuperscript { get; set; } = true;



   /// <summary>Renders single-tilde subscripts while double-tilde GFM strikethrough remains enabled.</summary>
   public bool RenderSubscript { get; set; } = true;



   /// <summary>Renders double-plus inserted/underlined content.</summary>
   public bool RenderUnderline { get; set; } = true;



   /// <summary>An immutable snapshot captures this window's options before asynchronous parsing begins.</summary>
   [JsonIgnore]
   public Documents.MarkdownExtensions MarkdownExtensions => new(RenderHighlight, RenderSuperscript, RenderSubscript, RenderUnderline);



   /// <summary>Limits reader content width, independently of print layout.</summary>
   public bool ContentWidthEnabled { get; set; } = true;



   /// <summary>The reference's 1000-pixel default scales with reading zoom.</summary>
   public double ContentWidthPx { get; set; } = 1000;



   /// <summary>Native user CSS shared across themes; saved separately from each theme's own CSS.</summary>
   public string GlobalCss { get; set; } = "";



   /// <summary>An immutable window snapshot keeps asynchronous style changes independent of saved theme data.</summary>
   [JsonIgnore]
   public Themes.ReadingAppearance ReadingAppearance => new(ContentWidthEnabled, ContentWidthPx, GlobalCss);



   /// <summary>New document windows start with Contents hidden unless explicitly enabled in Settings.</summary>
   public bool ShowOutlineByDefault { get; set; }



   /// <summary>New document windows start with Source hidden unless explicitly enabled in Settings.</summary>
   public bool ShowSourceByDefault { get; set; }



   /// <summary>Shows the native heading sidebar in this window without changing the selected startup default.</summary>
   public bool ShowOutline { get; set; }



   /// <summary>Maximum heading depth displayed in the sidebar.</summary>
   public int OutlineDepth { get; set; } = 3;



   /// <summary>Long Contents entries wrap in full; otherwise they use one line with an ellipsis and full-title tooltip.</summary>
   public bool WrapOutlineEntries { get; set; }



   /// <summary>Optional native depth markers distinguish heading levels without changing titles or identities.</summary>
   public bool ShowOutlineDepthMarkers { get; set; }



   /// <summary>Last native sidebar width in device-independent pixels.</summary>
   public double OutlineWidth { get; set; } = 220;


   /// <summary>Shows the literal, read-only Markdown snapshot beside the rendered document.</summary>
   public bool ShowSource { get; set; }



   /// <summary>Legacy fixed-width preference retained for older settings; current source opens at50/50 and resizes within each window.</summary>
   public double SourceWidth { get; set; } = 320;



   /// <summary>Legacy single-editor path, migrated into the list and kept as the first choice for compatible settings readers.</summary>
   public string ExternalEditor { get; set; } = "";



   /// <summary>Ordered named editor choices; the legacy path migrates into one entry on load.</summary>
   public Editors.EditorConfiguration[] ConfiguredEditors { get; set; } = [];



   /// <summary>Private bounded successful-open history; clearable without deleting documents.</summary>
   public string[] RecentFiles { get; set; } = [];



   /// <summary>Saved print choices are independent of the reading theme and window-local zoom.</summary>
   public Printing.PrintOptions Print { get; set; } = new();



   /// <inheritdoc/>
   public override void Sanitize()
   {
      base.Sanitize();
      Themes.ReadingAppearance appearance = ReadingAppearance.Sanitize();
      ContentWidthPx = appearance.WidthPixels;
      GlobalCss = appearance.GlobalCss;
      RecentFiles = Documents.RecentDocuments.Sanitize(RecentFiles);
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
      ThemeOrder = Themes.ThemeManagement.SanitizeNames(ThemeOrder);
      DisabledThemes = Themes.ThemeManagement.SanitizeNames(DisabledThemes);
      DeletedBuiltInThemes = Themes.ThemeManagement.SanitizeNames(DeletedBuiltInThemes);
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
         _ = Themes.ReaderTheme.ParseCompatible(json);
         return true;
      }
      catch(JsonException)
      {
         return false;
      }
   }
}
