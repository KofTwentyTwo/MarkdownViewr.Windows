/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;


namespace MarkdownViewr.Windows;

/// <summary>Synchronizes only owned native/browser viewports through validated numeric heading geometry.</summary>
public sealed partial class MainWindow
{
   private Core.Documents.DocumentSourceText? _sourceDocument;
   private ScrollViewer? _sourceScroller;
   private Microsoft.UI.Dispatching.DispatcherQueueTimer? _sourceSyncTimer;
   private bool _sourceSyncRunning;
   private bool _sourceScrolled;
   private bool _sourceCaptureAfterHold;
   private long _sourceSyncHoldUntil;
   private double _sourceLastY = double.NaN;
   private double _sourceReaderLastY = double.NaN;



   /// <summary>Polls fixed ready-page geometry without enabling authored scripts or messages; closing releases the timer.</summary>
   private void InitializeSourceSync()
   {
      _sourceSyncTimer = DispatcherQueue.CreateTimer();
      _sourceSyncTimer.Interval = TimeSpan.FromMilliseconds(150);
      _sourceSyncTimer.Tick += async (_, _) => await SynchronizeSourceAsync();
      SourceText.Loaded += (_, _) => AttachSourceScroller();
      SourceText.GettingFocus += Source_GettingFocus;
      _browser.DocumentReady += (_, _) => _sourceReaderLastY = double.NaN;
      Closed += (_, _) => _sourceSyncTimer.Stop();
      _sourceSyncTimer.Start();
   }



   /// <summary>Entering read-only source preserves its synchronized viewport instead of revealing the old native caret.</summary>
   private void Source_GettingFocus(UIElement sender, Microsoft.UI.Xaml.Input.GettingFocusEventArgs args)
   {
      if(_closed || !_settings.ShowSource || FindBar.Visibility == Visibility.Visible)
      {
         return;
      }
      AttachSourceScroller();
      if(_sourceScroller is not { } scroller)
      {
         return;
      }
      double position = scroller.VerticalOffset;
      if(string.Equals(Environment.GetEnvironmentVariable("MARKDOWNVIEWR_INPUT_TRACE"), "1", StringComparison.Ordinal))
      {
         App.Log.Info(FormattableString.Invariant($"[DEBUG-source-coordinates] focus entry; offset={position}; firstY={SourceText.GetRectFromCharacterIndex(0, false).Y}; selection={SourceText.Document.Selection.StartPosition}; state={SourceText.FocusState}."));
      }
      if(SourceText.SelectionLength == 0 && SourceText.TextLength > 0)
      {
         int first = 0;
         int last = SourceText.TextLength - 1;
         while(first < last)
         {
            int middle = first + (last - first) / 2;
            if(SourceText.GetRectFromCharacterIndex(middle, trailingEdge: false).Y < position)
            {
               first = middle + 1;
            }
            else
            {
               last = middle;
            }
         }
         SourceText.Select(first, 0);
      }
      Core.Documents.DocumentSourceText? snapshot = _sourceDocument;
      DispatcherQueue.TryEnqueue(() =>
      {
         if(!_closed && ReferenceEquals(snapshot, _sourceDocument) && SourceText.FocusState != FocusState.Unfocused)
         {
            _sourceSyncHoldUntil = Environment.TickCount64 + 200;
            scroller.ChangeView(null, position, null, disableAnimation: true);
            _sourceLastY = position;
            _sourceScrolled = false;
            if(string.Equals(Environment.GetEnvironmentVariable("MARKDOWNVIEWR_INPUT_TRACE"), "1", StringComparison.Ordinal))
            {
               App.Log.Info(FormattableString.Invariant($"[DEBUG-source-coordinates] focus restore; requested={position}; actual={scroller.VerticalOffset}; firstY={SourceText.GetRectFromCharacterIndex(0, false).Y}; selection={SourceText.Document.Selection.StartPosition}; state={SourceText.FocusState}."));
            }
         }
      });
   }



   /// <summary>Owns the actual native text viewport; source gestures can originate from mouse, keyboard or accessibility.</summary>
   private void AttachSourceScroller()
   {
      if(_sourceScroller is not null)
      {
         return;
      }
      SourceText.ApplyTemplate();
      _sourceScroller = FindSourceScroller(SourceText);
      if(_sourceScroller is not null)
      {
         _sourceScroller.ViewChanged += (_, _) =>
         {
            if(Environment.TickCount64 >= _sourceSyncHoldUntil)
            {
               _sourceScrolled = true;
            }
         };
      }
   }



