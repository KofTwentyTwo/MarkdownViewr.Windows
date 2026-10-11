/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;


namespace MarkdownViewr.Windows;

/// <summary>Routes native reading shortcuts through explicit focus checks and bounded browser commands.</summary>
public sealed partial class MainWindow
{
   private DocumentKeyboard? _documentKeyboard;
   private string? _readingFocusTrace;



   /// <summary>Registers only this window's thread and releases prefix/callback state on lifecycle changes.</summary>
   private void InitializeKeyboard()
   {
      try
      {
         _documentKeyboard = new DocumentKeyboard(WinRT.Interop.WindowNative.GetWindowHandle(this), IsReadingFocused, QueueReaderCommand);
      }
      catch(System.ComponentModel.Win32Exception)
      {
         App.Log.Warning("Document keyboard shortcuts could not be initialized; native menus remain available.");
      }
      Closed += (_, _) => _documentKeyboard?.Dispose();
      Activated += (_, args) =>
      {
         if(args.WindowActivationState == WindowActivationState.Deactivated)
         {
            _documentKeyboard?.Reset();
         }
      };
      _browser.DocumentReady += (_, _) => _documentKeyboard?.Reset();
   }



   /// <summary>Read-only source and the passive browser accept reading keys; find/dialog/other controls retain normal input.</summary>
   private bool IsReadingFocused()
   {
      if(_closed || !_browser.IsReady || Content.XamlRoot is not { } root)
      {
         return false;
      }
      object focused = FocusManager.GetFocusedElement(root);
      if(string.Equals(Environment.GetEnvironmentVariable("MARKDOWNVIEWR_INPUT_TRACE"), "1", StringComparison.Ordinal))
      {
         string observation = (focused as FrameworkElement)?.Name + ":" + focused?.GetType().Name;
         if(!string.Equals(observation, _readingFocusTrace, StringComparison.Ordinal))
         {
            _readingFocusTrace = observation;
            App.Log.Info("Input trace: reading focus " + observation + ".");
         }
      }
      return ReferenceEquals(focused, DocumentView) || _settings.ShowSource && ReferenceEquals(focused, SourceText);
   }



   /// <summary>Checks focus again on dispatch so queued reading commands cannot act after a text control takes focus.</summary>
   private bool QueueReaderCommand(Core.Navigation.ReaderCommand command)
   {
      if(_closed)
      {
         return false;
      }
      return DispatcherQueue.TryEnqueue(() => _ = ExecuteReaderCommandAsync(command));
   }



   /// <summary>Observes expected native lifetime failures while preserving the current document and normal input.</summary>
   private async Task ExecuteReaderCommandAsync(Core.Navigation.ReaderCommand command)
   {
      if(_closed || command != Core.Navigation.ReaderCommand.FocusFind && !IsReadingFocused())
      {
         _documentKeyboard?.Reset();
         return;
      }
      try
      {
         switch(command)
         {
            case Core.Navigation.ReaderCommand.FocusFind:
               OpenFind();
               break;
            case Core.Navigation.ReaderCommand.NextMatch:
               MoveFind(forward: true);
               break;
            case Core.Navigation.ReaderCommand.PreviousMatch:
               MoveFind(forward: false);
               break;
            case Core.Navigation.ReaderCommand.CloseFind:
               await CloseFindAsync();
               break;
            default:
               await _browser.NavigateReaderAsync(command);
               break;
         }
      }
      catch(Exception exception) when(exception is System.Runtime.InteropServices.COMException or ObjectDisposedException or InvalidOperationException)
      {
         if(!_closed)
         {
            StatusText.Text = "The reading command could not be completed.";
            App.Log.Warning("A native reading command could not be completed.");
         }
      }
   }
}
