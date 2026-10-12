/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Globalization;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Identifiers;
using FlaUI.Core.Input;
using MarkdownViewr.Windows.Core.Documents;
using MarkdownViewr.Windows.Core.Themes;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Observes actual native source text-range colors and formatting, rather than mirrored parser/token diagnostics.</summary>
public partial class AppTests
{
   /// <summary>Moving the actual pointer into and out of the native source must retain semantic character colors.</summary>
   [Fact]
   public void SourceHighlighting_PointerHoverAndFocus_RetainsNativeTokenColors()
   {
      using var session = new AppSession(document: "# Hover highlighting\n\nPlain text and \u0060literal code\u0060.");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Hover highlighting")), "the hover regression fixture", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "SourceToggle");
      session.InvokeMenuItem("View", "ThemeLight");
      AutomationElement source = session.WaitForElement("SourceText");
      var palette = new SourcePalette(ThemeCatalog.Find("Catppuccin Mocha"), dark: false);
      WaitForNativeSourceColor(session, source, "Hover highlighting", palette.Color(SourceTokenKind.Heading));
      WaitForNativeSourceColor(session, source, "literal code", palette.Color(SourceTokenKind.Code));
      session.MainWindow.SetForeground();
      System.Drawing.Rectangle bounds = source.BoundingRectangle;
      var point = new System.Drawing.Point(bounds.Left + bounds.Width / 2, bounds.Top + Math.Min(40, bounds.Height / 2));
      NativeMouse.MoveTo(point);
      Wait.UntilInputIsProcessed();
      Assert.Equal(palette.Color(SourceTokenKind.Heading), NativeSourceColorHex(SourceRange(source, "Hover highlighting"), session));
      Assert.Equal(palette.Color(SourceTokenKind.Code), NativeSourceColorHex(SourceRange(source, "literal code"), session));
      source.Focus();
      SourceRange(source, "Plain text").Select();
      Wait.UntilInputIsProcessed();
      Assert.Equal(palette.Color(SourceTokenKind.Heading), NativeSourceColorHex(SourceRange(source, "Hover highlighting"), session));
      Assert.Equal(palette.Color(SourceTokenKind.Code), NativeSourceColorHex(SourceRange(source, "literal code"), session));
      Assert.True(source.Patterns.Value.Pattern.IsReadOnly.Value);
      RetainDocumentWindow(session, session.MainWindow, "source-hover-regression");
   }



   /// <summary>Converts the observed native COLORREF for exact before/after-hover assertions.</summary>
   private static string NativeSourceColorHex(ITextRange range, AppSession session)
   {
      int color = NativeSourceAttribute(range, session.Automation.TextAttributeLibrary.ForegroundColor);
      return FormattableString.Invariant($"#{color & 255:X2}{color >> 8 & 255:X2}{color >> 16 & 255:X2}");
   }



