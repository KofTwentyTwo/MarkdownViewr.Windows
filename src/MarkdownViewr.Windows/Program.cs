/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Velopack;


namespace MarkdownViewr.Windows;

/// <summary>
/// Custom entry point (DISABLE_XAML_GENERATED_MAIN) so Velopack handles its install,
/// update, and uninstall hooks before any UI exists.
/// </summary>
public static class Program
{
   /// <summary>The primary's bounded current-user inbox is attached by App only after the native reader initializes.</summary>
   internal static ActivationInbox? Inbox { get; private set; }



   /// <summary>The initial canonical request preserves sender-relative paths and SDK rich file activation.</summary>
   internal static ActivationRequest InitialRequest { get; private set; } = new([]);



   /// <summary>Runs install hooks before XAML, chooses a profile/install-specific primary, then starts or forwards the native document request.</summary>
   [STAThread]
   private static int Main()
   {
      // Keep Build/Run in this entry point: vpk verifies it before creating installers.
      VelopackApp.Build()
         .OnAfterInstallFastCallback(_ => InstalledFileTypes.Register())
         .OnAfterUpdateFastCallback(_ => InstalledFileTypes.Register())
         .OnBeforeUninstallFastCallback(_ => InstalledFileTypes.Unregister())
         .Run();
      WinRT.ComWrappersSupport.InitializeComWrappers();
      InitialRequest = DesktopActivation.ReadInitialRequest();
      string key = ActivationRequest.InstanceKey(AppContext.BaseDirectory, Path.GetDirectoryName(AppIdentity.Paths.SettingsFile)!);
      var primary = AppInstance.FindOrRegisterForKey(key);
      if(!primary.IsCurrent)
      {
         return ForwardToPrimary(key, primary.ProcessId);
      }
      using var inbox = new ActivationInbox(key, DesktopActivation.ReportFailure);
      Inbox = inbox;
      try
      {
         Microsoft.UI.Xaml.Application.Start(callbackParams =>
         {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            _ = new App();
         });
      }
      finally
      {
         Inbox = null;
         primary.UnregisterKey();
      }
      return 0;
   }



   /// <summary>Current-user .NET pipe delivery avoids the SDK STA redirection deadlock and resolves relative paths before crossing processes.</summary>
   private static int ForwardToPrimary(string key, uint processId)
   {
      try
      {
         DesktopActivation.AllowPrimaryForeground(processId);
         bool delivered = ActivationInbox.SendAsync(key, InitialRequest, CancellationToken.None).GetAwaiter().GetResult();
         if(!delivered)
         {
            DesktopActivation.ReportFailure(new IOException("The running reader is busy."));
         }
         return delivered ? 0 : 1;
      }
      catch(Exception exception) when(exception is IOException or ArgumentException or InvalidDataException or OperationCanceledException or UnauthorizedAccessException)
      {
         DesktopActivation.ReportFailure(exception);
         return 1;
      }
   }
}
