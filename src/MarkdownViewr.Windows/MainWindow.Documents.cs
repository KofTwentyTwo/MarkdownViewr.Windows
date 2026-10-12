/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>Exposes deliberate native window and recent-file commands without giving document content activation access.</summary>
public sealed partial class MainWindow
{
   private string? _initialDocumentPath;
   private Core.AppSettings _lastSavedSettings;
   private bool _activationError;



   /// <summary>Rejected startup payloads produce a fixed native message without disclosing a path in logs.</summary>
   internal void ReportActivationFailure()
   {
      _activationError = true;
      StatusText.Text = "One or more startup file paths could not be opened.";
   }



   /// <summary>The successfully opened document or literal pending startup path; used only to focus duplicate requests.</summary>
   internal string? DocumentPath => _documentPath ?? _initialDocumentPath;



   /// <summary>A document-created link may focus only a reader whose committed physical snapshot belongs to its authorized folder.</summary>
   internal bool IsDocumentInside(string physicalRoot) => _documentPhysicalRoot?.StartsWith(physicalRoot, StringComparison.Ordinal) == true;



   /// <summary>Creates a genuinely blank window rather than re-reading the original process argument.</summary>
   private void NewWindow_Click(object sender, RoutedEventArgs args) => ((App)Application.Current).NewDocumentWindow();



   /// <summary>Closes just this native reader; sibling document watchers and zoom remain alive.</summary>
   private void CloseWindow_Click(object sender, RoutedEventArgs args) => Close();



   /// <summary>Rebuilds the private recent menu from the latest stored history, including a deliberate clear command.</summary>
   internal void RefreshRecentDocuments()
   {
      RecentDocumentsMenu.Items.Clear();
      string[] paths = AppIdentity.SettingsStore.Load().RecentFiles;
      for(int index = 0; index < paths.Length; index++)
      {
         var item = new MenuFlyoutItem { Text = Path.GetFileName(paths[index]), Tag = paths[index] };
         Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(item, "RecentDocument:" + index);
         item.Click += RecentDocument_Click;
         RecentDocumentsMenu.Items.Add(item);
      }
      if(paths.Length == 0)
      {
         RecentDocumentsMenu.Items.Add(new MenuFlyoutItem { Text = "No recent files", IsEnabled = false });
      }
      else
      {
         RecentDocumentsMenu.Items.Add(new MenuFlyoutSeparator());
         var clear = new MenuFlyoutItem { Text = "Clear recent files" };
         Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(clear, "ClearRecentDocuments");
         clear.Click += ClearRecentDocuments_Click;
         RecentDocumentsMenu.Items.Add(clear);
      }
   }



   /// <summary>Only an explicit native menu choice can activate a stored path; missing documents keep existing readers intact.</summary>
   private async void RecentDocument_Click(object sender, RoutedEventArgs args)
   {
      if(sender is FrameworkElement { Tag: string path })
      {
         await ((App)Application.Current).OpenDocumentAsync(this, path);
      }
   }



   /// <summary>Deletes history entries rather than files and reports an unsuccessful settings save.</summary>
   private void ClearRecentDocuments_Click(object sender, RoutedEventArgs args)
   {
      if(!((App)Application.Current).ClearRecentDocuments())
      {
         StatusText.Text = "Could not clear recent files.";
      }
   }
}
