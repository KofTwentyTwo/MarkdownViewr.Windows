/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core.Sharing;

/// <summary>
/// The invitation a person sends to someone who might like the app: an email draft that says
/// what the app is and where to download it, learn it and report on it. The text is fixed, so
/// nothing about the person's documents or settings leaves with it.
/// </summary>
public static class ShareInvitation
{
   /// <summary>
   /// Some mail programs are started with the whole link on a command line and cut it near 2,000
   /// characters. The draft stays under that, so it arrives complete.
   /// </summary>
   public const int MaximumLinkLength = 2000;



   /// <summary>The subject line of the draft.</summary>
   public static string Subject { get; } = "Try " + AppIdentity.Info.DisplayName + ", a Markdown reader for Windows";



   /// <summary>The message, with lines separated the way email requires.</summary>
   public static string Body { get; } = string.Join(
      "\r\n",
      "Hello,",
      "",
      "I use " + AppIdentity.Info.DisplayName + " to read Markdown files on Windows and thought you might like to try it. It shows README files, notes and project documentation in a clean reading view, follows the file as it changes, and prints or saves to PDF. It is free and open source.",
      "",
      "Download the installer:",
      AppIdentity.InstallerUrl.AbsoluteUri,
      "",
      "It installs for your account only, with no administrator rights needed, on 64-bit Windows 10 (version 1809 or later) or Windows 11. A portable ZIP and an MSI are on the release page:",
      AppIdentity.LatestReleaseUrl.AbsoluteUri,
      "",
      "User guide:",
      AppIdentity.UserGuideUrl.AbsoluteUri,
      "",
      "If something does not work, or you have an idea, please report it here:",
      AppIdentity.SupportUrl.AbsoluteUri,
      "",
      "Project page and source code:",
      AppIdentity.Info.RepositoryUrl!.AbsoluteUri);



   /// <summary>The whole invitation as plain text, for when no email program is available.</summary>
   public static string Text { get; } = Subject + "\r\n\r\n" + Body;



   /// <summary>
   /// A link that opens the draft in the person's email program with no recipient, so the only
   /// thing left to do is address it and send.
   /// </summary>
   public static Uri MailLink { get; } = new("mailto:?subject=" + Uri.EscapeDataString(Subject) + "&body=" + Uri.EscapeDataString(Body));
}
