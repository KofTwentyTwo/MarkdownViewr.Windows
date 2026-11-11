/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Sharing;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Tests of the invitation email a person can start from the Help menu.</summary>
public sealed class ShareInvitationTests
{
   /// <summary>The draft has no recipient: the person only has to address it and send.</summary>
   [Fact]
   public void MailLink_IsAMailDraftWithoutARecipient()
   {
      Assert.Equal("mailto", ShareInvitation.MailLink.Scheme);
      Assert.StartsWith("mailto:?subject=", ShareInvitation.MailLink.AbsoluteUri, StringComparison.Ordinal);
      Assert.Equal(ShareInvitation.MailLink.OriginalString, ShareInvitation.MailLink.AbsoluteUri);
   }



   /// <summary>An email program that decodes the link gets exactly the subject and message, and nothing else.</summary>
   [Fact]
   public void MailLink_DecodesToTheSubjectAndBody()
   {
      string query = ShareInvitation.MailLink.AbsoluteUri["mailto:?".Length..];
      string[] fields = query.Split('&');
      Assert.Equal(2, fields.Length);
      Assert.StartsWith("subject=", fields[0], StringComparison.Ordinal);
      Assert.StartsWith("body=", fields[1], StringComparison.Ordinal);
      Assert.Equal(ShareInvitation.Subject, Uri.UnescapeDataString(fields[0]["subject=".Length..]));
      Assert.Equal(ShareInvitation.Body, Uri.UnescapeDataString(fields[1]["body=".Length..]));
   }



   /// <summary>Only characters every mail link parser reads the same way appear unescaped, so nothing in the text can end a field early or add one.</summary>
   [Fact]
   public void MailLink_EscapesEverythingButUnreservedCharacters()
   {
      string query = ShareInvitation.MailLink.AbsoluteUri["mailto:?".Length..];
      foreach(string field in query.Split('&'))
      {
         string value = field[(field.IndexOf('=', StringComparison.Ordinal) + 1)..];
         Assert.Matches("^(?:[A-Za-z0-9._~-]|%[0-9A-F]{2})+$", value);
      }
   }



   /// <summary>Mail programs started from a command line cut a long link; the whole draft fits under that length.</summary>
   [Fact]
   public void MailLink_FitsTheLengthMailProgramsAccept()
   {
      Assert.Equal(2000, ShareInvitation.MaximumLinkLength);
      Assert.InRange(ShareInvitation.MailLink.AbsoluteUri.Length, 1, ShareInvitation.MaximumLinkLength);
   }



   /// <summary>The message gives the installer, the release page, the user guide, where to report on the app and where it lives.</summary>
   [Fact]
   public void Body_GivesTheDownloadGuideReportAndProjectLinks()
   {
      string[] lines = ShareInvitation.Body.Split("\r\n");
      Assert.Contains("https://git.kof22.com/KofTwentyTwo/MarkdownViewr.Windows/releases/download/latest/markdownviewr-windows-stable-Setup.exe", lines, StringComparer.Ordinal);
      Assert.Contains("https://git.kof22.com/KofTwentyTwo/MarkdownViewr.Windows/releases/latest", lines, StringComparer.Ordinal);
      Assert.Contains("https://git.kof22.com/KofTwentyTwo/MarkdownViewr.Windows/src/branch/main/docs/user-guide.md", lines, StringComparer.Ordinal);
      Assert.Contains("https://git.kof22.com/KofTwentyTwo/MarkdownViewr.Windows/issues", lines, StringComparer.Ordinal);
      Assert.Contains("https://git.kof22.com/KofTwentyTwo/MarkdownViewr.Windows", lines, StringComparer.Ordinal);
      Assert.Equal(AppIdentity.Info.RepositoryUrl!.AbsoluteUri, lines[^1]);
      Assert.Contains("Windows 10 (version 1809 or later) or Windows 11", ShareInvitation.Body, StringComparison.Ordinal);
      Assert.Contains("MarkdownViewr", ShareInvitation.Subject, StringComparison.Ordinal);
   }



   /// <summary>Email requires CRLF line ends; a bare line feed or carriage return shows as one long line in some programs.</summary>
   [Fact]
   public void Body_UsesOnlyEmailLineEnds()
   {
      string withoutLineEnds = ShareInvitation.Body.Replace("\r\n", "", StringComparison.Ordinal);
      Assert.DoesNotContain('\r', withoutLineEnds);
      Assert.DoesNotContain('\n', withoutLineEnds);
      Assert.False(ShareInvitation.Body.EndsWith("\r\n", StringComparison.Ordinal));
   }



   /// <summary>The clipboard fallback carries the same invitation: the subject, a blank line, then the message.</summary>
   [Fact]
   public void Text_IsTheSubjectThenTheBody()
   {
      Assert.Equal(ShareInvitation.Subject + "\r\n\r\n" + ShareInvitation.Body, ShareInvitation.Text);
   }
}
