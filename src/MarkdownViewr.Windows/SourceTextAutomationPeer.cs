/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;


namespace MarkdownViewr.Windows;

/// <summary>Adds read-only native plain-value accessibility while preserving RichEdit's own actual text-range provider.</summary>
internal sealed class SourceTextAutomationPeer(SourceTextView owner) : RichEditBoxAutomationPeer(owner), IValueProvider
{
   /// <summary>Source cannot be edited by accessibility, just as it cannot be edited by keyboard or paste.</summary>
   public bool IsReadOnly => true;



   /// <summary>Returns the real native plain text, never a cached renderer diagnostic or synthetic test value.</summary>
   public string Value => owner.Text;



   /// <summary>Read-only source rejects accessibility editing rather than silently modifying the committed document.</summary>
   public void SetValue(string value) => throw new InvalidOperationException("Markdown source is read-only.");



   /// <summary>Only Value is extended; native RichEdit supplies Text, selection and scrolling patterns.</summary>
   protected override object GetPatternCore(PatternInterface patternInterface) => patternInterface == PatternInterface.Value ? this : base.GetPatternCore(patternInterface);
}
