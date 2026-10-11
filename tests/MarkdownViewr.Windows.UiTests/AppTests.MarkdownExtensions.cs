/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Drawing;
using System.Text.Json;
using FlaUI.Core.AutomationElements;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Exercises deliberate native Markdown toggles against actual rendered glyphs, painted highlights and literal source.</summary>
public partial class AppTests
{
   /// <summary>Independent options persist, repaint actual content and survive watched reload without rewriting the source.</summary>
   [Fact]
   public void Extensions_NativePreferences_PreserveLiteralSourceAndReload()
   {
      const string Heading = "==Highlighted== ^Power^ ~Sub~ ++Underlined++";
      const string Source = "# " + Heading + "\n\nLiteral extension acceptance.";
      using var session = new AppSession(document: Source);
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-0").And(cf.ByName("Highlighted Power Sub Underlined"))), "all enabled extension glyphs in the actual browser", TimeSpan.FromSeconds(30));
      WaitForHighlightPaint(session, visible: true);
      session.InvokeMenuItem("View", "SourceToggle");
      string literal = session.WaitForElement("SourceText").Patterns.Value.Pattern.Value.Value;
      Assert.Contains(Heading, literal, StringComparison.Ordinal);
      string[] ids = ["HighlightExtension", "SuperscriptExtension", "SubscriptExtension", "UnderlineExtension"];
      string[] titles = ["==Highlighted== Power Sub Underlined", "==Highlighted== ^Power^ Sub Underlined", "==Highlighted== ^Power^ ~Sub~ Underlined", Heading];
      for(int index = 0; index < ids.Length; index++)
      {
         session.InvokeMenuItem("View", "MarkdownExtensionsMenu");
         session.MouseClickElement(ids[index]);
         session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-0").And(cf.ByName(titles[index]))), "the actual heading after the independent " + ids[index] + " change");
         Assert.Equal(literal, session.WaitForElement("SourceText").Patterns.Value.Pattern.Value.Value);
      }
      WaitForHighlightPaint(session, visible: false);
      using(var saved = JsonDocument.Parse(session.ReadSettings()))
      {
         foreach(string property in new[] { "RenderHighlight", "RenderSuperscript", "RenderSubscript", "RenderUnderline" })
         {
            Assert.False(saved.RootElement.GetProperty(property).GetBoolean());
         }
      }
      File.WriteAllText(Path.Combine(session.DataDirectory, "document.md"), Source + "\n\nWatched extension reload.");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Watched extension reload.")), "actual watched content after disabled-extension preferences", TimeSpan.FromSeconds(30));
      Assert.Equal(Heading, session.WaitForElement("mv-heading-0").Name);
      Assert.Contains("Watched extension reload.", session.WaitForElement("SourceText").Patterns.Value.Pattern.Value.Value, StringComparison.Ordinal);
      session.InvokeMenuItem("View", "MarkdownExtensionsMenu");
      session.MouseClickElement("HighlightExtension");
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-0").And(cf.ByName("Highlighted ^Power^ ~Sub~ ++Underlined++"))), "highlight restored independently of the three disabled extensions");
      WaitForHighlightPaint(session, visible: true);
      Assert.Contains(Heading, session.WaitForElement("SourceText").Patterns.Value.Pattern.Value.Value, StringComparison.Ordinal);
      RetainDocumentWindow(session, session.MainWindow, "markdown-extensions");
   }



   /// <summary>Requires the selected theme's highlight pixels in the current browser heading, never a native mirrored preference.</summary>
   private static void WaitForHighlightPaint(AppSession session, bool visible)
   {
      session.WaitFor(() =>
      {
         AutomationElement heading = session.WaitForElement("mv-heading-0");
         using Bitmap image = heading.Capture();
         int pixels = CountPixels(image, color => color.ToArgb() == Color.FromArgb(249, 226, 175).ToArgb());
         return (visible ? pixels >= 50 : pixels == 0) ? heading : null;
      }, visible ? "actual Catppuccin highlight paint" : "the actual heading with highlight paint removed");
   }
}
