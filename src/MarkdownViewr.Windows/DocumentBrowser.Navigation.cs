/*
 * Copyright (c) 2026 Darin Kelkhoff
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

namespace MarkdownViewr.Windows;

/// <summary>Executes fixed native reading operations without giving document content an input/message bridge.</summary>
internal sealed partial class DocumentBrowser
{
   /// <summary>Moves by computed line height, viewport fractions, document ends or visible generated headings.</summary>
   public Task NavigateReaderAsync(Core.Navigation.ReaderCommand command)
   {
      if(command is Core.Navigation.ReaderCommand.NextHeading or Core.Navigation.ReaderCommand.PreviousHeading)
      {
         return MoveVisibleHeadingAsync(command == Core.Navigation.ReaderCommand.NextHeading);
      }
      string? amount = command switch
      {
         Core.Navigation.ReaderCommand.LineDown => "line",
         Core.Navigation.ReaderCommand.LineUp => "-line",
         Core.Navigation.ReaderCommand.HalfPageDown => "s.clientHeight/2",
         Core.Navigation.ReaderCommand.HalfPageUp => "-s.clientHeight/2",
         Core.Navigation.ReaderCommand.PageUp => "-s.clientHeight",
         Core.Navigation.ReaderCommand.Top => "0",
         Core.Navigation.ReaderCommand.Bottom => "s.scrollHeight",
         _ => null,
      };
      if(amount is null)
      {
         return Task.CompletedTask;
      }
      string operation = command is Core.Navigation.ReaderCommand.Top or Core.Navigation.ReaderCommand.Bottom ? "scrollTo" : "scrollBy";
      return ExecuteDocumentScriptAsync("(()=>{const s=document.getElementById('content');if(!s)return;const c=getComputedStyle(document.body);let line=parseFloat(c.lineHeight);if(!Number.isFinite(line))line=parseFloat(c.fontSize)*1.5;if(!Number.isFinite(line)||line<=0)line=24;s." + operation + "({top:" + amount + ",behavior:'instant'});})()");
   }



   /// <summary>Heading sequences skip collapsed descendants and use document coordinates even inside nested sections.</summary>
   private Task<string?> MoveVisibleHeadingAsync(bool forward)
   {
      string direction = forward ? "top>y+2" : "top<y-2";
      string update = forward ? "target=h;break;" : "target=h;";
      return ExecuteDocumentScriptAsync(FormattableString.Invariant($$"""
         (()=>{
            const s=document.getElementById('content');if(!s)return;
            const y=s.scrollTop;let target=null;
            for(let i=0;i<{{_headings.Count}};i++){
               const h=document.getElementById('mv-heading-'+i);
               if(h&&h.offsetParent!==null){
                  const top=h.getBoundingClientRect().top-s.getBoundingClientRect().top+y;
                  if({{direction}}){ {{update}} }
               }
            }
            if(target)target.scrollIntoView({block:'start',behavior:'instant'});
         })()
         """));
   }
}
