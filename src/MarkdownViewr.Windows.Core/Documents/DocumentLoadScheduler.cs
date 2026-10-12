/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core.Documents;

/// <summary>Serializes UI document requests, coalescing reloads while preserving a new picker selection.</summary>
public sealed class DocumentLoadScheduler
{
   private bool _active;
   private string? _pendingReloadPath;
   private string? _pendingOpenPath;



   /// <summary>Starts an idle loader; busy reloads coalesce and the latest explicit selection takes priority.</summary>
   public bool TryBegin(string path, bool reload)
   {
      if(!_active)
      {
         _active = true;
         return true;
      }
      if(reload)
      {
         _pendingReloadPath = path;
      }
      else
      {
         _pendingOpenPath = path;
      }
      return false;
   }



   /// <summary>Finishes one load and consumes at most one queued request for the current document.</summary>
   public (string Path, bool Reload)? Complete(string? currentPath)
   {
      _active = false;
      if(_pendingOpenPath is not null)
      {
         string path = _pendingOpenPath;
         _pendingOpenPath = null;
         return (path, false);
      }
      (string Path, bool Reload)? next = _pendingReloadPath is not null && currentPath is not null &&
         string.Equals(_pendingReloadPath, currentPath, StringComparison.Ordinal) ? (currentPath, true) : null;
      _pendingReloadPath = null;
      return next;
   }
}
