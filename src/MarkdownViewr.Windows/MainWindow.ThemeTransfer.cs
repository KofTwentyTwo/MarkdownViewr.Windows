/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */



namespace MarkdownViewr.Windows;

/// <summary>Native theme transfer commits only explicitly selected, validated imports or user-chosen export destinations.</summary>
public sealed partial class MainWindow
{
   private bool _transferringThemes;



   /// <summary>Picker cancellation leaves settings intact; rejected files do not prevent other selected themes from importing.</summary>
   private async Task ImportThemesAsync()
   {
      if(_closed || _transferringThemes)
      {
         return;
      }
      _transferringThemes = true;
      try
      {
         Core.AppSettings before = Core.SettingsChanges.Copy(_settings);
         ThemeTransfer.ImportOutcome? result = await ThemeTransfer.ImportAsync(ConfigurationWindowId, ConfigurationRoot, before);
         if(result is not null && !_closed)
         {
            if(result.Imported > 0 && !await CommitThemeDraftAsync(before, result.Draft))
            {
               return;
            }
            if(!_closed)
            {
               StatusText.Text = FormattableString.Invariant($"Themes imported: {result.Imported}. Skipped: {result.Skipped}. Failed: {result.Failed}.");
            }
         }
      }
      catch(Exception exception) when(exception is System.Runtime.InteropServices.COMException or ObjectDisposedException or InvalidOperationException or ArgumentException)
      {
         ReportThemeTransferError(exception);
      }
      finally
      {
         _transferringThemes = false;
      }
   }



   /// <summary>Export offers all visible themes and writes the exact validated JSON after native destination confirmation.</summary>
   private async Task ExportThemesAsync()
   {
      if(_closed || _transferringThemes)
      {
         return;
      }
      _transferringThemes = true;
      try
      {
         Core.Themes.ReaderTheme? theme = await ThemeTransfer.SelectExportAsync(ConfigurationRoot, _settings);
         if(theme is not null && !_closed && await ThemeTransfer.ExportAsync(ConfigurationWindowId, theme) && !_closed)
         {
            StatusText.Text = "Theme exported.";
         }
      }
      catch(Exception exception) when(exception is System.Runtime.InteropServices.COMException or ObjectDisposedException or InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException)
      {
         ReportThemeTransferError(exception);
      }
      finally
      {
         _transferringThemes = false;
      }
   }



   /// <summary>Errors disclose only exception kind/HRESULT; paths and theme content stay private.</summary>
   private void ReportThemeTransferError(Exception exception)
   {
      if(!_closed)
      {
         StatusText.Text = "Theme transfer could not be completed. Check the selected file or destination.";
         App.Log.Warning(FormattableString.Invariant($"Theme transfer could not be completed ({exception.GetType().Name}, 0x{exception.HResult:X8})."));
      }
   }
}
