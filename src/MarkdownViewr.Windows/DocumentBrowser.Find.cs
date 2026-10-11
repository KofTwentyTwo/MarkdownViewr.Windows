/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;


namespace MarkdownViewr.Windows;

/// <summary>Native browser search accepts a literal term without executing it or exposing a host bridge.</summary>
internal sealed partial class DocumentBrowser
{
   private int[]? _findCollapsed;
   private bool _findSubscribed;
   private string _findQuery = "";



   /// <summary>Signals that restoration finished on the current intercepted page.</summary>
   public event EventHandler? DocumentReady;



   /// <summary>Reports native browser counts after an active find session changes.</summary>
   public event EventHandler? FindCountChanged;



   /// <summary>Search must wait for the active page, never a pending old document.</summary>
   public bool IsReady => !_closed && _pageReady;



   /// <summary>The rendered count is independent of literal source occurrences.</summary>
   public int FindCount => !_closed && _findSubscribed && _pageReady && IsCurrentDocument() ? _view.CoreWebView2.Find.MatchCount : 0;



   /// <summary>Expands sections during search and uses the runtime's safe native finder and highlight support.</summary>
   public async Task<int> FindAsync(string query)
   {
      if(_closed || !_pageReady || !IsCurrentDocument() || _environment is null)
      {
         return 0;
      }
      Uri document = _policy.DocumentUri;
      ulong navigation = _navigationId;
      try
      {
         CoreWebView2Find finder = _view.CoreWebView2.Find;
         if(!_findSubscribed)
         {
            finder.MatchCountChanged += (_, _) =>
            {
               if(!_closed && _pageReady && IsCurrentDocument())
               {
                  FindCountChanged?.Invoke(this, EventArgs.Empty);
               }
            };
            _findSubscribed = true;
         }
         if(_findCollapsed is null)
         {
            await CaptureViewportAsync();
            if(!IsMatchingFindDocument(document, navigation))
            {
               return 0;
            }
            _findCollapsed = _collapsedSections.ToArray();
         }
         await ExecuteDocumentScriptAsync("for(const e of document.querySelectorAll('[data-mv-body]')) e.hidden=false;");
         if(!IsMatchingFindDocument(document, navigation))
         {
            return 0;
         }
         finder.Stop();
         _findQuery = query;
         CoreWebView2FindOptions options = _environment.CreateFindOptions();
         options.FindTerm = query;
         options.IsCaseSensitive = false;
         options.ShouldMatchWord = false;
         options.ShouldHighlightAllMatches = true;
         options.SuppressDefaultFindDialog = true;
         await finder.StartAsync(options);
         return IsMatchingFindDocument(document, navigation) ? finder.MatchCount : 0;
      }
      catch(Exception exception) when(exception is COMException or NotImplementedException)
      {
         if(!_closed)
         {
            _status("Find could not search this document. Check that the Microsoft Edge WebView2 Runtime is up to date.");
         }
         return 0;
      }
   }



   /// <summary>Moves the real native browser highlight; the paired source count is controlled separately.</summary>
   public void MoveFind(bool forward)
   {
      if(_closed || !_pageReady || _findQuery.Length == 0 || !IsCurrentDocument())
      {
         return;
      }
      try
      {
         if(forward)
         {
            _view.CoreWebView2.Find.FindNext();
         }
         else
         {
            _view.CoreWebView2.Find.FindPrevious();
         }
      }
      catch(COMException)
      {
         if(!_closed)
         {
            _status("Find could not move to this occurrence.");
         }
      }
   }



   /// <summary>Removes native highlights and restores the section choices from before this search.</summary>
   public async Task EndFindAsync()
   {
      if(_closed)
      {
         return;
      }
      Uri document = _policy.DocumentUri;
      if(_findSubscribed)
      {
         _view.CoreWebView2.Find.Stop();
      }
      _findQuery = "";
      if(_findCollapsed is not { } collapsed)
      {
         return;
      }
      _findCollapsed = null;
      _collapsedSections = collapsed;
      string indices = JsonSerializer.Serialize(collapsed);
      await ExecuteDocumentScriptAsync("if(location.href.split('#')[0] === '" + document.AbsoluteUri + "') for(const e of document.querySelectorAll('[data-mv-body]')) e.hidden=" + indices + ".includes(Number(e.dataset.mvBody));");
   }



   /// <summary>Before navigation, preserve the pre-search collapse choices only for the same heading sequence.</summary>
   private void ResetFindForNavigation()
   {
      if(_findSubscribed)
      {
         _view.CoreWebView2.Find.Stop();
      }
      if(_findCollapsed is { } collapsed)
      {
         _collapsedSections = collapsed;
      }
      _findCollapsed = null;
      _findQuery = "";
   }



   /// <summary>Rejects stale continuations after reload, theme navigation, close or another document open.</summary>
   private bool IsMatchingFindDocument(Uri document, ulong navigation)
      => !_closed && _pageReady && document == _policy.DocumentUri && navigation == _navigationId && IsCurrentDocument();
}
