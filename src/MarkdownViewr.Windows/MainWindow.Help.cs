/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.InteropServices;
using MarkdownViewr.Windows.Core.Sharing;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;


namespace MarkdownViewr.Windows;

/// <summary>Opens only the app's fixed public Help destinations after deliberate native menu actions.</summary>
public sealed partial class MainWindow
{
   /// <summary>Shows this Windows app's repository in the user's default browser.</summary>
   private async void Repository_Click(object sender, RoutedEventArgs args) => await OpenHelpPageAsync(AppIdentity.Info.RepositoryUrl!);



   /// <summary>Offers the app's issue templates without sending private document data.</summary>
   private async void SubmitIssue_Click(object sender, RoutedEventArgs args) => await OpenHelpPageAsync(AppIdentity.IssuesUrl);



   /// <summary>Opens the questions and reports in this app's repository.</summary>
   private async void CommunitySupport_Click(object sender, RoutedEventArgs args) => await OpenHelpPageAsync(AppIdentity.SupportUrl);



   /// <summary>
   /// Starts an invitation in the person's email program, ready to address and send. Without an
   /// email program the invitation is put on the clipboard, so it can be pasted anywhere.
   /// </summary>
   private async void Share_Click(object sender, RoutedEventArgs args)
   {
      if(await LaunchAsync(ShareInvitation.MailLink))
      {
         return;
      }
      try
      {
         var invitation = new DataPackage();
         invitation.SetText(ShareInvitation.Text);
         Clipboard.SetContent(invitation);
         StatusText.Text = "No email app opened. The invitation was copied to the clipboard.";
      }
      catch(Exception exception) when(exception is COMException or UnauthorizedAccessException)
      {
         StatusText.Text = "The invitation could not be opened or copied. Check your default email app.";
         App.Log.Warning(FormattableString.Invariant($"The invitation could not be copied ({exception.GetType().Name}, 0x{exception.HResult:X8})."));
      }
   }



   /// <summary>A missing browser or OS launch failure is visible; no document path/content is included.</summary>
   private async Task OpenHelpPageAsync(Uri target)
   {
      if(!await LaunchAsync(target))
      {
         StatusText.Text = "The link could not be opened. Check your default browser.";
      }
   }



   /// <summary>Hands one of the app's fixed links to Windows. False when nothing opened it; the failure is logged without the link.</summary>
   private static async Task<bool> LaunchAsync(Uri target)
   {
      try
      {
         return await Launcher.LaunchUriAsync(target);
      }
      catch(Exception exception) when(exception is COMException or InvalidOperationException or ArgumentException)
      {
         App.Log.Warning(FormattableString.Invariant($"A Help link could not be opened ({exception.GetType().Name}, 0x{exception.HResult:X8})."));
         return false;
      }
   }
}
