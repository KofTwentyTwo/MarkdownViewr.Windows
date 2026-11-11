/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Updates;


namespace MarkdownViewr.Windows.Core.Updates;

/// <summary>
/// Checks for and installs updates published as releases of the app's repository. Every failure
/// comes back as a value, never an exception: nothing as optional as an update may take the
/// app down.
/// </summary>
public sealed class ReleaseUpdateService : IUpdateService
{
   private readonly Func<IReleaseUpdater> _create;
   private IReleaseUpdater? _updater;
   private bool _updateReady;



   /// <summary>The updater is created on first use, so constructing the service cannot fail.</summary>
   public ReleaseUpdateService(Func<IReleaseUpdater> create)
   {
      ArgumentNullException.ThrowIfNull(create);
      _create = create;
   }



   /// <inheritdoc/>
   public bool IsSupported
   {
      get
      {
         try
         {
            return Updater.IsInstalled;
         }
         catch(Exception)
         {
            return false;
         }
      }
   }



   /// <inheritdoc/>
   public string? CurrentVersion
   {
      get
      {
         try
         {
            return Updater.CurrentVersion;
         }
         catch(Exception)
         {
            return null;
         }
      }
   }



   /// <summary>The one updater this service uses; it remembers the release a check found.</summary>
   private IReleaseUpdater Updater => _updater ??= _create();



   /// <inheritdoc/>
   public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
   {
      _updateReady = false;
      try
      {
         if(!Updater.IsInstalled)
         {
            return UpdateCheckResult.Failed(UpdateText.NotSupported);
         }
         string? version = await Updater.CheckAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
         _updateReady = version is not null;
         return version is null ? UpdateCheckResult.UpToDate : UpdateCheckResult.Available(version);
      }
      catch(Exception exception)
      {
         return UpdateCheckResult.Failed(exception.Message);
      }
   }



   /// <inheritdoc/>
   public async Task<string?> DownloadAndApplyAsync(IProgress<int>? progress = null, CancellationToken cancellationToken = default)
   {
      if(!_updateReady)
      {
         return "No update is ready to install; check for updates first.";
      }
      try
      {
         await Updater.DownloadAsync(progress is null ? null : progress.Report, cancellationToken).ConfigureAwait(false);
         Updater.ApplyAndRestart();
         return null;
      }
      catch(Exception exception)
      {
         return exception.Message;
      }
   }
}
