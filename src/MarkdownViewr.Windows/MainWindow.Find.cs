/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;


namespace MarkdownViewr.Windows;

/// <summary>Coordinates native find controls with literal source selection and the browser's own finder.</summary>
public sealed partial class MainWindow
{
   private Microsoft.UI.Dispatching.DispatcherQueueTimer? _findDelay;
   private Core.Documents.DocumentFind? _find;
   private int _findGeneration;
   private bool _finding;
   private bool _findQueued;
   private DocumentKeyboard? _documentKeyboard;



   /// <summary>Debounces typing and refreshes the query only after the current page has finished restoration.</summary>
   private void InitializeFind()
   {
      try
      {
         _documentKeyboard = new DocumentKeyboard(WinRT.Interop.WindowNative.GetWindowHandle(this), () =>
         {
            if(_closed)
            {
               return false;
            }
            if(string.Equals(Environment.GetEnvironmentVariable("MARKDOWNVIEWR_INPUT_TRACE"), "1", StringComparison.Ordinal))
            {
               App.Log.Info("Input trace: owned document find command queued.");
            }
            return DispatcherQueue.TryEnqueue(OpenFind);
         });
      }
      catch(System.ComponentModel.Win32Exception)
      {
         App.Log.Warning("The document keyboard shortcut could not be initialized; Edit → Find remains available.");
      }
      Closed += (_, _) => _documentKeyboard?.Dispose();
      _findDelay = DispatcherQueue.CreateTimer();
      _findDelay.Interval = TimeSpan.FromMilliseconds(200);
      _findDelay.IsRepeating = false;
      _findDelay.Tick += async (_, _) => await RunFindAsync();
      _browser.DocumentReady += (_, _) => QueueFind();
      _browser.FindCountChanged += (_, _) =>
      {
         if(!_finding && !_closed && _find is { } find && string.Equals(find.Query, FindText.Text, StringComparison.Ordinal) && FindBar.Visibility == Visibility.Visible)
         {
            find.SetRenderedCount(_browser.FindCount);
            RefreshFindResult();
         }
      };
   }



   /// <summary>The menu invokes the same command as the always-loaded window accelerator.</summary>
   private void Find_Click(object sender, RoutedEventArgs e) => OpenFind();



   /// <summary>Owns Ctrl+F independently of the closed menu flyout.</summary>
   private void Find_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
   {
      args.Handled = true;
      OpenFind();
   }



   /// <summary>Opens one native find toolbar and keeps keyboard focus in its text field.</summary>
   private void OpenFind()
   {
      if(_closed)
      {
         return;
      }
      FindBar.Visibility = Visibility.Visible;
      bool focused = FindText.Focus(FocusState.Programmatic);
      if(string.Equals(Environment.GetEnvironmentVariable("MARKDOWNVIEWR_INPUT_TRACE"), "1", StringComparison.Ordinal))
      {
         App.Log.Info(FormattableString.Invariant($"Input trace: find toolbar opened; focus accepted={focused}."));
      }
      FindText.SelectAll();
      QueueFind();
   }



   /// <summary>Typing is literal; only the latest query and committed snapshot can update the result.</summary>
   private void FindText_Changed(object sender, TextChangedEventArgs e) => QueueFind();



   /// <summary>Supersedes pending results without launching parallel browser searches.</summary>
   private void QueueFind()
   {
      _findGeneration++;
      if(_closed || _findDelay is null || FindBar.Visibility != Visibility.Visible)
      {
         return;
      }
      _find = null;
      FindCountText.Text = "Searching…";
      FindNextButton.IsEnabled = false;
      FindPreviousButton.IsEnabled = false;
      _findDelay.Stop();
      _findDelay.Start();
   }



