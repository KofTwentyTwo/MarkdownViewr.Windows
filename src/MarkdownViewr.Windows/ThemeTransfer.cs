/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text;
using System.Text.Json;
using MarkdownViewr.Windows.Core.Themes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;


namespace MarkdownViewr.Windows;

/// <summary>Deliberate native pickers transfer validated theme JSON without exposing document-created commands.</summary>
internal static class ThemeTransfer
{
   /// <summary>Counts are user-visible results; file paths/JSON are never written to the activity log.</summary>
   public sealed record ImportOutcome(AppSettings Draft, int Imported, int Skipped, int Failed);



   /// <summary>Multiple selected JSON files are read serially within the schema bound and each conflict has its own explicit choice.</summary>
   public static async Task<ImportOutcome?> ImportAsync(Microsoft.UI.WindowId owner, XamlRoot root, AppSettings settings)
   {
      var picker = new FileOpenPicker(owner);
      picker.FileTypeFilter.Add(".json");
      IReadOnlyList<PickFileResult> files = await picker.PickMultipleFilesAsync();
      if(files.Count == 0)
      {
         return null;
      }
      AppSettings draft = Core.SettingsChanges.Copy(settings);
      int imported = 0;
      int skipped = 0;
      int failed = 0;
      foreach(PickFileResult file in files)
      {
         try
         {
            await using var stream = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            ReaderTheme theme = await ThemeJson.ReadAsync(stream);
            if(theme.SchemaVersion > 1 && !await ConfirmNewerAsync(root, theme))
            {
               skipped++;
               continue;
            }
            ThemeImportResolution resolution = ThemeManagement.All(draft).Any(item => string.Equals(item.Name, theme.Name, StringComparison.Ordinal))
               ? await ResolveConflictAsync(root, theme) : ThemeImportResolution.KeepBoth;
            if(resolution == ThemeImportResolution.Skip)
            {
               skipped++;
               continue;
            }
            draft = ThemeManagement.Import(draft, theme, resolution, approveNewerSchema: theme.SchemaVersion > 1);
            imported++;
         }
         catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or JsonException or DecoderFallbackException)
         {
            failed++;
            App.Log.Warning(FormattableString.Invariant($"A selected theme could not be imported ({exception.GetType().Name}, 0x{exception.HResult:X8})."));
         }
      }
      return new ImportOutcome(draft, imported, skipped, failed);
   }



   /// <summary>The reference newer-schema warning requires confirmation even when the known fields validate.</summary>
   private static async Task<bool> ConfirmNewerAsync(XamlRoot root, ReaderTheme theme)
   {
      var dialog = new ContentDialog
      {
         XamlRoot = root,
         Title = "Theme uses a newer schema",
         Content = $"{theme.Name} uses theme schema {theme.SchemaVersion}. It may not display correctly. Import anyway?",
         PrimaryButtonText = "Import anyway",
         CloseButtonText = "Skip",
      };
      return await dialog.ShowAsync() == ContentDialogResult.Primary;
   }



   /// <summary>Closing a conflict skips it; Keep both retains both schemas and metadata under a deterministic unique name.</summary>
   private static async Task<ThemeImportResolution> ResolveConflictAsync(XamlRoot root, ReaderTheme theme)
   {
      var dialog = new ContentDialog
      {
         XamlRoot = root,
         Title = "Theme already exists",
         Content = $"A theme named {theme.Name} already exists. Choose how to handle the imported copy.",
         PrimaryButtonText = "Overwrite",
         SecondaryButtonText = "Keep both",
         CloseButtonText = "Skip",
         DefaultButton = ContentDialogButton.Secondary,
      };
      return await dialog.ShowAsync() switch
      {
         ContentDialogResult.Primary => ThemeImportResolution.Overwrite,
         ContentDialogResult.Secondary => ThemeImportResolution.KeepBoth,
         _ => ThemeImportResolution.Skip,
      };
   }



   /// <summary>Exporting a selected theme, including a disabled choice or draft, never changes saved settings.</summary>
   public static async Task<bool> ExportAsync(Microsoft.UI.WindowId owner, ReaderTheme theme)
   {
      var picker = new FileSavePicker(owner) { SuggestedFileName = ThemeJson.SuggestedFileName(theme), DefaultFileExtension = ".json" };
      picker.FileTypeChoices.Add("Theme JSON", [".json"]);
      PickFileResult? file = await picker.PickSaveFileAsync();
      if(file is null)
      {
         return false;
      }
      await File.WriteAllTextAsync(file.Path, theme.Json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
      return true;
   }



   /// <summary>A separate native selector exposes every visible theme for export, including disabled choices.</summary>
   public static async Task<ReaderTheme?> SelectExportAsync(XamlRoot root, AppSettings settings)
   {
      var choices = new ComboBox { Name = "ThemeExportChoice", Header = "Theme", MinWidth = 280 };
      foreach(ReaderTheme theme in ThemeManagement.All(settings))
      {
         var item = new ComboBoxItem { Content = theme.Name, Tag = theme };
         Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(item, "ThemeExport:" + theme.Name);
         choices.Items.Add(item);
      }
      choices.SelectedItem = choices.Items.OfType<ComboBoxItem>().FirstOrDefault(item => string.Equals(((ReaderTheme)item.Tag).Name, settings.DocumentTheme, StringComparison.Ordinal));
      if(choices.SelectedItem is null && choices.Items.Count > 0)
      {
         choices.SelectedIndex = 0;
      }
      var dialog = new ContentDialog { XamlRoot = root, Title = "Export document theme", Content = choices, PrimaryButtonText = "Export theme", CloseButtonText = "Cancel", IsPrimaryButtonEnabled = choices.Items.Count > 0 };
      ContentDialogResult result = await dialog.ShowAsync();
      return result == ContentDialogResult.Primary && choices.SelectedItem is ComboBoxItem { Tag: ReaderTheme selected } ? selected : null;
   }
}
