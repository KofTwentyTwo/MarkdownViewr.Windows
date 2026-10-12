/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core.Navigation;

/// <summary>Bounded native reading commands adapted from the pinned reference's vim navigation.</summary>
public enum ReaderCommand
{
   None,
   Prefix,
   LineDown,
   LineUp,
   HalfPageDown,
   HalfPageUp,
   PageUp,
   Top,
   Bottom,
   NextHeading,
   PreviousHeading,
   FocusFind,
   NextMatch,
   PreviousMatch,
   CloseFind,
   ZoomIn,
   ZoomOut,
   ZoomReset,
   Print,
   NextTheme,
   PreviousTheme,
}
