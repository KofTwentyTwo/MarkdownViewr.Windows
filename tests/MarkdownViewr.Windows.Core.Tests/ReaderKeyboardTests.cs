/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Navigation;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Verifies all reading commands, focus/modifier guards and deterministic sequence timing.</summary>
public sealed class ReaderKeyboardTests
{
   /// <summary>Single keys retain the pinned reading, find and dismissal semantics.</summary>
   [Theory]
   [InlineData('j', ReaderCommand.LineDown)]
   [InlineData('k', ReaderCommand.LineUp)]
   [InlineData('G', ReaderCommand.Bottom)]
   [InlineData('n', ReaderCommand.NextMatch)]
   [InlineData('N', ReaderCommand.PreviousMatch)]
   [InlineData('/', ReaderCommand.FocusFind)]
   [InlineData('\u001B', ReaderCommand.CloseFind)]
   [InlineData('J', ReaderCommand.None)]
   [InlineData('x', ReaderCommand.None)]
   public void Resolve_SingleReadingKey_ReturnsCommand(char key, ReaderCommand expected)
      => Assert.Equal(expected, new ReaderKeyboard().Resolve(new ReaderKey(key), readingFocused: true, 100));



   /// <summary>Windows Ctrl+F owns native find, while half-page and reverse-page commands apply only to reading.</summary>
   [Theory]
   [InlineData('d', ReaderCommand.HalfPageDown)]
   [InlineData('u', ReaderCommand.HalfPageUp)]
   [InlineData('b', ReaderCommand.PageUp)]
   [InlineData('f', ReaderCommand.FocusFind)]
   [InlineData('F', ReaderCommand.FocusFind)]
   [InlineData('x', ReaderCommand.None)]
   [InlineData('D', ReaderCommand.HalfPageDown)]
   [InlineData('U', ReaderCommand.HalfPageUp)]
   [InlineData('B', ReaderCommand.PageUp)]
   public void Resolve_ControlKey_PreservesNativeCommand(char key, ReaderCommand expected)
      => Assert.Equal(expected, new ReaderKeyboard().Resolve(new ReaderKey(key, Control: true), readingFocused: true, 0));



   /// <summary>Two separate matching key presses complete each prefix before the 500 ms deadline.</summary>
   [Theory]
   [InlineData('g', ReaderCommand.Top)]
   [InlineData(']', ReaderCommand.NextHeading)]
   [InlineData('[', ReaderCommand.PreviousHeading)]
   public void Resolve_TwoKeys_CompletesBoundedSequence(char key, ReaderCommand expected)
   {
      var keyboard = new ReaderKeyboard();
      Assert.Equal(ReaderCommand.Prefix, keyboard.Resolve(new ReaderKey(key), true, 100));
      Assert.Equal(expected, keyboard.Resolve(new ReaderKey(key), true, 599));
      Assert.Equal(ReaderCommand.Prefix, keyboard.Resolve(new ReaderKey(key), true, 600));
   }



   /// <summary>Late keys, a backward clock and repeated keydown events start a new prefix without jumping.</summary>
   [Theory]
   [InlineData(600L, false)]
   [InlineData(99L, false)]
   [InlineData(200L, true)]
   public void Resolve_ExpiredOrRepeatedPrefix_DoesNotJump(long next, bool repeated)
   {
      var keyboard = new ReaderKeyboard();
      Assert.Equal(ReaderCommand.Prefix, keyboard.Resolve(new ReaderKey('g'), true, 100));
      Assert.Equal(ReaderCommand.Prefix, keyboard.Resolve(new ReaderKey('g', Repeat: repeated), true, next));
      Assert.Equal(ReaderCommand.Top, keyboard.Resolve(new ReaderKey('g'), true, next + 1));
   }



   /// <summary>Different prefixes and ordinary keys replace or clear the pending sequence.</summary>
   [Fact]
   public void Resolve_DifferentKeyOrExplicitReset_ClearsPreviousPrefix()
   {
      var keyboard = new ReaderKeyboard();
      Assert.Equal(ReaderCommand.Prefix, keyboard.Resolve(new ReaderKey('g'), true, 1));
      Assert.Equal(ReaderCommand.Prefix, keyboard.Resolve(new ReaderKey(']'), true, 2));
      Assert.Equal(ReaderCommand.NextHeading, keyboard.Resolve(new ReaderKey(']'), true, 3));
      Assert.Equal(ReaderCommand.Prefix, keyboard.Resolve(new ReaderKey('g'), true, 4));
      Assert.Equal(ReaderCommand.LineDown, keyboard.Resolve(new ReaderKey('j'), true, 5));
      Assert.Equal(ReaderCommand.Prefix, keyboard.Resolve(new ReaderKey('g'), true, 6));
      keyboard.Reset();
      Assert.Equal(ReaderCommand.Prefix, keyboard.Resolve(new ReaderKey('g'), true, 7));
   }



   /// <summary>Text-entry focus, Alt/Windows and unrelated modified commands never become vim actions.</summary>
   [Theory]
   [InlineData(false, false, false)]
   [InlineData(true, true, false)]
   [InlineData(true, false, true)]
   public void Resolve_NonReadingOrSystemModifier_LeavesInputAlone(bool focused, bool alt, bool windows)
   {
      var keyboard = new ReaderKeyboard();
      Assert.Equal(ReaderCommand.Prefix, keyboard.Resolve(new ReaderKey('g'), true, 0));
      Assert.Equal(ReaderCommand.None, keyboard.Resolve(new ReaderKey('g', Alt: alt, Windows: windows), focused, 1));
      Assert.Equal(ReaderCommand.Prefix, keyboard.Resolve(new ReaderKey('g'), true, 2));
      Assert.Equal(ReaderCommand.None, keyboard.Resolve(new ReaderKey('d', Control: true, Alt: alt, Windows: windows), false, 3));
   }



   /// <summary>Shift-modified Ctrl commands are rejected even if a keyboard layout reports a lowercase character.</summary>
   [Theory]
   [InlineData('d')]
   [InlineData('u')]
   [InlineData('b')]
   [InlineData('f')]
   public void Resolve_ShiftControlKey_LeavesInputAlone(char key)
      => Assert.Equal(ReaderCommand.None, new ReaderKeyboard().Resolve(new ReaderKey(key, Control: true, Shift: true), true, 0));


   /// <summary>Ctrl+F still works from native text controls; Ctrl+Shift+F remains available to those controls.</summary>
   [Fact]
   public void Resolve_TextControlFind_RequiresExactNativeModifiers()
   {
      var keyboard = new ReaderKeyboard();
      Assert.Equal(ReaderCommand.FocusFind, keyboard.Resolve(new ReaderKey('f', Control: true), false, 0));
      Assert.Equal(ReaderCommand.None, keyboard.Resolve(new ReaderKey('f', Control: true, Shift: true), false, 1));
   }
}
