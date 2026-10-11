/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.InteropServices;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;


namespace MarkdownViewr.Windows;

/// <summary>SDK rich file activation and command-line paths share one canonical native document request.</summary>
internal static partial class DesktopActivation
{
   /// <summary>Rich file payloads already identify local storage items; plain relative arguments are resolved at their launching process.</summary>
   internal static ActivationRequest ReadInitialRequest()
   {
      try
      {
         AppActivationArguments activation = AppInstance.GetCurrent().GetActivatedEventArgs();
         IEnumerable<string> paths = activation.Kind == ExtendedActivationKind.File && activation.Data is IFileActivatedEventArgs files
            ? files.Files.Select(item => item.Path)
            : Environment.GetCommandLineArgs().Skip(1);
         return ActivationRequest.Create(paths, Environment.CurrentDirectory);
      }
      catch(Exception exception) when(exception is ArgumentException or NotSupportedException or IOException or InvalidDataException or System.Runtime.InteropServices.COMException)
      {
         ReportFailure(exception);
         return new ActivationRequest([], HasInvalidPath: true);
      }
   }



   /// <summary>The foreground launcher permits the chosen primary to activate its existing document without simulating input.</summary>
   internal static void AllowPrimaryForeground(uint processId) => AllowSetForegroundWindow(processId);



   /// <summary>Logs activation failure kinds without document paths, JSON, profile identifiers or command-line content.</summary>
   internal static void ReportFailure(Exception exception)
      => AppIdentity.CreateLog().Warning(FormattableString.Invariant($"Document activation failed ({exception.GetType().Name}, 0x{exception.HResult:X8})."));



   /// <summary>Windows still applies its foreground policy; failure to grant focus never prevents opening the document.</summary>
   [LibraryImport("user32.dll")]
   [return: MarshalAs(UnmanagedType.Bool)]
   private static partial bool AllowSetForegroundWindow(uint processId);
}
