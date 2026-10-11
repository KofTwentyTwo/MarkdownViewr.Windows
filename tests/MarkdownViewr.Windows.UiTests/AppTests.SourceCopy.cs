/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Copies literal native selections with real Ctrl+C and observes the system clipboard rather than a source mirror.</summary>
public partial class AppTests
{
   /// <summary>Syntax styling never changes copied Unicode, whitespace, original newline spelling or the read-only source snapshot.</summary>
   [Fact]
   public void SourceHighlighting_CopyUnicodeAndNewlines_UsesPlainExactClipboard()
   {
      const string Markdown = "# Exact source copy\r\n\r\n**bold** 😀 café\n<script>literal</script>\r\nlast\r";
      using var session = new AppSession(document: Markdown);
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Exact source copy")), "the source copy fixture", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "SourceToggle");
      AutomationElement source = session.WaitForElement("SourceText");
      session.MouseClickElement("SourceText");
      source.Patterns.Text.Pattern.DocumentRange.Select();
      SendReadingKey(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_C);
      session.WaitFor(() => string.Equals(NativeClipboard.ReadText(), Markdown, StringComparison.Ordinal) ? source : null, "exact original source on the actual Unicode clipboard");
      Assert.False(NativeClipboard.HasRichText());
      SourceRange(source, "😀 café").Select();
      SendReadingKey(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_C);
      session.WaitFor(() => string.Equals(NativeClipboard.ReadText(), "😀 café", StringComparison.Ordinal) ? source : null, "the actual native Unicode selection copied without syntax colors");
      Assert.False(NativeClipboard.HasRichText());
      Keyboard.Type("Cannot edit source");
      Wait.UntilInputIsProcessed();
      Assert.Equal(Markdown.ReplaceLineEndings("\r"), source.Patterns.Value.Pattern.Value.Value);
      Assert.True(source.Patterns.Value.Pattern.IsReadOnly.Value);
   }
}
