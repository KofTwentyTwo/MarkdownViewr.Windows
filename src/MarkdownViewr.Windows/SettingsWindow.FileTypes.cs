/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>Default-file configuration is an explicit native Settings action owned by Windows, never by document content.</summary>
internal sealed partial class SettingsWindow
{
   private readonly TextBlock _defaultMarkdownStatus = new() { TextWrapping = TextWrapping.Wrap };
   private readonly Button _chooseDefaultFiles = new() { Content = "Choose default file types", HorizontalAlignment = HorizontalAlignment.Left, IsEnabled = false };
   private readonly TextBlock _unregisteredFilesGuidance = new() { Text = "Install or repair MarkdownViewr to make it available in Open with and Default Apps. This preview does not change Windows file associations.", TextWrapping = TextWrapping.Wrap };



   /// <summary>Installed builds offer the OS chooser; unregistered previews explain why they cannot become a registered default here.</summary>
   private void AddDefaultFileTypes(StackPanel page)
   {
      page.Children.Add(new TextBlock { Text = "Default file types", FontSize = 20, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 16, 0, 0) });
      var guidance = new TextBlock
      {
         Text = "Choose MarkdownViewr for .md, .markdown, .mdown or .mkd in Windows Default Apps. Plain text (.txt) is optional. Windows keeps your choices until you change them.",
         TextWrapping = TextWrapping.Wrap,
      };
      AutomationProperties.SetAutomationId(guidance, "SettingsDefaultFilesGuidance");
      page.Children.Add(guidance);
      AutomationProperties.SetAutomationId(_defaultMarkdownStatus, "SettingsDefaultMarkdownStatus");
      AutomationProperties.SetLiveSetting(_defaultMarkdownStatus, Microsoft.UI.Xaml.Automation.Peers.AutomationLiveSetting.Polite);
      AutomationProperties.SetAutomationId(_chooseDefaultFiles, "SettingsDefaultFilesButton");
      page.Children.Add(_defaultMarkdownStatus);
      var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
      var refresh = new Button { Content = "Refresh status" };
      AutomationProperties.SetAutomationId(refresh, "SettingsRefreshDefaultFiles");
      refresh.Click += (_, _) => RefreshDefaultFileTypes();
      _chooseDefaultFiles.Click += async (_, _) => await OpenDefaultFileTypesAsync();
      actions.Children.Add(_chooseDefaultFiles);
      actions.Children.Add(refresh);
      page.Children.Add(actions);
      page.Children.Add(_unregisteredFilesGuidance);
      RefreshDefaultFileTypes();
      Activated += (_, args) =>
      {
         if(args.WindowActivationState != WindowActivationState.Deactivated)
         {
            RefreshDefaultFileTypes();
         }
      };
   }



   /// <summary>Returning from Windows Settings refreshes effective status; unknown/error states never advertise a selected default.</summary>
   private void RefreshDefaultFileTypes()
   {
      if(_closed)
      {
         return;
      }
      try
      {
         _chooseDefaultFiles.IsEnabled = InstalledFileTypes.GetDefaultAppsUri() is not null;
         _unregisteredFilesGuidance.Visibility = _chooseDefaultFiles.IsEnabled ? Visibility.Collapsed : Visibility.Visible;
         _defaultMarkdownStatus.Text = !_chooseDefaultFiles.IsEnabled ? "This copy is not registered for Markdown defaults." : InstalledFileTypes.IsDefaultMarkdownViewer() switch
         {
            true => "Default viewer for .md files.",
            false => "Not the default viewer for .md files.",
            null => "Windows did not report the current .md default. Use Default Apps to check your choice.",
         };
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException)
      {
         _chooseDefaultFiles.IsEnabled = false;
         _defaultMarkdownStatus.Text = "Could not read the current .md default. Open Windows Settings → Apps → Default apps to check.";
         ReportError(exception);
      }
   }



   /// <summary>Revalidates the installed registration at click time and opens only Microsoft's fixed settings scheme.</summary>
   private async Task OpenDefaultFileTypesAsync()
   {
      if(_closed || _busy)
      {
         return;
      }
      _busy = true;
      try
      {
         Uri? target = InstalledFileTypes.GetDefaultAppsUri();
         if(target is null)
         {
            _status.Text = "Install or repair MarkdownViewr, then try again.";
         }
         else if(await global::Windows.System.Launcher.LaunchUriAsync(target))
         {
            if(!_closed)
            {
               _status.Text = "Choose MarkdownViewr for your Markdown extensions in Windows Default Apps. You can leave .txt unchanged.";
            }
         }
         else if(!_closed)
         {
            _status.Text = "Open Windows Settings → Apps → Default apps, search for .md, then choose MarkdownViewr.";
         }
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or System.Security.SecurityException or System.Runtime.InteropServices.COMException or InvalidOperationException or ArgumentException)
      {
         if(!_closed)
         {
            _status.Text = "Open Windows Settings → Apps → Default apps, search for .md, then choose MarkdownViewr.";
         }
         App.Log.Warning(FormattableString.Invariant($"Default Apps could not be opened ({exception.GetType().Name}, 0x{exception.HResult:X8})."));
      }
      finally
      {
         _busy = false;
      }
   }
}
