/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows;

/// <summary>Provides fixed numeric reading geometry without authored event handlers, scripts or messages.</summary>
internal sealed partial class DocumentBrowser
{
   /// <summary>Captures actual visible heading tops and hidden generated bodies only for the matching ready document.</summary>
   public async Task<Core.Documents.DocumentScrollState?> CaptureScrollStateAsync()
   {
      Uri document = _policy.DocumentUri;
      ulong navigation = _navigationId;
      string? json = await ExecuteDocumentScriptAsync(FormattableString.Invariant($$"""
         (()=>{
            const s=document.scrollingElement;if(!s)return null;
            const y=s.scrollTop;const headings=[];const collapsed=[];
            for(let i=0;i<{{_headings.Count}};i++){
               const h=document.getElementById('mv-heading-'+i);
               if(h&&h.offsetParent!==null)headings.push([i,h.getBoundingClientRect().top+y]);
               const b=document.querySelector('[data-mv-body="'+i+'"]');
               if(b&&b.hidden)collapsed.push(i);
            }
            return [y,Math.max(0,s.scrollHeight-s.clientHeight),headings,collapsed];
         })()
         """));
      return IsMatchingFindDocument(document, navigation) ? Core.Documents.DocumentScrollState.FromBrowserResult(json, _headings.Count) : null;
   }



   /// <summary>Scrolls only with a finite nonnegative host-computed pixel value; source text is never executable input.</summary>
   public Task ScrollToSourcePositionAsync(double top)
   {
      if(!double.IsFinite(top) || top < 0)
      {
         return Task.CompletedTask;
      }
      return ExecuteDocumentScriptAsync(FormattableString.Invariant($"window.scrollTo({{top:{top:R},behavior:'instant'}});"));
   }
}
