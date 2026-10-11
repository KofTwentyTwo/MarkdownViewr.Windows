/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.Patterns;
using FlaUI.Core.WindowsAPI;
using Xunit.Abstractions;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>
/// End-to-end tests of the real app: the AppKit shell pieces and the document surface.
/// UI tests share one desktop, so they never run in parallel.
/// </summary>
[Collection("Desktop")]
public class AppTests
{
   private readonly ITestOutputHelper _output;



   /// <summary>Records input observations in both passing and failing test results.</summary>
   public AppTests(ITestOutputHelper output) => _output = output;



   /// <summary>The native command launches the chosen real fixture process with one literal document path.</summary>
   [Fact]
   public void Editor_ConfiguredExecutable_LaunchesLiteralDocument()
   {
      using var session = new AppSession(document: "# Editor fixture", editorProbe: true, documentFileName: "notes & café $HOME.md");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Editor fixture")), "the editor fixture heading", TimeSpan.FromSeconds(30));
      string editor = ResolveEditorProbe();
      session.InvokeMenuItem("File", "ConfigureEditorMenuItem");
      session.EnterText("EditorPath", editor);
      session.ClickButton("Save");
      session.WaitForElementGone("EditorPath");
      using(var settings = JsonDocument.Parse(session.ReadSettings()))
      {
         Assert.Equal(editor, settings.RootElement.GetProperty("ExternalEditor").GetString());
      }
      session.InvokeMenuItem("File", "OpenEditorMenuItem");
      string resultPath = Path.Combine(session.DataDirectory, "editor-result.json");
      session.WaitFor(() => File.Exists(resultPath) ? session.MainWindow : null, "the actual editor fixture's result file");
      using var result = JsonDocument.Parse(File.ReadAllText(resultPath));
      Assert.Equal(Path.Combine(session.DataDirectory, "notes & café $HOME.md"), Assert.Single(result.RootElement.GetProperty("Arguments").EnumerateArray()).GetString());
      Assert.Equal(session.DataDirectory, result.RootElement.GetProperty("WorkingDirectory").GetString());
      Assert.NotNull(session.FindInApp(cf => cf.ByName("Editor fixture")));
      Assert.DoesNotContain("notes & café", session.ReadLogs(), StringComparison.Ordinal);
   }



   /// <summary>A missing choice keeps the dialog open; cancel does not change saved settings.</summary>
   [Fact]
   public void Editor_InvalidExecutableAndCancel_DoesNotPersist()
   {
      using var session = new AppSession();
      session.InvokeMenuItem("File", "ConfigureEditorMenuItem");
      session.EnterText("EditorPath", Path.Combine(session.DataDirectory, "missing.exe"));
      session.ClickButton("Save");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Enter the full path of an existing .exe file.")), "the editor validation error");
      Assert.NotNull(session.FindInApp(cf => cf.ByAutomationId("EditorPath")));
      session.ClickButton("Cancel");
      session.WaitForElementGone("EditorPath");
      Assert.DoesNotContain("missing.exe", session.ReadSettings(), StringComparison.Ordinal);
   }



