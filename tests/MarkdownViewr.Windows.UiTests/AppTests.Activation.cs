/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using System.Diagnostics;
using FlaUI.Core.AutomationElements;


namespace MarkdownViewr.Windows.UiTests;

/// <summary>Actual secondary application processes must forward literal files into the correct existing native reader/profile.</summary>
public partial class AppTests
{
   /// <summary>A secondary process with a different working directory opens its relative Unicode file in the primary and focuses duplicates.</summary>
   [Fact]
   public void Activation_SecondaryRelativeFile_PreservesExistingReaderAndFocusesDuplicate()
   {
      using var session = new AppSession(document: "# Activation original", documentFileName: "original.md");
      Window original = WaitForDocumentWindow(session, "original.md");
      WaitForWindowHeading(session, original, "Activation original");
      InvokeDocumentMenu(session, original, "SourceToggle", menuName: "View");
      Assert.Equal("# Activation original", WaitForWindowElement(session, original, "SourceText").Patterns.Value.Pattern.Value.Value);
      string senderDirectory = Path.Combine(session.DataDirectory, "sender space");
      Directory.CreateDirectory(senderDirectory);
      string filename = "new & café $HOME.md";
      File.WriteAllText(Path.Combine(senderDirectory, filename), "# Sender relative activation");
      ForwardFileActivation(session, senderDirectory, filename);
      Window opened = WaitForDocumentWindow(session, filename);
      WaitForWindowHeading(session, opened, "Sender relative activation");
      Assert.Equal(2, DocumentWindows(session).Length);
      Assert.Equal("# Activation original", WaitForWindowElement(session, original, "SourceText").Patterns.Value.Pattern.Value.Value);
      ForwardFileActivation(session, senderDirectory, Path.Combine(session.DataDirectory, "original.md"));
      Assert.Equal(2, DocumentWindows(session).Length);
      session.WaitFor(() => GetForegroundWindow() == original.Properties.NativeWindowHandle.ValueOrDefault ? original : null, "the existing document focused after duplicate activation");
      File.WriteAllText(Path.Combine(session.DataDirectory, "original.md"), "# Original watcher survives activation");
      WaitForWindowHeading(session, original, "Original watcher survives activation");
      session.WaitFor(() => original.FindFirstDescendant(cf => cf.ByAutomationId("SourceText")) is { } source && string.Equals(source.Patterns.Value.Pattern.Value.Value, "# Original watcher survives activation", StringComparison.Ordinal) ? source : null, "the original source/watch state after activation");
      RetainDocumentWindow(session, original, "secondary-activation-original");
      RetainDocumentWindow(session, opened, "secondary-activation-new-reader");
   }



   /// <summary>Launching the same binary into another data profile keeps its windows and document activations separate.</summary>
   [Fact]
   public void Activation_SeparateReviewProfiles_DoesNotRedirectToSiblingProfile()
   {
      using var first = new AppSession(document: "# First activation profile", documentFileName: "first.md");
      using var second = new AppSession(document: "# Second activation profile", documentFileName: "second.md");
      WaitForWindowHeading(first, first.MainWindow, "First activation profile");
      WaitForWindowHeading(second, second.MainWindow, "Second activation profile");
      Assert.NotEqual(first.App.ProcessId, second.App.ProcessId);
      File.WriteAllText(Path.Combine(first.DataDirectory, "third.md"), "# First profile redirected file");
      ForwardFileActivation(first, first.DataDirectory, "third.md");
      WaitForWindowHeading(first, WaitForDocumentWindow(first, "third.md"), "First profile redirected file");
      Assert.Equal(2, DocumentWindows(first).Length);
      Assert.Single(DocumentWindows(second));
      Assert.NotNull(second.MainWindow.FindFirstDescendant(cf => cf.ByName("Second activation profile")));
      RetainDocumentWindow(first, first.MainWindow, "activation-first-profile");
      RetainDocumentWindow(second, second.MainWindow, "activation-isolated-profile");
   }



   /// <summary>Starts the real reviewed executable with literal ArgumentList values and the owning session profile; only this child is cleaned up on failure.</summary>
   private static void ForwardFileActivation(AppSession session, string directory, params string[] paths)
   {
      var start = new ProcessStartInfo(session.ExePath) { WorkingDirectory = directory, UseShellExecute = false };
      start.Environment[AppIdentity.ForChannel("stable").DataDirectoryVariable] = session.DataDirectory;
      start.Environment[AppIdentity.ForChannel("dev").DataDirectoryVariable] = session.DataDirectory;
      foreach(string path in paths)
      {
         start.ArgumentList.Add(path);
      }
      using Process child = Process.Start(start) ?? throw new IOException("The secondary activation process did not start.");
      if(!child.WaitForExit(15000))
      {
         child.Kill(entireProcessTree: true);
         throw new TimeoutException("The secondary activation process did not exit after delivery.");
      }
      Assert.Equal(0, child.ExitCode);
   }
}