   /// <summary>Shell mode and document palette recolor native glyphs while exact text, selection and read-only source remain intact.</summary>
   [Fact]
   public void SourceHighlighting_ThemeModesAndPalette_UpdatesNativeRanges()
   {
      const string Markdown = "# Highlighting fixture\r\n\r\nPlain 😀 text and **bold** with *italic* and `code`.\n\n<script>literal()</script>\n\n[passive](https://example.invalid)";
      using var session = new AppSession(document: Markdown);
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Highlighting fixture")), "the source color fixture", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "SourceToggle");
      AutomationElement source = session.WaitForElement("SourceText");
      Assert.True(source.Patterns.Value.Pattern.IsReadOnly.Value);
      Assert.Equal(Markdown.ReplaceLineEndings("\r"), source.Patterns.Value.Pattern.Value.Value);
      session.InvokeMenuItem("View", "ThemeLight");
      AssertNativeSourcePalette(session, source, ThemeCatalog.Find("Catppuccin Mocha"), dark: false);
      session.InvokeMenuItem("View", "ThemeSystem");
      session.InvokeMenuItem("View", "ThemeLight");
      AssertNativeSourcePalette(session, source, ThemeCatalog.Find("Catppuccin Mocha"), dark: false);
      SourceRange(source, "Plain 😀 text").Select();
      string selected = ReadSourceSelection(source);
      session.InvokeMenuItem("View", "ThemeDark");
      AssertNativeSourcePalette(session, source, ThemeCatalog.Find("Catppuccin Mocha"), dark: true);
      Assert.Equal(selected, ReadSourceSelection(source));
      session.InvokeMenuItem("View", "DocumentThemesMenu");
      session.ClickElement("DocumentTheme:GitHub Light");
      AssertNativeSourcePalette(session, source, ThemeCatalog.Find("GitHub Light"), dark: true);
      Assert.Equal(selected, ReadSourceSelection(source));
      Assert.True(NativeSourceAttribute(SourceRange(source, "bold"), session.Automation.TextAttributeLibrary.FontWeight) >= 600);
      Assert.True(Assert.IsType<bool>(SourceRange(source, "italic").GetAttributeValue(session.Automation.TextAttributeLibrary.IsItalic)));
      Assert.Empty(source.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Hyperlink)));
      Assert.Equal(Markdown.ReplaceLineEndings("\r"), source.Patterns.Value.Pattern.Value.Value);
      RetainDocumentWindow(session, session.MainWindow, "source-native-dark-document-palette");
      File.WriteAllText(Path.Combine(session.DataDirectory, "document.md"), "# Fresh highlighting\n\nPlain 😀 text and `new code`.");
      session.WaitFor(() => source.Patterns.Value.Pattern.Value.Value.Contains("Fresh highlighting", StringComparison.Ordinal) ? source : null, "the committed highlighted reload snapshot");
      var palette = new SourcePalette(ThemeCatalog.Find("GitHub Light"), dark: true);
      WaitForNativeSourceColor(session, source, "new code", palette.Color(SourceTokenKind.Code));
   }



   /// <summary>Real native foreground attributes follow semantic spans and remain distinct from ordinary source text.</summary>
   private static void AssertNativeSourcePalette(AppSession session, AutomationElement source, ReaderTheme theme, bool dark)
   {
      var palette = new SourcePalette(theme, dark);
      WaitForNativeSourceColor(session, source, "Highlighting fixture", palette.Color(SourceTokenKind.Heading));
      WaitForNativeSourceColor(session, source, "Plain 😀 text", palette.Foreground);
      WaitForNativeSourceColor(session, source, "code", palette.Color(SourceTokenKind.Code));
      WaitForNativeSourceColor(session, source, "passive", palette.Color(SourceTokenKind.Link));
   }



   /// <summary>UIA's native color attribute is COLORREF, with red in the low byte.</summary>
   private static void WaitForNativeSourceColor(AppSession session, AutomationElement source, string literal, string expected)
   {
      int red = int.Parse(expected.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
      int green = int.Parse(expected.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
      int blue = int.Parse(expected.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
      int color = red | green << 8 | blue << 16;
      try
      {
         int stable = 0;
         session.WaitFor(() =>
         {
            stable = NativeSourceAttribute(SourceRange(source, literal), session.Automation.TextAttributeLibrary.ForegroundColor) == color ? stable + 1 : 0;
            return stable >= 3 ? source : null;
         }, "the actual native source color of " + literal);
      }
      catch(TimeoutException exception)
      {
         string actual = NativeSourceColorHex(SourceRange(source, literal), session);
         throw new TimeoutException(exception.Message + "; Expected=" + expected + "; Actual=" + actual + "; Last native invocation=" + session.LastInvocation + "; App logs: " + session.ReadLogs() + "; Native tree: " + session.DescribeDocumentStatus(), exception);
      }
   }



   /// <summary>Reads an actual native text range rather than using cached application source positions.</summary>
   private static ITextRange SourceRange(AutomationElement source, string literal)
      => source.Patterns.Text.Pattern.DocumentRange.FindText(literal, backward: false, ignoreCase: false) ?? throw new InvalidOperationException("The literal native source range is absent: " + literal);



   /// <summary>Only a native numeric attribute satisfies appearance assertions; unsupported/mixed attributes fail explicitly.</summary>
   private static int NativeSourceAttribute(ITextRange range, TextAttributeId attribute)
      => Assert.IsType<int>(range.GetAttributeValue(attribute));
}
