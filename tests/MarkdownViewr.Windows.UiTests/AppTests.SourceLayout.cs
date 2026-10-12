/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Compares actual native/browser pane geometry without counting the outline as document space.</summary>
public partial class AppTests
{
   /// <summary>UIA widths include display scaling; equal DIP columns must remain within two physical pixels.</summary>
   private static void AssertEqualSourcePanes(AppSession session)
   {
      try
      {
         session.WaitFor(() =>
         {
            AutomationElement source = session.WaitForElement("SourceText");
            AutomationElement document = session.WaitForElement("DocumentView");
            return source.BoundingRectangle.Width > 0 && Math.Abs(source.BoundingRectangle.Width - document.BoundingRectangle.Width) <= 2 ? source : null;
         }, "equal actual document/source widths, excluding the outline and dividers");
      }
      catch(TimeoutException exception)
      {
         throw new TimeoutException(exception.Message + "; Last native invocation=" + session.LastInvocation + "; Settings=" + session.ReadSettings() + "; Native tree: " + session.DescribeDocumentStatus(), exception);
      }
   }



   /// <summary>Moves the real owned divider and checks the pointer target before pressing a button.</summary>
   private static void DragSourceDivider(AppSession session, AutomationElement divider, int horizontalChange)
   {
      session.MainWindow.SetForeground();
      Assert.True(divider.TryGetClickablePoint(out System.Drawing.Point start));
      Assert.Equal("SourceDivider", session.Automation.FromPoint(start).Properties.AutomationId.ValueOrDefault);
      NativeMouse.MoveTo(start);
      Wait.UntilInputIsProcessed();
      Mouse.Down(MouseButton.Left);
      Wait.UntilInputIsProcessed();
      NativeMouse.MoveTo(new System.Drawing.Point(start.X + horizontalChange, start.Y));
      Wait.UntilInputIsProcessed();
      Mouse.Up(MouseButton.Left);
      Wait.UntilInputIsProcessed();
   }
}
