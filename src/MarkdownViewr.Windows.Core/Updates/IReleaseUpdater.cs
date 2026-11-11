/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */


namespace MarkdownViewr.Windows.Core.Updates;

/// <summary>
/// The part of the update library the app uses: find a newer release of this edition, download
/// it, and restart into it. Implementations may throw; <see cref="ReleaseUpdateService"/> turns
/// every failure into a message.
/// </summary>
public interface IReleaseUpdater
{
   /// <summary>True when this copy was installed by the update library and can replace itself.</summary>
   bool IsInstalled { get; }



   /// <summary>The installed version, or null for a copy that was not installed.</summary>
   string? CurrentVersion { get; }



   /// <summary>Looks for a newer release and remembers it for the download. Returns its version, or null when there is none.</summary>
   Task<string?> CheckAsync();



   /// <summary>Downloads the release found by the last check, reporting progress from 0 to 100.</summary>
   Task DownloadAsync(Action<int>? progress, CancellationToken cancellation);



   /// <summary>Installs the downloaded release and restarts the app. The process exits on success.</summary>
   void ApplyAndRestart();
}
