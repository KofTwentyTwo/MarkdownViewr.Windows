/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using MarkdownViewr.Windows.Core.Updates;
using Velopack;
using Velopack.Sources;


namespace MarkdownViewr.Windows;

/// <summary>
/// Velopack's update manager over the releases of the app's Gitea repository. A pass-through:
/// every member needs an installed copy or the network, and the decisions around it live in
/// <see cref="ReleaseUpdateService"/>.
/// </summary>
internal sealed class GiteaReleaseUpdater : IReleaseUpdater
{
   private readonly UpdateManager _manager;
   private UpdateInfo? _pending;



   /// <summary>
   /// Reads the repository's releases without credentials. Development builds are published as
   /// prereleases, so only a development build looks at those.
   /// </summary>
   public GiteaReleaseUpdater(Uri repository, bool includePrereleases)
   {
      _manager = new UpdateManager(new GiteaSource(repository.AbsoluteUri, accessToken: null, prerelease: includePrereleases));
   }



   /// <inheritdoc/>
   public bool IsInstalled => _manager.IsInstalled;



   /// <inheritdoc/>
   public string? CurrentVersion => _manager.CurrentVersion?.ToString();



   /// <inheritdoc/>
   public async Task<string?> CheckAsync()
   {
      _pending = await _manager.CheckForUpdatesAsync().ConfigureAwait(false);
      return _pending?.TargetFullRelease.Version.ToString();
   }



   /// <inheritdoc/>
   public Task DownloadAsync(Action<int>? progress, CancellationToken cancellation)
      => _pending is null
         ? Task.FromException(new InvalidOperationException("No update was found to download."))
         : _manager.DownloadUpdatesAsync(_pending, progress, cancellation);



   /// <inheritdoc/>
   public void ApplyAndRestart()
   {
      if(_pending is null)
      {
         throw new InvalidOperationException("No update was found to apply.");
      }
      _manager.ApplyUpdatesAndRestart(_pending.TargetFullRelease);
   }
}