   /// <summary>Serializes native sessions and discards old query/document completions.</summary>
   private async Task RunFindAsync()
   {
      if(_closed || FindBar.Visibility != Visibility.Visible || !_browser.IsReady)
      {
         return;
      }
      if(_finding)
      {
         _findQueued = true;
         return;
      }
      _finding = true;
      try
      {
         do
         {
            _findQueued = false;
            int generation = _findGeneration;
            string snapshot = _sourceMarkdown;
            var find = new Core.Documents.DocumentFind(_settings.ShowSource ? SourceText.Text : snapshot, FindText.Text);
            if(find.Query.Length == 0)
            {
               await _browser.EndFindAsync();
            }
            else
            {
               find.SetRenderedCount(await _browser.FindAsync(find.Query));
            }
            if(!_closed && generation == _findGeneration && string.Equals(snapshot, _sourceMarkdown, StringComparison.Ordinal) && FindBar.Visibility == Visibility.Visible && _browser.IsReady)
            {
               _find = find;
            }
            else if(!_closed && FindBar.Visibility == Visibility.Visible)
            {
               _findQueued = true;
            }
            else
            {
               await _browser.EndFindAsync();
            }
         } while(_findQueued && !_closed && FindBar.Visibility == Visibility.Visible && _browser.IsReady);
      }
      finally
      {
         _finding = false;
         if(!_closed)
         {
            RefreshFindResult();
         }
      }
   }



   /// <summary>Displays one paired count and selects only a valid literal source range.</summary>
   private void RefreshFindResult()
   {
      int current = _find?.Current ?? 0;
      int total = _find?.Total ?? 0;
      FindCountText.Text = FormattableString.Invariant($"{current} of {total}");
      FindNextButton.IsEnabled = total > 0 && !_finding;
      FindPreviousButton.IsEnabled = total > 0 && !_finding;
      if(_settings.ShowSource && _find?.SourceSelection is { } selection)
      {
         SourceText.Select(selection.Start, selection.Length);
      }
      else
      {
         SourceText.Select(0, 0);
      }
   }



   /// <summary>Moves one paired occurrence; browser-only and source-only tails retain their own valid selections.</summary>
   private void MoveFind(bool forward)
   {
      if(_finding || _find is not { Total: > 0 } find)
      {
         return;
      }
      if(forward)
      {
         find.Next();
      }
      else
      {
         find.Previous();
      }
      if(find.Current <= _browser.FindCount)
      {
         _browser.MoveFind(forward);
      }
      RefreshFindResult();
   }



   /// <summary>The native next button wraps through the same paired occurrence list.</summary>
   private void FindNext_Click(object sender, RoutedEventArgs e) => MoveFind(forward: true);



   /// <summary>The native previous button wraps from the first paired occurrence to the last.</summary>
   private void FindPrevious_Click(object sender, RoutedEventArgs e) => MoveFind(forward: false);



   /// <summary>Escape closes find; Enter and Shift+Enter move through occurrences without entering a newline.</summary>
   private async void FindText_KeyDown(object sender, KeyRoutedEventArgs e)
   {
      if(e.Key == global::Windows.System.VirtualKey.Escape)
      {
         e.Handled = true;
         await CloseFindAsync();
      }
      else if(e.Key == global::Windows.System.VirtualKey.Enter)
      {
         e.Handled = true;
         bool shift = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(global::Windows.System.VirtualKey.Shift) & global::Windows.UI.Core.CoreVirtualKeyStates.Down) != global::Windows.UI.Core.CoreVirtualKeyStates.None;
         MoveFind(forward: !shift);
      }
   }



   /// <summary>The close button clears highlights and restores pre-search section choices.</summary>
   private async void FindClose_Click(object sender, RoutedEventArgs e) => await CloseFindAsync();



   /// <summary>Invalidates pending queries before clearing both surfaces.</summary>
   private async Task CloseFindAsync()
   {
      _findGeneration++;
      _findDelay?.Stop();
      FindBar.Visibility = Visibility.Collapsed;
      _find = null;
      SourceText.Select(0, 0);
      await _browser.EndFindAsync();
   }
}