   /// <summary>Walks only this owned control's native visual tree, never other applications or desktop windows.</summary>
   private static ScrollViewer? FindSourceScroller(DependencyObject element)
   {
      if(element is ScrollViewer scroller)
      {
         return scroller;
      }
      for(int index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
      {
         if(FindSourceScroller(VisualTreeHelper.GetChild(element, index)) is { } found)
         {
            return found;
         }
      }
      return null;
   }



   /// <summary>Search temporarily expands literal source; the canonical query/count remains independent of folding.</summary>
   private void ExpandSourceForFind()
   {
      if(_sourceDocument is not null && _sourceDocument.SetCollapsed([]))
      {
         RefreshSource();
      }
      HoldSourceSync();
   }



   /// <summary>Find's explicit paired selection/viewport operations cannot be overwritten by scroll feedback.</summary>
   private void HoldSourceSync(bool preserveSelection = true)
   {
      _sourceSyncHoldUntil = Environment.TickCount64 + 250;
      _sourceScrolled = false;
      _sourceCaptureAfterHold = preserveSelection;
      _sourceLastY = _sourceScroller?.VerticalOffset ?? double.NaN;
      _sourceReaderLastY = double.NaN;
   }



   /// <summary>Applies only current snapshot/query/page geometry and prevents reverse feedback after its own native movements.</summary>
   private async Task SynchronizeSourceAsync()
   {
      if(_closed || _printing || _sourceSyncRunning || !_settings.ShowSource || _finding || !_browser.IsReady || _sourceDocument is not { } source)
      {
         return;
      }
      AttachSourceScroller();
      if(_sourceScroller is not { } scroller)
      {
         return;
      }
      _sourceSyncRunning = true;
      int findGeneration = _findGeneration;
      try
      {
         Core.Documents.DocumentScrollState? frame = await _browser.CaptureScrollStateAsync();
         if(frame is null || _closed || !ReferenceEquals(source, _sourceDocument) || findGeneration != _findGeneration || !_settings.ShowSource || !_browser.IsReady || _finding)
         {
            return;
         }
         if(source.SetCollapsed(frame.Collapsed))
         {
            RefreshSource();
            if(FindBar.Visibility == Visibility.Visible && _find is not null)
            {
               RefreshFindResult();
            }
            return;
         }
         if(Environment.TickCount64 < _sourceSyncHoldUntil)
         {
            return;
         }
         if(_sourceCaptureAfterHold)
         {
            _sourceCaptureAfterHold = false;
            _sourceScrolled = false;
            _sourceLastY = scroller.VerticalOffset;
            _sourceReaderLastY = frame.Position;
            return;
         }
         await SynchronizeViewportsAsync(source, scroller, frame);
      }
      catch(Exception exception) when(exception is System.Runtime.InteropServices.COMException or InvalidOperationException or ArgumentException)
      {
         if(!_closed)
         {
            App.Log.Warning("The source reading position could not be synchronized.");
         }
      }
      finally
      {
         _sourceSyncRunning = false;
      }
   }



   /// <summary>Pairs visible native character rectangles with generated heading tops and moves only the surface that follows.</summary>
   private async Task SynchronizeViewportsAsync(Core.Documents.DocumentSourceText source, ScrollViewer scroller, Core.Documents.DocumentScrollState frame)
   {
      var anchors = new List<(double Source, double Reader)>();
      foreach(Core.Documents.DocumentSourceHeading heading in source.Headings)
      {
         if(heading.Start < SourceText.TextLength && frame.Headings.TryGetValue(heading.Index, out double readerTop))
         {
            global::Windows.Foundation.Rect rectangle = SourceText.GetRectFromCharacterIndex(heading.Start, trailingEdge: false);
            double sourceTop = rectangle.Y;
            if(rectangle.Height > 0 && double.IsFinite(sourceTop))
            {
               anchors.Add((sourceTop, readerTop));
            }
         }
      }
      double sourceY = scroller.VerticalOffset;
      bool sourceMoved = _sourceScrolled || double.IsFinite(_sourceLastY) && Math.Abs(sourceY - _sourceLastY) > 0.5;
      _sourceScrolled = false;
      if(sourceMoved && double.IsFinite(_sourceReaderLastY))
      {
         double target = Core.Documents.DocumentScrollState.MapPosition(sourceY, scroller.ScrollableHeight, frame.Maximum, anchors.Select(pair => (pair.Source, pair.Reader)));
         _sourceSyncHoldUntil = Environment.TickCount64 + 200;
         await _browser.ScrollToSourcePositionAsync(target);
         _sourceLastY = sourceY;
         _sourceReaderLastY = target;
      }
      else if(!double.IsFinite(_sourceReaderLastY) || Math.Abs(frame.Position - _sourceReaderLastY) > 0.5)
      {
         double target = Core.Documents.DocumentScrollState.MapPosition(frame.Position, frame.Maximum, scroller.ScrollableHeight, anchors.Select(pair => (pair.Reader, pair.Source)));
         _sourceSyncHoldUntil = Environment.TickCount64 + 200;
         scroller.ChangeView(null, target, null, disableAnimation: true);
         _sourceLastY = target;
         _sourceReaderLastY = frame.Position;
      }
   }
}
