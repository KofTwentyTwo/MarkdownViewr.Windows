/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Documents;
using MarkdownViewr.Windows.Core.Themes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using NativeText = Microsoft.UI.Text;


namespace MarkdownViewr.Windows;

/// <summary>Parser classification runs off-thread; bounded native paint batches discard stale snapshots and palettes.</summary>
public sealed partial class SourceTextView
{
   /// <summary>Native hover/focus chrome remains intact; semantic document ranges own their foreground across visual states.</summary>
   protected override void OnApplyTemplate()
   {
      base.OnApplyTemplate();
      if(VisualTreeHelper.GetChildrenCount(this) > 0 && VisualTreeHelper.GetChild(this, 0) is FrameworkElement root)
      {
         foreach(VisualStateGroup group in VisualStateManager.GetVisualStateGroups(root))
         {
            foreach(VisualState state in group.States)
            {
               if(state.Storyboard is null)
               {
                  continue;
               }
               Microsoft.UI.Xaml.Media.Animation.Timeline[] foregroundAnimations = state.Storyboard.Children
                  .Where(animation => string.Equals(Microsoft.UI.Xaml.Media.Animation.Storyboard.GetTargetName(animation), "ContentElement", StringComparison.Ordinal)
                     && string.Equals(Microsoft.UI.Xaml.Media.Animation.Storyboard.GetTargetProperty(animation), "Foreground", StringComparison.Ordinal))
                  .ToArray();
               foreach(Microsoft.UI.Xaml.Media.Animation.Timeline animation in foregroundAnimations)
               {
                  state.Storyboard.Children.Remove(animation);
               }
            }
         }
      }
      _ = PaintAsync();
   }



   /// <summary>Only the most recent literal snapshot can install tokens after background parsing completes.</summary>
   private async Task HighlightAsync(string text, int generation, CancellationToken cancellationToken)
   {
      try
      {
         IReadOnlyList<SourceToken> tokens = await Task.Run(() => MarkdownSourceHighlighting.Parse(text, cancellationToken), cancellationToken);
         if(_disposed || generation != _generation || cancellationToken.IsCancellationRequested)
         {
            return;
         }
         _tokens = tokens;
         await PaintAsync();
      }
      catch(OperationCanceledException)
      {
         // A newer snapshot or closing window owns the source now.
      }
      catch(Exception exception)
      {
         App.Log.Warning(FormattableString.Invariant($"Source highlighting could not complete ({exception.GetType().Name}, 0x{exception.HResult:X8})."));
      }
   }



   /// <summary>Windows high contrast overrides semantic colors; ordinary light/dark modes use readable document accents.</summary>
   private async Task PaintAsync()
   {
      try
      {
         await PaintPaletteAsync();
      }
      catch(Exception exception)
      {
         App.Log.Warning(FormattableString.Invariant($"Source colors could not be applied ({exception.GetType().Name}, 0x{exception.HResult:X8})."));
      }
   }



   /// <summary>Applies the current native palette and yields between batches, leaving ordinary source readable on failure.</summary>
   private async Task PaintPaletteAsync()
   {
      if(_disposed)
      {
         return;
      }
      TraceSourceAppearance("paint start");
      int generation = ++_paintGeneration;
      int snapshot = _generation;
      var palette = new SourcePalette(_theme, ActualTheme == ElementTheme.Dark);
      bool highContrast = _accessibility.HighContrast;
      global::Windows.UI.Color foreground = highContrast ? _systemColors.UIElementColor(global::Windows.UI.ViewManagement.UIElementType.WindowText) : Color(palette.Foreground);
      global::Windows.UI.Color background = highContrast ? _systemColors.UIElementColor(global::Windows.UI.ViewManagement.UIElementType.Window) : Color(palette.Background);
      Background = new SolidColorBrush(background);
      Foreground = new SolidColorBrush(foreground);
      NativeText.ITextCharacterFormat defaults = Document.GetDefaultCharacterFormat();
      defaults.ForegroundColor = foreground;
      defaults.Bold = NativeText.FormatEffect.Off;
      defaults.Italic = NativeText.FormatEffect.Off;
      defaults.Strikethrough = NativeText.FormatEffect.Off;
      WriteNative(() =>
      {
         Document.SetDefaultCharacterFormat(defaults);
         Document.GetRange(0, TextLength).CharacterFormat = defaults;
      });
      IReadOnlyList<SourceToken> tokens = _tokens;
      for(int first = 0; first < tokens.Count; first += 256)
      {
         if(_disposed || generation != _paintGeneration || snapshot != _generation)
         {
            return;
         }
         PaintBatch(tokens, first, palette, foreground, highContrast);
         await Task.Yield();
      }
      TraceSourceAppearance("paint complete");
      if(string.Equals(Environment.GetEnvironmentVariable("MARKDOWNVIEWR_INPUT_TRACE"), "1", StringComparison.Ordinal))
      {
         DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => TraceSourceAppearance("after native queue"));
      }
   }



   /// <summary>Temporary fixture-only evidence distinguishes parser completion from native foreground replacement without text.</summary>
   private void TraceSourceAppearance(string operation)
   {
      if(_disposed || !string.Equals(Environment.GetEnvironmentVariable("MARKDOWNVIEWR_INPUT_TRACE"), "1", StringComparison.Ordinal))
      {
         return;
      }
      SourceToken? heading = _tokens.FirstOrDefault(token => token.Kind == SourceTokenKind.Heading);
      global::Windows.UI.Color? color = heading is null ? null : Document.GetRange(heading.Start, heading.Start + heading.Length).CharacterFormat.ForegroundColor;
      global::Windows.UI.Color? foreground = (Foreground as SolidColorBrush)?.Color;
      global::Windows.UI.Color? content = (GetTemplateChild("ContentElement") as Microsoft.UI.Xaml.Controls.ScrollViewer)?.Foreground is SolidColorBrush brush ? brush.Color : null;
      App.Log.Info(FormattableString.Invariant($"[DEBUG-source-colors] {operation}; snapshot={_generation}; paint={_paintGeneration}; tokens={_tokens.Count}; theme={ActualTheme}; focus={FocusState}; heading={color}; foreground={foreground}; content={content}; highContrast={_accessibility.HighContrast}."));
   }



   /// <summary>Independent ranges change character appearance only, retaining source selection, spelling and scroll positions.</summary>
   private void PaintBatch(IReadOnlyList<SourceToken> tokens, int first, SourcePalette palette, global::Windows.UI.Color foreground, bool highContrast)
   {
      WriteNative(() =>
      {
         Document.BatchDisplayUpdates();
         try
         {
            for(int index = first; index < Math.Min(first + 256, tokens.Count); index++)
            {
               SourceToken token = tokens[index];
               NativeText.ITextRange range = Document.GetRange(token.Start, token.Start + token.Length);
               NativeText.ITextCharacterFormat format = range.CharacterFormat;
               format.ForegroundColor = highContrast ? foreground : Color(palette.Color(token.Kind));
               if(token.Kind is SourceTokenKind.Heading or SourceTokenKind.Strong)
               {
                  format.Bold = NativeText.FormatEffect.On;
               }
               if(token.Kind is SourceTokenKind.Emphasis or SourceTokenKind.Quote)
               {
                  format.Italic = NativeText.FormatEffect.On;
               }
               if(token.Kind == SourceTokenKind.Strikethrough)
               {
                  format.Strikethrough = NativeText.FormatEffect.On;
               }
               range.CharacterFormat = format;
            }
         }
         finally
         {
            Document.ApplyDisplayUpdates();
         }
      });
   }
}
