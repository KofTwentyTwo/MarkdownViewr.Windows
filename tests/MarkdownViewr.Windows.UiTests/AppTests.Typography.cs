/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Drives the complete native typography draft and checks actual browser glyphs rather than saved settings alone.</summary>
public partial class AppTests
{
   /// <summary>All fonts/sizes save, real heading/body/code glyphs grow and cancellation preserves the accepted theme.</summary>
   [Fact]
   public void ThemeEditor_AllTypography_PaintsActualGlyphsAndPreservesCancel()
   {
      using var session = new AppSession(document: "# Heading font probe\n\nBody font probe\n\n`Code font probe`\n\n## Heading two\n\n### Heading three\n\n#### Heading four\n\n##### Heading five\n\n###### Heading six");
      AutomationElement heading = session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-0")), "the typography fixture heading", TimeSpan.FromSeconds(30));
      double headingHeight = heading.BoundingRectangle.Height;
      double bodyHeight = WaitForTypographyText(session, "Body font probe").BoundingRectangle.Height;
      double codeHeight = WaitForTypographyText(session, "Code font probe").BoundingRectangle.Height;
      Assert.True(headingHeight > 0 && bodyHeight > 0 && codeHeight > 0);
      OpenSettingsAction(session, "EditDocumentThemeMenu");
      session.EnterText("ThemeEditorName", "Typography acceptance");
      foreach(string id in new[] { "ThemeEditorBodyFont", "ThemeEditorHeadingFont", "ThemeEditorCodeFont" })
      {
         session.EnterText(id, "Consolas");
      }
      // Give native rounded glyph metrics a clear growth margin while retaining the40% assertion.
      var sizes = new Dictionary<string, (string Id, double Value)>(StringComparer.Ordinal)
      {
         ["baseFontSize"] = ("ThemeEditorBodySize", 28),
         ["h1Size"] = ("ThemeEditorHeading1Size", 56),
         ["h2Size"] = ("ThemeEditorHeading2Size", 48),
         ["h3Size"] = ("ThemeEditorHeading3Size", 44),
         ["h4Size"] = ("ThemeEditorHeading4Size", 40),
         ["h5Size"] = ("ThemeEditorHeading5Size", 38),
         ["h6Size"] = ("ThemeEditorHeading6Size", 36),
         ["codeFontSize"] = ("ThemeEditorCodeSize", 20),
         ["lineHeight"] = ("ThemeEditorLineHeight", 2),
      };
      foreach((string _, (string id, double value)) in sizes)
      {
         session.WaitForElement(id).Patterns.RangeValue.Pattern.SetValue(value);
      }
      session.ClickButton("Save theme");
      session.WaitForElementGone("ThemeEditorName");
      CloseSettings(session);
      AssertSavedTypography(session, sizes);
      try
      {
         session.WaitFor(() =>
         {
            AutomationElement current = session.WaitForElement("mv-heading-0");
            return current.BoundingRectangle.Height > headingHeight * 1.4 &&
               WaitForTypographyText(session, "Body font probe").BoundingRectangle.Height > bodyHeight * 1.4 &&
               WaitForTypographyText(session, "Code font probe").BoundingRectangle.Height > codeHeight * 1.15 ? current : null;
         }, "actual heading, body and code glyph growth after all typography edits");
      }
      catch(TimeoutException exception)
      {
         string heights = FormattableString.Invariant($"Heading={headingHeight:R}/{session.WaitForElement("mv-heading-0").BoundingRectangle.Height:R}; Body={bodyHeight:R}/{WaitForTypographyText(session, "Body font probe").BoundingRectangle.Height:R}; Code={codeHeight:R}/{WaitForTypographyText(session, "Code font probe").BoundingRectangle.Height:R}");
         throw new TimeoutException(exception.Message + "; Initial/current actual glyph heights: " + heights + "; Native tree: " + session.DescribeDocumentStatus(), exception);
      }
      string accepted = session.ReadSettings();
      double acceptedHeight = session.WaitForElement("mv-heading-0").BoundingRectangle.Height;
      OpenSettingsAction(session, "EditDocumentThemeMenu");
      session.EnterText("ThemeEditorHeadingFont", "Arial");
      session.WaitForElement("ThemeEditorBodySize").Patterns.RangeValue.Pattern.SetValue(36);
      session.ClickButton("Cancel");
      session.WaitForElementGone("ThemeEditorName");
      CloseSettings(session);
      Assert.Equal(accepted, session.ReadSettings());
      Assert.Equal(acceptedHeight, session.WaitForElement("mv-heading-0").BoundingRectangle.Height);
      RetainDocumentWindow(session, session.MainWindow, "full-typography");
   }



   /// <summary>Checks every accepted native control against the actual persisted theme schema.</summary>
   private static void AssertSavedTypography(AppSession session, IReadOnlyDictionary<string, (string Id, double Value)> sizes)
   {
      using(var settings = JsonDocument.Parse(session.ReadSettings()))
      {
         Assert.Equal("Typography acceptance", settings.RootElement.GetProperty("DocumentTheme").GetString());
         using var theme = JsonDocument.Parse(Assert.Single(settings.RootElement.GetProperty("UserThemes").EnumerateArray()).GetString()!);
         foreach(string key in new[] { "body", "heading", "code" })
         {
            Assert.Equal("Consolas", theme.RootElement.GetProperty("fonts").GetProperty(key).GetString());
         }
         foreach((string key, (string _, double value)) in sizes)
         {
            Assert.Equal(value, theme.RootElement.GetProperty("sizes").GetProperty(key).GetDouble());
         }
      }
   }



   /// <summary>Requires an actual browser text glyph, excluding the native editor's labels and controls.</summary>
   private static AutomationElement WaitForTypographyText(AppSession session, string text)
      => session.WaitFor(() => session.WaitForElement("DocumentView").FindFirstDescendant(cf => cf.ByName(text).And(cf.ByControlType(ControlType.Text))), "the actual " + text + " browser glyph");
}
