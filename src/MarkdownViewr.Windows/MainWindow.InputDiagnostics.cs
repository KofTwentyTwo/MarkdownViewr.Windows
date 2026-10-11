/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;


namespace MarkdownViewr.Windows;

/// <summary>Opt-in native event evidence for isolated fixtures; never records document text or keystrokes.</summary>
public sealed partial class MainWindow
{
   /// <summary>Observes handled events without changing routing, focus, capture or control behavior.</summary>
   private void InitializeInputDiagnostics()
   {
      if(!string.Equals(Environment.GetEnvironmentVariable("MARKDOWNVIEWR_INPUT_TRACE"), "1", StringComparison.Ordinal))
      {
         return;
      }
      foreach(UIElement element in new UIElement[] { RootGrid, OutlineDivider, SourceDivider, FrontmatterHide, FrontmatterMetadata, FrontmatterMarkdown })
      {
         element.AddHandler(UIElement.PointerEnteredEvent, new PointerEventHandler((sender, args) => TracePointer("entered", sender, args)), handledEventsToo: true);
         element.AddHandler(UIElement.PointerExitedEvent, new PointerEventHandler((sender, args) => TracePointer("exited", sender, args)), handledEventsToo: true);
         element.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((sender, args) => TracePointer("pressed", sender, args)), handledEventsToo: true);
         element.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler((sender, args) => TracePointer("released", sender, args)), handledEventsToo: true);
         element.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler((sender, args) => TracePointer("capture-lost", sender, args)), handledEventsToo: true);
      }
      OutlineDivider.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler((sender, args) => TracePointer("moved", sender, args)), handledEventsToo: true);
      OutlineDivider.DragStarted += (_, args) => App.Log.Info(FormattableString.Invariant($"Input trace: outline drag started; x={args.HorizontalOffset}; y={args.VerticalOffset}."));
      OutlineDivider.DragDelta += (_, args) => App.Log.Info(FormattableString.Invariant($"Input trace: outline drag delta; x={args.HorizontalChange}; y={args.VerticalChange}."));
      OutlineDivider.DragCompleted += (_, args) => App.Log.Info(FormattableString.Invariant($"Input trace: outline drag completed; canceled={args.Canceled}."));
      foreach(Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem item in new[] { FrontmatterHide, FrontmatterMetadata, FrontmatterMarkdown })
      {
         item.Click += (_, _) => App.Log.Info($"Input trace: {item.Name} click; checked={item.IsChecked}.");
      }
   }



   /// <summary>Records only owned native control names, button state and local pointer coordinates.</summary>
   private void TracePointer(string operation, object sender, PointerRoutedEventArgs args)
   {
      Microsoft.UI.Input.PointerPoint point = args.GetCurrentPoint((UIElement)sender);
      App.Log.Info(FormattableString.Invariant($"Input trace: {operation}; target={((FrameworkElement)sender).Name}; source={(args.OriginalSource as FrameworkElement)?.Name}; left={point.Properties.IsLeftButtonPressed}; handled={args.Handled}; x={point.Position.X}; y={point.Position.Y}."));
   }
}
