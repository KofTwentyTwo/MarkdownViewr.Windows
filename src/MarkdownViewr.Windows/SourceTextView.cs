/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using MarkdownViewr.Windows.Core.Documents;
using MarkdownViewr.Windows.Core.Themes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NativeText = Microsoft.UI.Text;


namespace MarkdownViewr.Windows;

/// <summary>Read-only native source with passive character formatting, literal copying and real text accessibility.</summary>
public sealed partial class SourceTextView : RichEditBox, IDisposable
{
   private readonly global::Windows.UI.ViewManagement.AccessibilitySettings _accessibility = new();
   private readonly global::Windows.UI.ViewManagement.UISettings _systemColors = new();
   private CancellationTokenSource? _classification;
   private IReadOnlyList<SourceToken> _tokens = [];
   private ReaderTheme _theme = ThemeCatalog.Find(null);
   private DocumentSourceText? _snapshot;
   private string _input = "";
   private int _generation;
   private int _paintGeneration;
   private bool _disposed;



   /// <summary>Source never loads rich text or enables URL input; copy emits only deliberate plain text.</summary>
   public SourceTextView()
   {
      IsReadOnly = true;
      IsSpellCheckEnabled = false;
      IsTextPredictionEnabled = false;
      ClipboardCopyFormat = RichEditClipboardFormat.PlainText;
      InputScope = new InputScope();
      InputScope.Names.Add(new InputScopeName { NameValue = InputScopeNameValue.Default });
      ActualThemeChanged += ThemeChanged;
      _systemColors.ColorValuesChanged += SystemColorsChanged;
      CopyingToClipboard += CopySource;
   }



   /// <summary>Reads actual native plain text, excluding RichEdit's final paragraph sentinel.</summary>
   public string Text
   {
      get
      {
         Document.GetRange(0, TextLength).GetText(NativeText.TextGetOptions.None, out string text);
         return text;
      }
   }



   /// <summary>Literal UTF16 character count bounds native selection and geometry without repeatedly allocating the document.</summary>
   public int TextLength => _input.Length;



   /// <summary>Reports the actual native selection, keeping paired find and read-only source focus intact.</summary>
   public int SelectionLength => Document.Selection.EndPosition - Document.Selection.StartPosition;



   /// <summary>Commits a new shared snapshot only as literal native text; parser work can never replace or mutate it.</summary>
   public void SetSource(string text, DocumentSourceText? snapshot)
   {
      ArgumentNullException.ThrowIfNull(text);
      _snapshot = snapshot;
      if(string.Equals(_input, text, StringComparison.Ordinal))
      {
         return;
      }
      _input = text;
      WriteNative(() => Document.SetText(NativeText.TextSetOptions.None, text));
      _tokens = [];
      _classification?.Cancel();
      _classification?.Dispose();
      _classification = new CancellationTokenSource();
      _ = HighlightAsync(text, ++_generation, _classification.Token);
   }



   /// <summary>Palette changes recolor the current tokens without reparsing or altering the selected text.</summary>
   public void SetTheme(ReaderTheme theme)
   {
      _theme = theme;
      _ = PaintAsync();
   }



   /// <summary>Find selections remain native UTF16 ranges, never HTML or synthetic accessibility values.</summary>
   public void Select(int start, int length)
   {
      int first = Math.Clamp(start, 0, TextLength);
      Document.Selection.SetRange(first, Math.Clamp(start + length, first, TextLength));
   }



   /// <summary>Returns native paragraph geometry in unscrolled control coordinates, preserving the source scroll contract.</summary>
   public global::Windows.Foundation.Rect GetRectFromCharacterIndex(int index, bool trailingEdge)
   {
      int position = Math.Clamp(index + (trailingEdge ? 1 : 0), 0, TextLength);
      Document.GetRange(position, Math.Min(position + 1, TextLength)).GetRect(NativeText.PointOptions.ClientCoordinates | NativeText.PointOptions.AllowOffClient, out global::Windows.Foundation.Rect rectangle, out _);
      ScrollViewer? scroller = FindScroller(this);
      rectangle.Y += scroller?.VerticalOffset ?? 0;
      return rectangle;
   }



   /// <summary>Preserves native Text/Scroll patterns while exposing the actual read-only plain value for accessibility.</summary>
   protected override AutomationPeer OnCreateAutomationPeer() => new SourceTextAutomationPeer(this);



   /// <summary>Mode changes use native Windows colors; document CSS cannot style or execute within this control.</summary>
   private void ThemeChanged(FrameworkElement sender, object args) => _ = PaintAsync();



   /// <summary>System high contrast takes precedence over document accents and marshals back to the owned UI queue.</summary>
   private void SystemColorsChanged(global::Windows.UI.ViewManagement.UISettings sender, object args) => DispatcherQueue.TryEnqueue(() =>
   {
      if(!_disposed)
      {
         _ = PaintAsync();
      }
   });



   /// <summary>A native clipboard failure can be reported by the owning reader without exposing selected document content.</summary>
   public event EventHandler? CopyFailed;



   /// <summary>Only an explicit native Copy action receives exact original visible source, with no rich-text clipboard formats.</summary>
   private void CopySource(RichEditBox sender, TextControlCopyingToClipboardEventArgs args)
   {
      int start = Math.Clamp(Document.Selection.StartPosition, 0, TextLength);
      int end = Math.Clamp(Document.Selection.EndPosition, start, TextLength);
      string text = _snapshot?.GetCopyText(start, end - start) ?? Text.Substring(start, end - start);
      args.Handled = true;
      try
      {
         var package = new global::Windows.ApplicationModel.DataTransfer.DataPackage();
         package.SetText(text);
         global::Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
      }
      catch(System.Runtime.InteropServices.COMException exception)
      {
         App.Log.Warning(FormattableString.Invariant($"The source could not be copied ({exception.GetType().Name}, 0x{exception.HResult:X8})."));
         CopyFailed?.Invoke(this, EventArgs.Empty);
      }
   }



   /// <summary>Closing cancels stale classification and painting and releases the system accessibility subscription.</summary>
   public void Dispose()
   {
      if(!_disposed)
      {
         _disposed = true;
         _generation++;
         _paintGeneration++;
         _classification?.Cancel();
         _classification?.Dispose();
         _systemColors.ColorValuesChanged -= SystemColorsChanged;
         ActualThemeChanged -= ThemeChanged;
         CopyingToClipboard -= CopySource;
      }
      GC.SuppressFinalize(this);
   }



   /// <summary>Read-only RichEdit denies host range formatting; native writes run synchronously with read-only restored before yielding.</summary>
   private void WriteNative(Action write)
   {
      IsReadOnly = false;
      try
      {
         write();
         Document.ClearUndoRedoHistory();
      }
      finally
      {
         IsReadOnly = true;
      }
   }



   /// <summary>Recurses only through this source control's native template; no desktop or unrelated application is queried.</summary>
   private static ScrollViewer? FindScroller(DependencyObject element)
   {
      if(element is ScrollViewer scroller)
      {
         return scroller;
      }
      for(int index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
      {
         if(FindScroller(VisualTreeHelper.GetChild(element, index)) is { } found)
         {
            return found;
         }
      }
      return null;
   }



   /// <summary>Converts only validated six-digit RGB values to native colors, never authored CSS.</summary>
   private static global::Windows.UI.Color Color(string value) => global::Windows.UI.Color.FromArgb(255, byte.Parse(value.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture), byte.Parse(value.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture), byte.Parse(value.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
}
