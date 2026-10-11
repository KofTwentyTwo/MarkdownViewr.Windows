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
