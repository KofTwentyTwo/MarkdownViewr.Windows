/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core.Navigation;

/// <summary>Resolves the reference's vim sequences without interfering with text entry or modified native shortcuts.</summary>
public sealed class ReaderKeyboard
{
   private char _prefix;
   private long _prefixAt;



   /// <summary>Uses injected monotonic milliseconds; a held prefix cannot imitate a deliberate two-key sequence.</summary>
   public ReaderCommand Resolve(ReaderKey key, bool readingFocused, long milliseconds)
   {
      if(key.Alt || key.Windows)
      {
         Reset();
         return ReaderCommand.None;
      }
      if(key.Control)
      {
         Reset();
         return ResolveControl(key, readingFocused);
      }
      if(!readingFocused)
      {
         Reset();
         return ReaderCommand.None;
      }
      long elapsed = milliseconds - _prefixAt;
      if(_prefix != '\0' && key.Character == _prefix && !key.Repeat && elapsed is >= 0 and < 500)
      {
         char prefix = _prefix;
         Reset();
         return prefix switch { 'g' => ReaderCommand.Top, ']' => ReaderCommand.NextHeading, _ => ReaderCommand.PreviousHeading };
      }
      Reset();
      if(key.Character is 'g' or ']' or '[')
      {
         _prefix = key.Character;
         _prefixAt = milliseconds;
         return ReaderCommand.Prefix;
      }
      return key.Character switch
      {
         'j' => ReaderCommand.LineDown,
         'k' => ReaderCommand.LineUp,
         'G' => ReaderCommand.Bottom,
         'n' => ReaderCommand.NextMatch,
         'N' => ReaderCommand.PreviousMatch,
         '/' => ReaderCommand.FocusFind,
         '\u001B' => ReaderCommand.CloseFind,
         _ => ReaderCommand.None,
      };
   }



   /// <summary>Windows find ignores Caps Lock; Shift-modified commands remain available to native controls.</summary>
   private static ReaderCommand ResolveControl(ReaderKey key, bool readingFocused)
   {
      if(readingFocused && key.Character is '+' or '=' && (!key.Shift || key.Character == '+'))
      {
         return ReaderCommand.ZoomIn;
      }
      if(readingFocused && !key.Shift && key.Character is '-' or '0')
      {
         return key.Character == '-' ? ReaderCommand.ZoomOut : ReaderCommand.ZoomReset;
      }
      if(key.Shift)
      {
         return ReaderCommand.None;
      }
      if(readingFocused && char.ToUpperInvariant(key.Character) == 'P')
      {
         return ReaderCommand.Print;
      }
      if(char.ToUpperInvariant(key.Character) == 'F')
      {
         return ReaderCommand.FocusFind;
      }
      return readingFocused ? char.ToLowerInvariant(key.Character) switch
      {
         'd' => ReaderCommand.HalfPageDown,
         'u' => ReaderCommand.HalfPageUp,
         'b' => ReaderCommand.PageUp,
         _ => ReaderCommand.None,
      } : ReaderCommand.None;
   }


   /// <summary>Clears only the bounded prefix on focus, page or window changes; no arbitrary key history is stored.</summary>
   public void Reset() => _prefix = '\0';
}
