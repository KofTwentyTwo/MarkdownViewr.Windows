/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Printing;
using MarkdownViewr.Windows.Core.Themes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;


namespace MarkdownViewr.Windows;

/// <summary>A native Printing settings page saves defaults without opening a printer or exporting a file.</summary>
internal sealed partial class SettingsWindow
{
   private readonly PrintOptionsEditor _printEditor;
   private readonly Func<PrintOptions, bool> _savePrint;
   private PrintOptions _savedPrint;
   private bool _printDirty;



   /// <summary>Print/PDF choices share one form; Apply saves the draft and Restore discards it without changing stored defaults.</summary>
   private StackPanel PrintingPage()
   {
      var page = new StackPanel { Spacing = 16, MaxWidth = 720 };
      page.Children.Add(new TextBlock { Text = "Choose the defaults for printing and PDF export. Apply saves these choices; the Print dialog lets you adjust them for your next output.", TextWrapping = TextWrapping.Wrap });
      page.Children.Add(_printEditor.Fields);
      var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
      var apply = new Button { Content = "Apply printing defaults" };
      var restore = new Button { Content = "Restore saved choices" };
      AutomationProperties.SetAutomationId(apply, "SettingsApplyPrint");
      AutomationProperties.SetAutomationId(restore, "SettingsRestorePrint");
      apply.Click += (_, _) => SavePrint();
      restore.Click += (_, _) =>
      {
         _printDirty = false;
         UpdatePrinting(_savedPrint);
      };
      actions.Children.Add(apply);
      actions.Children.Add(restore);
      page.Children.Add(actions);
      page.Children.Add(new TextBlock { Text = "PDF destinations and Open PDF/Open folder actions are selected when exporting. This page never sends a print job.", TextWrapping = TextWrapping.Wrap, Opacity = 0.7 });
      _printEditor.Changed += (_, _) =>
      {
         if(!_updating)
         {
            _printDirty = true;
         }
      };
      return page;
   }



   /// <summary>Sibling saves refresh the baseline while retaining an unsubmitted print draft.</summary>
   internal void UpdatePrinting(PrintOptions options)
   {
      if(_closed)
      {
         return;
      }
      _savedPrint = options;
      bool previous = _updating;
      _updating = true;
      try
      {
         if(!_printDirty)
         {
            _printEditor.Update(options);
         }
      }
      finally
      {
         _updating = previous;
      }
   }



   /// <summary>New/disabled/deleted themes refresh the shared choices without erasing other draft options.</summary>
   internal void UpdatePrintThemes(IEnumerable<ReaderTheme> themes)
   {
      if(_closed)
      {
         return;
      }
      bool previous = _updating;
      _updating = true;
      try
      {
         _printEditor.SetThemes(themes);
      }
      finally
      {
         _updating = previous;
      }
   }



   /// <summary>Only valid explicit Apply persists; failure leaves the draft available to correct or retry.</summary>
   private void SavePrint()
   {
      if(_closed || _busy || _printEditor.Read() is not { } options)
      {
         return;
      }
      try
      {
         if(_savePrint(options))
         {
            _printDirty = false;
            UpdatePrinting(options);
            _status.Text = "Printing defaults saved.";
         }
         else
         {
            _status.Text = "Could not save printing defaults. Please try again.";
         }
      }
      catch(Exception exception)
      {
         ReportError(exception);
      }
   }
}
