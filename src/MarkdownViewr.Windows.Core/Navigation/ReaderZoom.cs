/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows.Core.Navigation;

/// <summary>Keeps the pinned reference's bounded reading scale within one document window.</summary>
public sealed class ReaderZoom
{
   /// <summary>Starts at actual size and never persists across independent windows.</summary>
   public double Scale { get; private set; } = 1;



   /// <summary>Applies only explicit zoom commands, retaining 30–500 percent bounds.</summary>
   public void Apply(ReaderCommand command)
   {
      Scale = command switch
      {
         ReaderCommand.ZoomIn => Math.Min(Scale * 1.1, 5),
         ReaderCommand.ZoomOut => Math.Max(Scale / 1.1, 0.3),
         ReaderCommand.ZoomReset => 1,
         _ => Scale,
      };
   }
}
