/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Activation;
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
   /// <summary>The running reader's inbox; null in a process that could not create one or that opened its documents itself.</summary>
   internal static ActivationInbox? Inbox { get; private set; }



   /// <summary>The documents this launch was asked to open, resolved against its own working folder.</summary>
   internal static ActivationRequest InitialRequest { get; private set; } = new([]);



   /// <summary>
   /// Runs the installer hooks, then either hands this launch's documents to the reader that is
   /// already running or becomes a reader. A launch whose documents the running reader could not
   /// take opens them itself, so a double-clicked file always opens.
   /// </summary>
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
      var instance = AppInstance.FindOrRegisterForKey(key);
      if(!instance.IsCurrent && Forward(key, instance.ProcessId))
      {
         return 0;
      }
      ActivationInbox? inbox = instance.IsCurrent ? OpenInbox(key) : null;
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
         // The pipe name is released before the instance key, so the next reader to register can create its own inbox.
         inbox?.Dispose();
         if(instance.IsCurrent)
         {
            instance.UnregisterKey();
         }
      }
      return 0;
   }



   /// <summary>
   /// Creates the inbox, or returns null when the pipe name is taken. The reader then runs
   /// without one: later launches find nobody listening and open their documents themselves.
   /// </summary>
   private static ActivationInbox? OpenInbox(string key)
   {
      try
      {
         return new ActivationInbox(key, DesktopActivation.ReportFailure);
      }
      catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
      {
         DesktopActivation.ReportFailure(exception);
         return null;
      }
   }



   /// <summary>Hands this launch's documents to the running reader; false when it did not take them.</summary>
   private static bool Forward(string key, uint processId)
   {
      try
      {
         DesktopActivation.AllowPrimaryForeground(processId);
         // A plain pipe is used because the SDK's own redirection deadlocks an STA entry point.
         if(ActivationInbox.SendAsync(key, InitialRequest, CancellationToken.None).GetAwaiter().GetResult())
         {
            return true;
         }
         DesktopActivation.ReportFailure(new IOException("The running reader is busy."));
      }
      catch(Exception exception) when(exception is IOException or ArgumentException or InvalidDataException or OperationCanceledException or UnauthorizedAccessException)
      {
         DesktopActivation.ReportFailure(exception);
      }
      return false;
   }
}