   /// <summary>An editor uninstalled after configuration reports the problem without launching anything.</summary>
   [Fact]
   public void Editor_MissingConfiguredExecutable_PreservesDocument()
   {
      using var session = new AppSession(document: "# Missing editor fixture");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Missing editor fixture")), "the fixture heading", TimeSpan.FromSeconds(30));
      string editor = Path.Combine(session.DataDirectory, "removed-editor.exe");
      File.WriteAllText(editor, "Never executed; presence-only configuration fixture.");
      session.InvokeMenuItem("File", "ConfigureEditorMenuItem");
      session.EnterText("EditorPath", editor);
      session.ClickButton("Save");
      session.WaitForElementGone("EditorPath");
      File.Delete(editor);
      session.InvokeMenuItem("File", "OpenEditorMenuItem");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("The configured editor is missing. Use File → Configure editor to choose an installed executable.")), "the missing editor status");
      Assert.NotNull(session.FindInApp(cf => cf.ByName("Missing editor fixture")));
   }



   /// <summary>Uses the solution's existing test-only executable, never a real user's editor.</summary>
   private static string ResolveEditorProbe()
   {
      for(DirectoryInfo? current = new(AppContext.BaseDirectory); current is not null; current = current.Parent)
      {
         if(File.Exists(Path.Combine(current.FullName, "MarkdownViewr.Windows.slnx")))
         {
            string path = Path.Combine(current.FullName, "tests", "MarkdownViewr.Windows.UpdateProbe", "bin", "Debug", "net10.0-windows", "MarkdownViewr.Windows.UpdateProbe.exe");
            return File.Exists(path) ? path : throw new FileNotFoundException("Build the solution's test-only editor probe first.", path);
         }
      }
      throw new FileNotFoundException("Could not locate the solution's editor probe.");
   }


   /// <summary>Splash: shows, then dismisses itself.</summary>
   [Fact]
   public void Splash_ShowsThenDismissesItself()
   {
      using var session = new AppSession(showSplash: true);
      session.WaitForElement("AppKitSplash");
      session.WaitForElementGone("AppKitSplash", TimeSpan.FromSeconds(15));
   }



   /// <summary>About: shows the build version.</summary>
   [Fact]
   public void About_ShowsTheBuildVersion()
   {
      using var session = new AppSession();
      session.InvokeMenuItem("Help", "AboutMenuItem");
      AutomationElement version = session.WaitForElement("AboutVersionText");
      Assert.StartsWith("Version ", version.Name, StringComparison.Ordinal);
      session.ClickButton("Close");
      session.WaitForElementGone("AboutVersionText");
   }



   /// <summary>Activity log: opens a window showing the log.</summary>
   [Fact]
   public void ActivityLog_OpensAWindowShowingTheLog()
   {
      using var session = new AppSession();
      session.InvokeMenuItem("Help", "ActivityLogMenuItem");
      string title = AppIdentity.Info.DisplayName + " — Activity log";
      session.WaitFor(
         () => session.TopLevelWindows().FirstOrDefault(window => string.Equals(window.Name, title, StringComparison.Ordinal)),
         "the activity log window");
   }



   /// <summary>Theme: the choice is saved to settings.json.</summary>
   [Fact]
   public void ThemeChoice_IsSaved()
   {
      using var session = new AppSession();
      session.InvokeMenuItem("View", "ThemeDark");
      _output.WriteLine(session.LastInvocation);
      _output.WriteLine(session.LastMouseInput);
      try
      {
         session.WaitFor(
            () => session.ReadSettings().Contains("\"Theme\": \"Dark\"", StringComparison.Ordinal) ? session.MainWindow : null,
            "settings.json to record the dark theme");
      }
      catch(TimeoutException exception)
      {
         throw new TimeoutException($"{exception.Message} {session.LastInvocation} {session.LastMouseInput} Settings: {session.ReadSettings()}. App logs: {session.ReadLogs()}", exception);
      }
   }



   /// <summary>The original document themes persist independently and keep the displayed document intact.</summary>
   [Fact]
   public void DocumentTheme_ChangedChoice_PersistsAndRetainsDocument()
   {
      using var session = new AppSession(document: "# Theme acceptance");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Theme acceptance")), "the document heading", TimeSpan.FromSeconds(30));
      WaitForDocumentColor(session, System.Drawing.Color.FromArgb(30, 30, 46));
      session.InvokeMenuItem("View", "DocumentThemesMenu");
      session.ClickElement("DocumentTheme:GitHub Light");
      session.WaitFor(
         () => session.ReadSettings().Contains("\"DocumentTheme\": \"GitHub Light\"", StringComparison.Ordinal) ? session.MainWindow : null,
         "the selected document theme to be saved");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Theme acceptance")), "the document after its theme changes");
      WaitForDocumentColor(session, System.Drawing.Color.White);
      _output.WriteLine(session.DescribeDocumentStatus());
      Assert.DoesNotContain("\"Theme\": \"Light\"", session.ReadSettings(), StringComparison.Ordinal);
   }


   /// <summary>A named custom theme saves all editing controls and applies its CSS to the actual browser.</summary>
   [Fact]
   public void ThemeEditor_NewCustomTheme_SavesAndPaintsChanges()
   {
      using var session = new AppSession(document: "# Custom theme acceptance\n\n`sample code`");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Custom theme acceptance")), "the document heading", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "EditDocumentThemeMenu");
      session.EnterText("ThemeEditorName", "My reading theme");
      session.EnterText("ThemeEditorCodeFont", "Consolas");
      session.WaitForElement("ThemeEditorCodeSize").Patterns.RangeValue.Pattern.SetValue(17);
      session.EnterText("ThemeEditorColor:background", "#112233");
      session.EnterText("ThemeEditorCss", "html { background: #223344; }");
      session.ClickButton("Save theme");
      session.WaitForElementGone("ThemeEditorName");
      WaitForDocumentColor(session, System.Drawing.Color.FromArgb(34, 51, 68));
      using var settings = JsonDocument.Parse(session.ReadSettings());
      Assert.Equal("My reading theme", settings.RootElement.GetProperty("DocumentTheme").GetString());
      using var theme = JsonDocument.Parse(Assert.Single(settings.RootElement.GetProperty("UserThemes").EnumerateArray()).GetString()!);
      Assert.Equal("Consolas", theme.RootElement.GetProperty("fonts").GetProperty("code").GetString());
      Assert.Equal(17, theme.RootElement.GetProperty("sizes").GetProperty("codeFontSize").GetDouble());
      Assert.Equal("#112233", theme.RootElement.GetProperty("colors").GetProperty("background").GetString());
      Assert.Equal("html { background: #223344; }", theme.RootElement.GetProperty("customCSS").GetString());
      Assert.NotNull(session.FindInApp(cf => cf.ByName("Custom theme acceptance")));
      _output.WriteLine(session.DescribeDocumentStatus());
   }



   /// <summary>Invalid colors keep the dialog open; cancellation cannot create a saved override.</summary>
   [Fact]
   public void ThemeEditor_InvalidColorAndCancel_DoesNotPersist()
   {
      using var session = new AppSession();
      session.InvokeMenuItem("View", "EditDocumentThemeMenu");
      session.EnterText("ThemeEditorColor:background", "invalid");
      session.ClickButton("Save theme");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Theme colors must use six-digit hexadecimal notation.")), "the color validation error");
      Assert.NotNull(session.FindInApp(cf => cf.ByAutomationId("ThemeEditorName")));
      session.ClickButton("Cancel");
      session.WaitForElementGone("ThemeEditorName");
      Assert.DoesNotContain("invalid", session.ReadSettings(), StringComparison.Ordinal);
   }



   /// <summary>Restoring built-ins removes an edited built-in palette after explicit confirmation.</summary>
   [Fact]
   public void ThemeEditor_BuiltInOverride_RestoresOriginalPalette()
   {
      using var session = new AppSession(document: "# Theme restore acceptance");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Theme restore acceptance")), "the document heading", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "EditDocumentThemeMenu");
      session.EnterText("ThemeEditorColor:background", "#112233");
      session.ClickButton("Save theme");
      session.WaitForElementGone("ThemeEditorName");
      WaitForDocumentColor(session, System.Drawing.Color.FromArgb(17, 34, 51));
      session.InvokeMenuItem("View", "RestoreDocumentThemesMenu");
      session.ClickButton("Restore themes");
      WaitForDocumentColor(session, System.Drawing.Color.FromArgb(30, 30, 46));
      using var settings = JsonDocument.Parse(session.ReadSettings());
      Assert.Equal(0, settings.RootElement.GetProperty("UserThemes").GetArrayLength());
   }


   /// <summary>The real reading surface switches between hidden, metadata-table and Markdown frontmatter modes.</summary>
   [Fact]
   public void Frontmatter_ChangeModes_UpdatesSurfaceAndSavesChoice()
   {
      using var session = new AppSession(document: "---\n**Visible metadata**\n---\n# Frontmatter body");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Frontmatter body")), "the document body", TimeSpan.FromSeconds(30));
      Assert.Null(session.FindInApp(cf => cf.ByName("Visible metadata")));
      session.InvokeMenuItem("View", "FrontmatterMenu");
      session.ClickElement("FrontmatterMetadata");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Visible metadata")), "the metadata table value");
      session.WaitFor(() => session.FindInApp(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Table)), "the frontmatter table");
      session.InvokeMenuItem("View", "FrontmatterMenu");
      session.ClickElement("FrontmatterMarkdown");
      try
      {
         session.WaitFor(
               () => session.ReadSettings().Contains("\"Frontmatter\": \"Markdown\"", StringComparison.Ordinal) ? session.MainWindow : null,
               "the Markdown frontmatter choice to be saved");
         AppSession.WaitUntilGone(() => session.FindInApp(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Table)), "the metadata table after switching to Markdown");
      }
      catch(TimeoutException exception)
      {
         throw new TimeoutException($"{exception.Message} Input: {session.LastInvocation} {session.LastMouseInput} Settings: {session.ReadSettings()} Logs: {session.ReadLogs()} Native tree: {session.DescribeDocumentStatus()}", exception);
      }
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Visible metadata")), "the rendered frontmatter heading");
      session.InvokeMenuItem("View", "FrontmatterMenu");
      session.ClickElement("FrontmatterHide");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Reloaded document.md")), "the hidden-mode reload status");
      AppSession.WaitUntilGone(() => session.FindInApp(cf => cf.ByName("Visible metadata")), "the hidden metadata");
      Assert.NotNull(session.FindInApp(cf => cf.ByName("Frontmatter body")));
   }


   /// <summary>Source remains literal and read-only, follows committed reloads and persists its visibility.</summary>
   [Fact]
   public void Source_ShowReloadAndHide_PreservesLiteralSnapshot()
   {
      const string Markdown = "---\nsecret: literal metadata\n---\n# Source acceptance\n\n<script>document.body.textContent='Executed source';</script>\n\n```html\n<button onclick='evil()'>Literal code</button>\n```";
      using var session = new AppSession(document: Markdown);
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Source acceptance")), "the document heading", TimeSpan.FromSeconds(30));
      Assert.Null(session.FindInApp(cf => cf.ByAutomationId("SourceText")));
      session.InvokeMenuItem("View", "SourceToggle");
      AutomationElement source;
      try
      {
         source = session.WaitForElement("SourceText");
      }
      catch(TimeoutException exception)
      {
         throw new TimeoutException($"{exception.Message} Settings: {session.ReadSettings()} Logs: {session.ReadLogs()} Native tree: {session.DescribeDocumentStatus()}", exception);
      }
      Assert.True(source.Patterns.Value.Pattern.IsReadOnly.Value);
      string value = source.Patterns.Value.Pattern.Value.Value.ReplaceLineEndings("\n");
      Assert.Equal(Markdown, value);
      using(var settings = JsonDocument.Parse(session.ReadSettings()))
      {
         Assert.True(settings.RootElement.GetProperty("ShowSource").GetBoolean());
         Assert.Equal(320, settings.RootElement.GetProperty("SourceWidth").GetDouble());
      }
      File.WriteAllText(Path.Combine(session.DataDirectory, "document.md"), "# Source replacement\n\n**New literal text**");
      session.WaitFor(() => source.Patterns.Value.Pattern.Value.Value.Contains("**New literal text**", StringComparison.Ordinal) ? source : null, "the source snapshot after reload");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Source replacement")), "the rendered replacement heading");
      AutomationElement divider = session.WaitForElement("SourceDivider");
      session.MainWindow.SetForeground();
      Assert.True(divider.TryGetClickablePoint(out System.Drawing.Point start));
      Assert.Equal("SourceDivider", session.Automation.FromPoint(start).Properties.AutomationId.ValueOrDefault);
      NativeMouse.MoveTo(start);
      FlaUI.Core.Input.Wait.UntilInputIsProcessed();
      FlaUI.Core.Input.Mouse.Down(FlaUI.Core.Input.MouseButton.Left);
      FlaUI.Core.Input.Wait.UntilInputIsProcessed();
      NativeMouse.MoveTo(new System.Drawing.Point(start.X - 70, start.Y));
      FlaUI.Core.Input.Wait.UntilInputIsProcessed();
      FlaUI.Core.Input.Mouse.Up(FlaUI.Core.Input.MouseButton.Left);
      FlaUI.Core.Input.Wait.UntilInputIsProcessed();
      session.WaitFor(
         () =>
         {
            using var settings = JsonDocument.Parse(session.ReadSettings());
            return settings.RootElement.GetProperty("SourceWidth").GetDouble() > 360 ? session.MainWindow : null;
         },
         "the physically resized source width to be saved");
      session.InvokeMenuItem("View", "SourceToggle");
      session.WaitForElementGone("SourceText");
      using var hidden = JsonDocument.Parse(session.ReadSettings());
      Assert.False(hidden.RootElement.GetProperty("ShowSource").GetBoolean());
      _output.WriteLine(session.DescribeDocumentStatus());
   }


   /// <summary>Actual reader jumps, native source text-range scrolling and physical source wheel input synchronize both viewports.</summary>
   [Fact]
   public void Source_BidirectionalScroll_AlignsActualHeadingViewports()
   {
      string body = string.Join("\n\n", Enumerable.Range(0, 80).Select(index => "Source sync paragraph " + index));
      using var session = new AppSession(document: "# Source sync root\n\n" + body + "\n\n## Source sync middle\n\n" + body + "\n\n## Source sync last\n\n" + body);
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Source sync root")), "the source sync document", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "SourceToggle");
      AutomationElement source = session.WaitForElement("SourceText");
      session.InvokeMenuItem("View", "OutlineToggle");
      session.ClickElement("OutlineHeading:2");
      WaitForSourceHeadingAlignment(session, source, "## Source sync last");
      FlaUI.Core.ITextRange middle = source.Patterns.Text.Pattern.DocumentRange.FindText("## Source sync middle", backward: false, ignoreCase: false);
      Assert.NotNull(middle);
      middle.ScrollIntoView(alignToTop: true);
      Wait.UntilInputIsProcessed();
      WaitForBrowserHeadingAlignment(session, "mv-heading-1");
      session.ClickElement("OutlineHeading:2");
      WaitForSourceHeadingAlignment(session, source, "## Source sync last");
      AutomationElement browserScroller = FindDocumentScroller(session);
      double previous = browserScroller.Patterns.Scroll.Pattern.VerticalScrollPercent.Value;
      session.MouseClickElement("SourceText");
      Mouse.Scroll(-3);
      Wait.UntilInputIsProcessed();
      WaitForReaderScroll(session, browserScroller, value => value > previous + 0.2, "physical source-wheel scrolling mirrored in the reader");
      Assert.Contains("## Source sync middle", source.Patterns.Value.Pattern.Value.Value, StringComparison.Ordinal);
   }



   /// <summary>Actual nested collapse mirrors source, search reveals original content and reload/expansion retains independent choices.</summary>
   [Fact]
   public void Source_CollapseFindReload_PreservesCanonicalSnapshot()
   {
      const string Markdown = "# Source fold root\n\nRoot body\n\n## Source fold child\n\nChild body\n\n### Source fold grandchild\n\nGrand body\n\n```md\n# Fence pseudo heading\n```\n\nSource fold peer\n----------------\nPeer body\n\n# Source fold last\nTail";
      using var session = new AppSession(document: Markdown);
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Source fold root")), "the source folding document", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "SourceToggle");
      AutomationElement source = session.WaitForElement("SourceText");
      session.InvokeMenuItem("View", "OutlineToggle");
      session.WaitForElement("OutlineDepth").AsComboBox().Select(2);
      session.ClickElement("OutlineHeading:2");
      session.ClickElement("ToggleSection");
      WaitForSourceValue(session, source, value => !value.Contains("Grand body", StringComparison.Ordinal) && !value.Contains("# Fence pseudo", StringComparison.Ordinal) && value.Contains("### Source fold grandchild", StringComparison.Ordinal), "the folded native grandchild body and fence");
      session.ClickElement("OutlineHeading:1");
      session.ClickElement("ToggleSection");
      WaitForSourceValue(session, source, value => !value.Contains("### Source fold grandchild", StringComparison.Ordinal) && value.Contains("Source fold peer\r\n----------------", StringComparison.Ordinal), "the folded parent and retained Setext peer");
      session.InvokeMenuItem("Edit", "FindMenuItem");
      session.EnterText("FindText", "Grand body");
      WaitForFindCount(session, "1 of 1");
      Assert.Equal("Grand body", ReadSourceSelection(source));
      WaitForSourceValue(session, source, value => value.Contains("# Fence pseudo heading", StringComparison.Ordinal), "literal fenced source revealed for find");
      session.ClickElement("FindCloseButton");
      WaitForSourceValue(session, source, value => !value.Contains("Grand body", StringComparison.Ordinal) && !value.Contains("### Source fold grandchild", StringComparison.Ordinal), "pre-search source folds restored");
      session.ClickElement("OutlineHeading:1");
      session.ClickElement("ToggleSection");
      WaitForSourceValue(session, source, value => value.Contains("Child body", StringComparison.Ordinal) && value.Contains("### Source fold grandchild", StringComparison.Ordinal) && !value.Contains("Grand body", StringComparison.Ordinal), "expanded parent with independently folded child");
      string replacement = Markdown.Replace("Grand body", "Grand body reloaded", StringComparison.Ordinal);
      File.WriteAllText(Path.Combine(session.DataDirectory, "document.md"), replacement);
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Reloaded document.md")), "the committed source folding reload");
      session.ClickElement("OutlineHeading:2");
      session.ClickElement("ToggleSection");
      WaitForSourceValue(session, source, value => value.Contains("Grand body reloaded", StringComparison.Ordinal), "the reloaded canonical body after expansion");
      Assert.Equal(replacement, source.Patterns.Value.Pattern.Value.Value.ReplaceLineEndings("\n"));
   }



   /// <summary>Checks actual source-provider glyph rectangles against the owned viewport, retaining exact geometry on failure.</summary>
   private void WaitForSourceHeadingAlignment(AppSession session, AutomationElement source, string text)
   {
      try
      {
         session.WaitFor(() =>
         {
            FlaUI.Core.ITextRange range = source.Patterns.Text.Pattern.DocumentRange.FindText(text, backward: false, ignoreCase: false);
            System.Drawing.Rectangle[] rectangles = range.GetBoundingRectangles();
            return rectangles.Length > 0 && rectangles[0].Top - source.BoundingRectangle.Top is >= -2 and <= 36 ? source : null;
         }, "the actual native source heading aligned with its viewport");
      }
      catch(TimeoutException)
      {
         _output.WriteLine(session.ReadLogs());
         _output.WriteLine(session.DescribeDocumentStatus());
         throw;
      }
   }



   /// <summary>Checks the generated browser heading's actual viewport location, never a mirrored application property.</summary>
   private void WaitForBrowserHeadingAlignment(AppSession session, string identity)
   {
      try
      {
         session.WaitFor(() =>
         {
            AutomationElement? heading = session.FindInApp(cf => cf.ByAutomationId(identity));
            double top = session.WaitForElement("DocumentView").BoundingRectangle.Top;
            return heading is not null && heading.BoundingRectangle.Top - top is >= -2 and <= 36 ? heading : null;
         }, "the actual reader heading aligned after native source scrolling");
      }
      catch(TimeoutException)
      {
         _output.WriteLine(session.ReadLogs());
         _output.WriteLine(session.DescribeDocumentStatus());
         throw;
      }
   }



   /// <summary>Reads the native source value until the expected real presentation arrives, retaining owned fixture evidence.</summary>
   private void WaitForSourceValue(AppSession session, AutomationElement source, Func<string, bool> matches, string description)
   {
      try
      {
         session.WaitFor(() => matches(source.Patterns.Value.Pattern.Value.Value) ? source : null, description);
      }
      catch(TimeoutException)
      {
         _output.WriteLine(session.ReadLogs());
         _output.WriteLine(session.DescribeDocumentStatus());
         throw;
      }
   }



   /// <summary>Ctrl+F accepts physical typing and pairs the actual source selection with wrapping browser occurrences.</summary>
   [Fact]
   public void Find_PairedMatches_UpdatesCounterAndSourceSelection()
   {
      using var session = new AppSession(document: "# Find acceptance\n\n**Alpha** first alpha\n\nLast ALPHA");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Find acceptance")), "the document heading", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "SourceToggle");
      AutomationElement source = session.WaitForElement("SourceText");
      session.MainWindow.SetForeground();
      session.WaitForElement("DocumentView").Focus();
      FlaUI.Core.Input.Keyboard.TypeSimultaneously(FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL, FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_F);
      try
      {
         session.WaitForElement("FindText");
      }
      catch(TimeoutException)
      {
         AutomationElement focused = session.Automation.FocusedElement();
         _output.WriteLine($"Find shortcut focus: Id={focused.Properties.AutomationId.ValueOrDefault}; Type={focused.Properties.ControlType.ValueOrDefault}; PID={focused.Properties.ProcessId.ValueOrDefault}.");
         _output.WriteLine(session.ReadLogs());
         _output.WriteLine(session.DescribeDocumentStatus());
         throw;
      }
      FlaUI.Core.Input.Keyboard.Type("alpha");
      WaitForFindCount(session, "1 of 3");
      Assert.Equal("Alpha", ReadSourceSelection(source));
      session.ClickElement("FindNextButton");
      WaitForFindCount(session, "2 of 3");
      Assert.Equal("alpha", ReadSourceSelection(source));
      session.ClickElement("FindPreviousButton");
      WaitForFindCount(session, "1 of 3");
      session.ClickElement("FindPreviousButton");
      WaitForFindCount(session, "3 of 3");
      Assert.Equal("ALPHA", ReadSourceSelection(source));
      session.ClickElement("FindNextButton");
      WaitForFindCount(session, "1 of 3");
      _output.WriteLine(session.DescribeDocumentStatus());
      session.ClickElement("FindCloseButton");
      session.WaitForElementGone("FindText");
      Assert.Equal("", ReadSourceSelection(source));
      _output.WriteLine(session.DescribeDocumentStatus());
   }



   /// <summary>Find reveals collapsed text, moves the real browser viewport and restores prior section choices on close.</summary>
   [Fact]
   public void Find_CollapsedLongDocument_MovesViewportAndRestoresSections()
   {
      string paragraphs = string.Join("\n\n", Enumerable.Range(0, 100).Select(index => "Find filler paragraph " + index));
      using var session = new AppSession(document: "# Find root\n\nFirst needle occurrence\n\n" + paragraphs + "\n\n## Find tail\n\nLast needle occurrence");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Find root")), "the root heading", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "OutlineToggle");
      session.ClickElement("OutlineHeading:1");
      session.ClickElement("ToggleSection");
      AppSession.WaitUntilGone(() => session.FindInApp(cf => cf.ByName("Last needle occurrence")), "the collapsed tail content");
      session.ClickElement("OutlineHeading:0");
      session.InvokeMenuItem("Edit", "FindMenuItem");
      session.EnterText("FindText", "needle");
      WaitForFindCount(session, "1 of 2");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Last needle occurrence")), "the revealed tail match");
      session.ClickElement("FindNextButton");
      WaitForFindCount(session, "2 of 2");
      AutomationElement scroller = FindDocumentScroller(session);
      session.WaitFor(() => scroller.Patterns.Scroll.Pattern.VerticalScrollPercent.Value > 70 ? scroller : null, "the browser viewport at the last find match");
      session.ClickElement("FindNextButton");
      WaitForFindCount(session, "1 of 2");
      session.WaitFor(() => scroller.Patterns.Scroll.Pattern.VerticalScrollPercent.Value < 10 ? scroller : null, "the browser viewport after wrapping to the first match");
      session.ClickElement("FindCloseButton");
      AppSession.WaitUntilGone(() => session.FindInApp(cf => cf.ByName("Last needle occurrence")), "the restored collapsed tail content");
      Assert.NotNull(session.FindInApp(cf => cf.ByName("Find root")));
      session.InvokeMenuItem("Edit", "FindMenuItem");
      WaitForFindCount(session, "1 of 2");
      session.ClickElement("ExpandSections");
      session.ClickElement("FindCloseButton");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Last needle occurrence")), "the explicit expansion retained after closing find");
   }



   /// <summary>Hostile-looking terms stay literal and the current query is recomputed for the newly committed reload snapshot.</summary>
   [Fact]
   public void Find_LiteralQueryAndReload_KeepsDocumentSafeAndRefreshesCount()
   {
      const string Query = "');document.body.textContent='unsafe';//";
      using var session = new AppSession(document: "# Literal find\n\n`a.*[x]`\n\n```text\n" + Query + "\n```");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Literal find")), "the document heading", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "SourceToggle");
      AutomationElement source = session.WaitForElement("SourceText");
      session.InvokeMenuItem("Edit", "FindMenuItem");
      session.EnterText("FindText", "a.*[x]");
      WaitForFindCount(session, "1 of 1");
      Assert.Equal("a.*[x]", ReadSourceSelection(source));
      session.EnterText("FindText", Query);
      WaitForFindCount(session, "1 of 1");
      session.WaitFor(() => string.Equals(ReadSourceSelection(source), Query, StringComparison.Ordinal) ? source : null, "the literal hostile-looking query selection");
      Assert.NotNull(session.FindInApp(cf => cf.ByName("Literal find")));
      File.WriteAllText(Path.Combine(session.DataDirectory, "document.md"), "# Reloaded find\n\n```text\n" + Query + "\n" + Query + "\n```");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Reloaded find")), "the reloaded document heading");
      WaitForFindCount(session, "1 of 2");
      Assert.Equal(Query, ReadSourceSelection(source));
      session.EnterText("FindText", "absent literal query");
      WaitForFindCount(session, "0 of 0");
      Assert.False(session.WaitForElement("FindNextButton").IsEnabled);
      Assert.False(session.WaitForElement("FindPreviousButton").IsEnabled);
      Assert.Equal("", ReadSourceSelection(source));
      Assert.NotNull(session.FindInApp(cf => cf.ByName("Reloaded find")));
   }



   /// <summary>Waits for real native results, retaining fixture-only surface/log evidence on timeout.</summary>
   private static void WaitForFindCount(AppSession session, string count)
   {
      try
      {
         session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("FindCountText").And(cf.ByName(count))), "the paired find count " + count);
      }
      catch(TimeoutException exception)
      {
         throw new TimeoutException($"{exception.Message} Logs: {session.ReadLogs()} Native tree: {session.DescribeDocumentStatus()}", exception);
      }
   }



   /// <summary>Reads the native text provider's actual selected range, rather than a mirrored application property.</summary>
   private static string ReadSourceSelection(AutomationElement source)
      => string.Concat(source.Patterns.Text.Pattern.GetSelection().Select(range => range.GetText(-1)));


   /// <summary>The native depth choice filters exact headings and a real divider drag saves its new width.</summary>
   [Fact]
   public void Outline_DepthAndResize_PersistsNativeChoices()
   {
      using var session = new AppSession(document: "# Root outline\n\n## Child outline\n\n#### Deep outline");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Root outline")), "the document heading", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "OutlineToggle");
      session.WaitForElement("OutlineHeading:1");
      Assert.Null(session.FindInApp(cf => cf.ByAutomationId("OutlineHeading:2")));
      session.WaitForElement("OutlineDepth").AsComboBox().Select(0);
      session.WaitForElementGone("OutlineHeading:1");
      session.WaitForElement("OutlineDepth").AsComboBox().Select(3);
      session.WaitForElement("OutlineHeading:2");
      session.WaitForElement("OutlineDepth").Patterns.ExpandCollapse.Pattern.Collapse();
      AutomationElement divider = session.WaitForElement("OutlineDivider");
      session.MainWindow.SetForeground();
      Assert.True(divider.TryGetClickablePoint(out System.Drawing.Point start));
      Assert.Equal(session.App.ProcessId, session.Automation.FromPoint(start).Properties.ProcessId.ValueOrDefault);
      Assert.Equal("OutlineDivider", session.Automation.FromPoint(start).Properties.AutomationId.ValueOrDefault);
      NativeMouse.MoveTo(start);
      FlaUI.Core.Input.Wait.UntilInputIsProcessed();
      FlaUI.Core.Input.Mouse.Down(FlaUI.Core.Input.MouseButton.Left);
      FlaUI.Core.Input.Wait.UntilInputIsProcessed();
      NativeMouse.MoveTo(new System.Drawing.Point(start.X + 70, start.Y));
      FlaUI.Core.Input.Wait.UntilInputIsProcessed();
      FlaUI.Core.Input.Mouse.Up(FlaUI.Core.Input.MouseButton.Left);
      FlaUI.Core.Input.Wait.UntilInputIsProcessed();
      try
      {
         session.WaitFor(
            () =>
            {
               using var settings = JsonDocument.Parse(session.ReadSettings());
               return settings.RootElement.GetProperty("OutlineWidth").GetDouble() > 240 ? session.MainWindow : null;
            },
            "the resized outline width to be saved");
      }
      catch(TimeoutException exception)
      {
         throw new TimeoutException($"{exception.Message} Divider: {divider.BoundingRectangle}; pointer: {FlaUI.Core.Input.Mouse.Position}; logs: {session.ReadLogs()}; settings: {session.ReadSettings()}; tree: {session.DescribeDocumentStatus()}", exception);
      }
      using var saved = JsonDocument.Parse(session.ReadSettings());
      Assert.True(saved.RootElement.GetProperty("ShowOutline").GetBoolean());
      Assert.Equal(4, saved.RootElement.GetProperty("OutlineDepth").GetInt32());
      session.InvokeMenuItem("View", "OutlineToggle");
      session.WaitForElementGone("OutlineHeading:0");
      _output.WriteLine(session.DescribeDocumentStatus());
   }



   /// <summary>Physical vim keys move the actual browser by lines, half-pages and document ends.</summary>
   [Fact]
   public void ReadingKeys_ScrollCommands_MoveActualBrowserViewport()
   {
      string body = string.Join("\n\n", Enumerable.Range(0, 180).Select(index => "Vim scrolling paragraph " + index));
      using var session = new AppSession(document: "# Vim scroll fixture\n\n" + body);
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Vim scroll fixture")), "the vim heading", TimeSpan.FromSeconds(30));
      AutomationElement scroller = FindDocumentScroller(session);
      FocusBrowser(session);
      SendReadingKey(VirtualKeyShort.KEY_J);
      WaitForReaderScroll(session, scroller, value => value > 0.001, "j scrolling one line");
      SendReadingKey(VirtualKeyShort.KEY_K);
      WaitForReaderScroll(session, scroller, value => value < 0.01, "k returning one line");
      SendReadingKey(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_D);
      WaitForReaderScroll(session, scroller, value => value > 0.1, "Ctrl+D scrolling half a viewport");
      SendReadingKey(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_U);
      WaitForReaderScroll(session, scroller, value => value < 0.01, "Ctrl+U returning half a viewport");
      SendReadingKey(VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_G);
      WaitForReaderScroll(session, scroller, value => value > 99, "G reaching the document end");
      SendReadingKey(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_B);
      WaitForReaderScroll(session, scroller, value => value < 99, "Ctrl+B returning a viewport");
      Keyboard.Type(VirtualKeyShort.KEY_G, VirtualKeyShort.KEY_G);
      Wait.UntilInputIsProcessed();
      WaitForReaderScroll(session, scroller, value => value < 0.01, "gg returning to the document start");
      Assert.NotNull(session.FindInApp(cf => cf.ByName("Vim scroll fixture")));
   }



   /// <summary>Slash opens native find; n/N wrap matches while real typing remains in the query control.</summary>
   [Fact]
   public void ReadingKeys_FindAndTextFocus_PreserveLiteralInput()
   {
      string body = string.Join("\n\n", Enumerable.Range(0, 100).Select(index => "Vim find filler " + index));
      using var session = new AppSession(document: "# Vim find fixture\n\nFirst needle\n\n" + body + "\n\nLast needle");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Vim find fixture")), "the vim find heading", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "SourceToggle");
      AutomationElement scroller = FindDocumentScroller(session);
      FocusBrowser(session);
      SendReadingKey(VirtualKeyShort.OEM_2);
      session.WaitForElement("FindText");
      session.EnterText("FindText", "needle");
      WaitForFindCount(session, "1 of 2");
      FocusBrowser(session);
      SendReadingKey(VirtualKeyShort.KEY_N);
      WaitForFindCount(session, "2 of 2");
      WaitForReaderScroll(session, scroller, value => value > 80, "n moving to the final native match");
      SendReadingKey(VirtualKeyShort.SHIFT, VirtualKeyShort.KEY_N);
      WaitForFindCount(session, "1 of 2");
      WaitForReaderScroll(session, scroller, value => value < 10, "N moving to the first native match");
      SendReadingKey(VirtualKeyShort.OEM_2);
      AutomationElement query = session.WaitForElement("FindText");
      query.Focus();
      SendReadingKey(VirtualKeyShort.KEY_J);
      SendReadingKey(VirtualKeyShort.KEY_K);
      SendReadingKey(VirtualKeyShort.KEY_G);
      Assert.Equal("jkg", query.AsTextBox().Text);
      WaitForFindCount(session, "0 of 0");
      Assert.True(scroller.Patterns.Scroll.Pattern.VerticalScrollPercent.Value < 10);
      FocusBrowser(session);
      SendReadingKey(VirtualKeyShort.ESCAPE);
      session.WaitForElementGone("FindText");
      Assert.NotNull(session.FindInApp(cf => cf.ByName("Vim find fixture")));
   }



   /// <summary>Heading sequences move between actual visible headings and skip a collapsed nested descendant.</summary>
   [Fact]
   public void ReadingKeys_HeadingSequences_SkipCollapsedDescendant()
   {
      string body = string.Join("\n\n", Enumerable.Range(0, 60).Select(index => "Vim heading filler " + index));
      using var session = new AppSession(document: "# Vim root\n\n" + body + "\n\n## Vim middle\n\nMiddle body\n\n### Hidden vim child\n\n" + body + "\n\n## Vim end\n\n" + body);
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Vim root")), "the vim root heading", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "OutlineToggle");
      session.ClickElement("OutlineHeading:1");
      session.ClickElement("ToggleSection");
      AppSession.WaitUntilGone(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-2")), "the collapsed nested vim heading");
      AutomationElement scroller = FindDocumentScroller(session);
      FocusBrowser(session);
      Keyboard.Type(VirtualKeyShort.KEY_G, VirtualKeyShort.KEY_G);
      Wait.UntilInputIsProcessed();
      WaitForReaderScroll(session, scroller, value => value < 0.01, "gg starting visible-heading navigation");
      Keyboard.Type(VirtualKeyShort.OEM_6, VirtualKeyShort.OEM_6);
      Wait.UntilInputIsProcessed();
      WaitForReaderScroll(session, scroller, value => value > 0.001, "the first visible heading");
      Keyboard.Type(VirtualKeyShort.OEM_6, VirtualKeyShort.OEM_6);
      Wait.UntilInputIsProcessed();
      WaitForReaderScroll(session, scroller, value => value > 20, "the middle visible heading");
      double middle = scroller.Patterns.Scroll.Pattern.VerticalScrollPercent.Value;
      Keyboard.Type(VirtualKeyShort.OEM_6, VirtualKeyShort.OEM_6);
      Wait.UntilInputIsProcessed();
      WaitForReaderScroll(session, scroller, value => value > middle + 0.01, "the final heading after skipping its hidden predecessor");
      Assert.Null(session.FindInApp(cf => cf.ByAutomationId("mv-heading-2")));
      AutomationElement finalHeading = session.WaitForElement("mv-heading-3");
      double viewportTop = session.WaitForElement("DocumentView").BoundingRectangle.Top;
      Assert.InRange(finalHeading.BoundingRectangle.Top - viewportTop, -2, 10);
      Keyboard.Type(VirtualKeyShort.OEM_4, VirtualKeyShort.OEM_4);
      Wait.UntilInputIsProcessed();
      WaitForReaderScroll(session, scroller, value => Math.Abs(value - middle) < 0.1, "the previous visible middle heading");
   }



   /// <summary>A real click establishes WinUI reading focus; UIA SetFocus alone left the logical focus empty or on an outline button.</summary>
   private static void FocusBrowser(AppSession session)
   {
      session.MainWindow.SetForeground();
      AutomationElement browser = session.WaitForElement("DocumentView");
      System.Drawing.Rectangle bounds = browser.BoundingRectangle;
      var point = new System.Drawing.Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
      Assert.True(bounds.Contains(point) && session.MainWindow.BoundingRectangle.Contains(point));
      NativeMouse.MoveTo(point);
      Wait.UntilInputIsProcessed();
      AutomationElement hit = session.Automation.FromPoint(point);
      Assert.True(hit.Equals(browser) || browser.FindAllDescendants().Any(element => element.Equals(hit)), "Reading focus must begin with a physical click inside the owned browser provider.");
      Mouse.Down(MouseButton.Left);
      Wait.UntilInputIsProcessed();
      Mouse.Up(MouseButton.Left);
      Wait.UntilInputIsProcessed();
   }



   /// <summary>Uses actual Windows keydown/up messages, including simultaneous modifiers.</summary>
   private static void SendReadingKey(params VirtualKeyShort[] keys)
   {
      Keyboard.TypeSimultaneously(keys);
      Wait.UntilInputIsProcessed();
   }



   /// <summary>Retains the owned fixture's focus/provider/viewport evidence when a physical reading key fails.</summary>
   private void WaitForReaderScroll(AppSession session, AutomationElement scroller, Func<double, bool> matches, string description)
   {
      try
      {
         session.WaitFor(() => matches(scroller.Patterns.Scroll.Pattern.VerticalScrollPercent.Value) ? scroller : null, description);
      }
      catch(TimeoutException)
      {
         AutomationElement focused = session.Automation.FocusedElement();
         _output.WriteLine($"Reading focus Id={focused.Properties.AutomationId.ValueOrDefault}; Type={focused.Properties.ControlType.ValueOrDefault}; PID={focused.Properties.ProcessId.ValueOrDefault}; viewport={scroller.Patterns.Scroll.Pattern.VerticalScrollPercent.Value}.");
         _output.WriteLine(session.ReadLogs());
         _output.WriteLine(session.DescribeDocumentStatus());
         throw;
      }
   }


   /// <summary>A physical local-fragment click reveals a hidden generated heading without loading another page.</summary>
   [Fact]
   public void Outline_LocalFragmentClick_RevealsCollapsedTarget()
   {
      using var session = new AppSession(document: "[Jump to target](#target-heading)\n\n# Fragment root\n\nRoot body\n\n## Target heading\n\nTarget body");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Fragment root")), "the root heading", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "OutlineToggle");
      session.ClickElement("OutlineHeading:0");
      session.ClickElement("ToggleSection");
      AppSession.WaitUntilGone(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-1").And(cf.ByName("Target heading"))), "the heading inside its collapsed ancestor");
      AutomationElement link = session.WaitFor(() => session.MainWindow.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Hyperlink).And(cf.ByName("Jump to target"))), "the local fragment link");
      ClickDocumentLink(session, link);
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-1").And(cf.ByName("Target heading"))), "the fragment's revealed target heading");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Target body")), "the revealed target body");
      Assert.NotNull(session.FindInApp(cf => cf.ByName("Fragment root")));
      session.ClickElement("ToggleSection");
      AppSession.WaitUntilGone(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-1")), "the target collapsed a second time");
      ClickDocumentLink(session, link);
      session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("mv-heading-1").And(cf.ByName("Target heading"))), "the same fragment's revealed target after recollapse");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Target body")), "the target body revealed by the repeated link click");
   }


   /// <summary>Clicks the exact link bounds through its owned composed provider, retaining physical input checks.</summary>
   private void ClickDocumentLink(AppSession session, AutomationElement link)
   {
      session.MainWindow.SetForeground();
      Assert.True(link.TryGetClickablePoint(out System.Drawing.Point point));
      Assert.True(session.MainWindow.BoundingRectangle.Contains(point));
      NativeMouse.MoveTo(point);
      FlaUI.Core.Input.Wait.UntilInputIsProcessed();
      AutomationElement hit = session.Automation.FromPoint(point);
      bool exactLink = hit.Equals(link) || hit.Parent.Equals(link);
      bool browserHost = hit.Equals(session.WaitForElement("DocumentView"));
      bool withinLink = link.BoundingRectangle.Contains(FlaUI.Core.Input.Mouse.Position);
      if(!exactLink && !browserHost || !withinLink)
      {
         _output.WriteLine($"Fragment point={point}; mouse={FlaUI.Core.Input.Mouse.Position}; link={link.Properties.BoundingRectangle.ValueOrDefault}; hit Id={hit.Properties.AutomationId.ValueOrDefault}; Type={hit.Properties.ControlType.ValueOrDefault}; PID={hit.Properties.ProcessId.ValueOrDefault}; bounds={hit.Properties.BoundingRectangle.ValueOrDefault}; parent Type={hit.Parent.Properties.ControlType.ValueOrDefault}; bounds={hit.Parent.Properties.BoundingRectangle.ValueOrDefault}.");
         _output.WriteLine(session.DescribeDocumentStatus());
      }
      Assert.True(withinLink, "The physical pointer must stay within the exact document link bounds.");
      Assert.True(exactLink || browserHost, "The pointer must belong to the owned browser provider or the exact link/text child.");
      FlaUI.Core.Input.Mouse.Down(FlaUI.Core.Input.MouseButton.Left);
      FlaUI.Core.Input.Wait.UntilInputIsProcessed();
      FlaUI.Core.Input.Mouse.Up(FlaUI.Core.Input.MouseButton.Left);
      FlaUI.Core.Input.Wait.UntilInputIsProcessed();
   }



   /// <summary>A heading selection moves the actual browser viewport and all-section operations change displayed content.</summary>
   [Fact]
   public void Outline_HeadingJumpAndCollapse_UpdatesActualDocument()
   {
      string paragraphs = string.Join("\n\n", Enumerable.Range(0, 100).Select(index => "Long outline paragraph " + index));
      using var session = new AppSession(document: "# Start outline\n\n" + paragraphs + "\n\n## Target outline\n\nTarget section content\n\n## Peer outline\n\nPeer section content");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Start outline")), "the initial heading", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "OutlineToggle");
      session.ClickElement("OutlineHeading:1");
      session.WaitFor(
         () =>
         {
            AutomationElement scroller = FindDocumentScroller(session);
            return scroller.Patterns.Scroll.Pattern.VerticalScrollPercent.Value > 70 ? scroller : null;
         },
         "the actual browser to jump to the target heading");
      session.ClickElement("ToggleSection");
      AppSession.WaitUntilGone(() => session.FindInApp(cf => cf.ByName("Target section content")), "the collapsed target body");
      Assert.NotNull(session.FindInApp(cf => cf.ByName("Peer section content")));
      session.ClickElement("ToggleSection");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Target section content")), "the expanded target body");
      session.ClickElement("CollapseSections");
      AppSession.WaitUntilGone(() => session.FindInApp(cf => cf.ByName("Peer section content")), "all collapsed section content");
      session.ClickElement("ExpandSections");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Target section content")), "all expanded sections");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Peer section content")), "the expanded peer section");
      _output.WriteLine(session.DescribeDocumentStatus());
   }



   /// <summary>Expanding a parent retains a collapsed child, and theme/reload retain the same hierarchy's collapse state.</summary>
   [Fact]
   public void Outline_NestedCollapse_RetainsStateAcrossThemeAndReload()
   {
      using var session = new AppSession(document: "# Parent outline\n\nParent body\n\n## Child outline\n\nHidden child body\n\n## Peer outline\n\nVisible peer body");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Parent body")), "the original body", TimeSpan.FromSeconds(30));
      session.InvokeMenuItem("View", "OutlineToggle");
      session.ClickElement("OutlineHeading:1");
      session.ClickElement("ToggleSection");
      AppSession.WaitUntilGone(() => session.FindInApp(cf => cf.ByName("Hidden child body")), "the collapsed child");
      session.ClickElement("OutlineHeading:0");
      session.ClickElement("ToggleSection");
      AppSession.WaitUntilGone(() => session.FindInApp(cf => cf.ByName("Visible peer body")), "the collapsed parent");
      session.ClickElement("ToggleSection");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Visible peer body")), "the expanded parent");
      Assert.Null(session.FindInApp(cf => cf.ByName("Hidden child body")));
      session.InvokeMenuItem("View", "DocumentThemesMenu");
      session.ClickElement("DocumentTheme:GitHub Light");
      WaitForDocumentColor(session, System.Drawing.Color.White);
      Assert.Null(session.FindInApp(cf => cf.ByName("Hidden child body")));
      File.WriteAllText(Path.Combine(session.DataDirectory, "document.md"), "# Parent outline\n\nParent body\n\n## Child outline\n\nUpdated child body\n\n## Peer outline\n\nUpdated peer body");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Updated peer body")), "the new snapshot");
      Assert.Null(session.FindInApp(cf => cf.ByName("Updated child body")));
      session.ClickElement("OutlineHeading:1");
      session.ClickElement("ToggleSection");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Updated child body")), "the expanded reloaded child");
   }


   /// <summary>Updates: a build that was not installed by Velopack says updates need an install.</summary>
   [Fact]
   public void CheckForUpdates_InALooseBuild_SaysUpdatesNeedAnInstall()
   {
      using var session = new AppSession();
      session.InvokeMenuItem("Help", "CheckForUpdatesMenuItem");
      session.WaitFor(
         () => session.FindInApp(cf => cf.ByName("Updates are only available in installed builds.")),
         "the not-installed message");
      session.ClickButton("OK");
   }



   /// <summary>The launched document appears in the actual browser accessibility tree.</summary>
   [Fact]
   public void OpenDocument_GfmDocument_ShowsRenderedContent()
   {
      using var session = new AppSession(document: "# Reader acceptance\n\n| Column | Value |\n| --- | --- |\n| One | Two |\n\n- [x] Finished\n\n<script>document.body.textContent='UNSAFE EXECUTION'</script>");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Reader acceptance")), "the rendered document heading", TimeSpan.FromSeconds(30));
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Two")), "the rendered table cell");
      Assert.Null(session.FindInApp(cf => cf.ByName("UNSAFE EXECUTION")));
      Assert.Contains("Opened a Markdown document.", session.ReadLogs(), StringComparison.Ordinal);
   }



   /// <summary>Reload uses the actual source file again instead of preserving an obsolete rendered snapshot.</summary>
   [Fact]
   public void Reload_ChangedDocument_ShowsNewContent()
   {
      using var session = new AppSession(document: "# Before reload");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Before reload")), "the initial heading", TimeSpan.FromSeconds(30));
      File.WriteAllText(Path.Combine(session.DataDirectory, "document.md"), "# After reload");
      session.InvokeMenuItem("File", "ReloadMenuItem");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("After reload")), "the reloaded heading");
      Assert.Null(session.FindInApp(cf => cf.ByName("Before reload")));
   }

   /// <summary>Saving through a replacement file updates the real browser without invoking Reload.</summary>
   [Fact]
   public void LiveReload_AtomicReplacement_ShowsNewContent()
   {
      using var session = new AppSession(document: "# Before automatic reload");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Before automatic reload")), "the initial heading", TimeSpan.FromSeconds(30));
      string replacement = Path.Combine(session.DataDirectory, "replacement.tmp");
      File.WriteAllText(replacement, "# After automatic reload");
      File.Move(replacement, Path.Combine(session.DataDirectory, "document.md"), overwrite: true);
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("After automatic reload")), "the automatically reloaded heading");
      Assert.Contains("Reloaded a Markdown document.", session.ReadLogs(), StringComparison.Ordinal);
   }



   /// <summary>A missing file preserves the displayed snapshot and recovers when the path reappears.</summary>
   [Fact]
   public void LiveReload_DeletedThenRecreated_PreservesAndRecovers()
   {
      using var session = new AppSession(document: "# Last readable version");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Last readable version")), "the initial heading", TimeSpan.FromSeconds(30));
      try
      {
         session.WaitFor(() => session.FindInApp(cf => cf.ByAutomationId("StatusText").And(cf.ByName("Opened document.md"))), "the accessible initial document status");
      }
      catch(TimeoutException exception)
      {
         throw new TimeoutException($"{exception.Message} Native tree: {session.DescribeDocumentStatus()}", exception);
      }
      string path = Path.Combine(session.DataDirectory, "document.md");
      File.Delete(path);
      try
      {
         session.WaitFor(
            () => session.FindInApp(cf => cf.ByName("The document is missing. Showing the last version; it will reload when restored.")),
            "the missing-file status");
      }
      catch(TimeoutException exception)
      {
         AutomationElement? status = session.FindInApp(cf => cf.ByAutomationId("StatusText"));
         throw new TimeoutException($"{exception.Message} Observed status: '{status?.Properties.Name.ValueOrDefault}'. Logs: {session.ReadLogs()} Native tree: {session.DescribeDocumentStatus()}", exception);
      }
      Assert.NotNull(session.FindInApp(cf => cf.ByName("Last readable version")));
      File.WriteAllText(path, "# Recovered document");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Recovered document")), "the recreated document heading");
   }



   /// <summary>The application retries a brief exclusive editor lock instead of dropping its pending save.</summary>
   [Fact]
   public void LiveReload_EditorTemporarilyLocksFile_Retries()
   {
      using var session = new AppSession(document: "# Before editor lock");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Before editor lock")), "the initial heading", TimeSpan.FromSeconds(30));
      using(var file = new FileStream(Path.Combine(session.DataDirectory, "document.md"), FileMode.Create, FileAccess.Write, FileShare.None))
      {
         using var writer = new StreamWriter(file);
         writer.Write("# After editor lock");
         writer.Flush();
         Thread.Sleep(800);
      }
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("After editor lock")), "the unlocked save");
   }



   /// <summary>Reload retains relative position when a save changes the document's total height.</summary>
   [Fact]
   public void LiveReload_HeightChanges_RetainsRelativeScroll()
   {
      string paragraphs = string.Join("\n\n", Enumerable.Range(0, 200).Select(index => $"Paragraph {index}: a document long enough to scroll."));
      using var session = new AppSession(document: "# Scroll before\n\n" + paragraphs);
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Scroll before")), "the initial heading", TimeSpan.FromSeconds(30));
      AutomationElement scroller = FindDocumentScroller(session);
      scroller.Patterns.Scroll.Pattern.SetScrollPercent(-1, 80);
      session.WaitFor(
         () => scroller.Patterns.Scroll.Pattern.VerticalScrollPercent.Value > 75 ? scroller : null,
         "the document to scroll");
      File.WriteAllText(Path.Combine(session.DataDirectory, "document.md"), "# Scroll after\n\n" + paragraphs + "\n\n" + paragraphs);
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Scroll after")), "the reloaded heading");
      session.WaitFor(
         () =>
         {
            AutomationElement updated = FindDocumentScroller(session);
            return updated.Patterns.Scroll.Pattern.VerticalScrollPercent.Value > 75 ? updated : null;
         },
         "relative scroll restoration");
   }



   /// <summary>Remote image markup cannot connect to a listening server even when the parser retains its URL.</summary>
   [Fact]
   public void OpenDocument_RemoteImage_DoesNotConnect()
   {
      using var listener = new TcpListener(IPAddress.Loopback, 0);
      listener.Start();
      int port = ((IPEndPoint)listener.LocalEndpoint).Port;
      using var session = new AppSession(document: $"# Network boundary\n\n![remote image](http://127.0.0.1:{port}/remote.png)");
      session.WaitFor(() => session.FindInApp(cf => cf.ByName("Network boundary")), "the rendered document heading", TimeSpan.FromSeconds(30));
      Thread.Sleep(1500);
      Assert.False(listener.Pending(), "The document attempted to fetch its remote image.");
   }



   /// <summary>Waits for the actual painted document palette rather than just a saved preference or old heading.</summary>
   private static void WaitForDocumentColor(AppSession session, System.Drawing.Color expected)
   {
      session.WaitFor(
         () =>
         {
            using System.Drawing.Bitmap image = session.MainWindow.Capture();
            return image.GetPixel(image.Width / 2, image.Height / 2).ToArgb() == expected.ToArgb() ? session.MainWindow : null;
         },
         "the rendered document background " + expected);
   }


   /// <summary>Finds the browser's actual scroll provider, which is separate from the native window.</summary>
   private static AutomationElement FindDocumentScroller(AppSession session)
   {
      AutomationElement browser = session.WaitForElement("DocumentView");
      return session.WaitFor(
         () => browser.FindAllDescendants().Prepend(browser).FirstOrDefault(element =>
            element.Patterns.Scroll.TryGetPattern(out IScrollPattern? scroll) && scroll.VerticallyScrollable.Value),
         "the owned browser scroll provider");
   }


}



/// <summary>UI tests share one desktop, so the collection never runs in parallel.</summary>
[CollectionDefinition("Desktop", DisableParallelization = true)]
public class DesktopCollectionDefinition
{
}
