/*
 * Copyright (c) 2026 James Maes
 * SPDX-License-Identifier: MIT
 */

using KofTwentyTwo.AppKit.Updates;
using MarkdownViewr.Windows.Core.Updates;


namespace MarkdownViewr.Windows.Core.Tests;

/// <summary>Tests of the update service's decisions around a scripted updater: no installed copy and no network are needed.</summary>
public sealed class ReleaseUpdateServiceTests
{
   /// <summary>A service needs a way to make its updater.</summary>
   [Fact]
   public void Constructor_WithoutAFactory_IsRefused()
   {
      Assert.Throws<ArgumentNullException>(() => new ReleaseUpdateService(null!));
   }



   /// <summary>The updater is made on first use and reused, so building the window cannot fail and a check's result is still there for the download.</summary>
   [Fact]
   public async Task Updater_IsCreatedOnFirstUseAndOnlyOnce()
   {
      int created = 0;
      var updater = new ScriptedUpdater { Installed = true, Version = "1.0.3", Found = "1.0.4" };
      var service = new ReleaseUpdateService(() =>
      {
         created++;
         return updater;
      });
      Assert.Equal(0, created);
      Assert.True(service.IsSupported);
      Assert.Equal("1.0.3", service.CurrentVersion);
      Assert.True((await service.CheckAsync()).IsUpdateAvailable);
      Assert.Null(await service.DownloadAndApplyAsync());
      Assert.Equal(1, created);
   }



   /// <summary>A copy that was not installed reports that, with no version, and a check explains why instead of asking the network.</summary>
   [Fact]
   public async Task CheckAsync_CopyNotInstalled_ExplainsWithoutChecking()
   {
      var updater = new ScriptedUpdater { Installed = false };
      var service = new ReleaseUpdateService(() => updater);
      Assert.False(service.IsSupported);
      Assert.Null(service.CurrentVersion);
      UpdateCheckResult result = await service.CheckAsync();
      Assert.Equal(UpdateText.NotSupported, result.Error);
      Assert.False(result.IsUpdateAvailable);
      Assert.Equal(0, updater.Checks);
   }



   /// <summary>An updater that cannot even be made leaves the app running: unsupported, no version, and a failed check carrying the reason.</summary>
   [Fact]
   public async Task Members_UpdaterCannotBeCreated_ReportFailureAsValues()
   {
      var service = new ReleaseUpdateService(() => throw new InvalidOperationException("no update manager"));
      Assert.False(service.IsSupported);
      Assert.Null(service.CurrentVersion);
      UpdateCheckResult result = await service.CheckAsync();
      Assert.Equal("no update manager", result.Error);
      Assert.Equal("No update is ready to install; check for updates first.", await service.DownloadAndApplyAsync());
   }



   /// <summary>No newer release is not an error and leaves nothing to install.</summary>
   [Fact]
   public async Task CheckAsync_NoNewerRelease_IsUpToDate()
   {
      var updater = new ScriptedUpdater { Installed = true, Version = "1.0.4", Found = null };
      var service = new ReleaseUpdateService(() => updater);
      UpdateCheckResult result = await service.CheckAsync();
      Assert.Same(UpdateCheckResult.UpToDate, result);
      Assert.Equal("No update is ready to install; check for updates first.", await service.DownloadAndApplyAsync());
      Assert.Equal(0, updater.Downloads);
   }



   /// <summary>A newer release is offered by version, then downloaded with progress and applied.</summary>
   [Fact]
   public async Task DownloadAndApplyAsync_AfterAFoundRelease_DownloadsReportsProgressAndApplies()
   {
      var updater = new ScriptedUpdater { Installed = true, Version = "1.0.3", Found = "1.0.4" };
      var service = new ReleaseUpdateService(() => updater);
      UpdateCheckResult result = await service.CheckAsync();
      Assert.Equal("1.0.4", result.AvailableVersion);
      var progress = new RecordingProgress();
      Assert.Null(await service.DownloadAndApplyAsync(progress));
      Assert.Equal([0, 100], progress.Values);
      Assert.Equal(1, updater.Downloads);
      Assert.Equal(1, updater.Applied);
   }



   /// <summary>A download without a progress listener passes none on.</summary>
   [Fact]
   public async Task DownloadAndApplyAsync_WithoutProgress_PassesNoCallback()
   {
      var updater = new ScriptedUpdater { Installed = true, Found = "1.0.4" };
      var service = new ReleaseUpdateService(() => updater);
      await service.CheckAsync();
      Assert.Null(await service.DownloadAndApplyAsync());
      Assert.False(updater.ProgressRequested);
   }



   /// <summary>A check that fails says why and leaves nothing to install, even after an earlier check found a release.</summary>
   [Fact]
   public async Task CheckAsync_Fails_ReportsTheReasonAndForgetsTheEarlierRelease()
   {
      var updater = new ScriptedUpdater { Installed = true, Found = "1.0.4" };
      var service = new ReleaseUpdateService(() => updater);
      Assert.True((await service.CheckAsync()).IsUpdateAvailable);
      updater.CheckFailure = new HttpRequestException("the server did not answer");
      UpdateCheckResult failed = await service.CheckAsync();
      Assert.Equal("the server did not answer", failed.Error);
      Assert.Equal("No update is ready to install; check for updates first.", await service.DownloadAndApplyAsync());
      Assert.Equal(0, updater.Downloads);
   }



   /// <summary>A check abandoned by the caller ends as a failed check, not an exception.</summary>
   [Fact]
   public async Task CheckAsync_Cancelled_ReturnsAFailureValue()
   {
      var updater = new ScriptedUpdater { Installed = true, Pending = new TaskCompletionSource<string?>() };
      var service = new ReleaseUpdateService(() => updater);
      using var cancellation = new CancellationTokenSource();
      Task<UpdateCheckResult> check = service.CheckAsync(cancellation.Token);
      await cancellation.CancelAsync();
      UpdateCheckResult result = await check;
      Assert.NotNull(result.Error);
      Assert.False(result.IsUpdateAvailable);
   }



   /// <summary>A failed download or a failed install comes back as the reason; the app keeps running on the installed version.</summary>
   [Theory]
   [InlineData(true)]
   [InlineData(false)]
   public async Task DownloadAndApplyAsync_DownloadOrInstallFails_ReturnsTheReason(bool failDownload)
   {
      var updater = new ScriptedUpdater { Installed = true, Found = "1.0.4" };
      if(failDownload)
      {
         updater.DownloadFailure = new IOException("the disk is full");
      }
      else
      {
         updater.ApplyFailure = new InvalidOperationException("the installer is busy");
      }
      var service = new ReleaseUpdateService(() => updater);
      await service.CheckAsync();
      string? error = await service.DownloadAndApplyAsync();
      Assert.Equal(failDownload ? "the disk is full" : "the installer is busy", error);
      Assert.Equal(failDownload ? 0 : 1, updater.Applied);
   }



   /// <summary>Collects progress reports synchronously, in order.</summary>
   private sealed class RecordingProgress : IProgress<int>
   {
      /// <summary>Every value reported so far.</summary>
      public List<int> Values { get; } = [];



      /// <summary>Records one report.</summary>
      public void Report(int value) => Values.Add(value);
   }



   /// <summary>An updater whose answers and failures are set by the test.</summary>
   private sealed class ScriptedUpdater : IReleaseUpdater
   {
      /// <summary>Whether this copy counts as installed.</summary>
      public bool Installed { get; init; }

      /// <summary>The installed version reported.</summary>
      public string? Version { get; init; }

      /// <summary>The newer version a check finds, or null for none.</summary>
      public string? Found { get; init; }

      /// <summary>A check that completes only when the test says so.</summary>
      public TaskCompletionSource<string?>? Pending { get; init; }

      /// <summary>Makes the next checks fail.</summary>
      public Exception? CheckFailure { get; set; }

      /// <summary>Makes the download fail.</summary>
      public Exception? DownloadFailure { get; set; }

      /// <summary>Makes the install fail.</summary>
      public Exception? ApplyFailure { get; set; }

      /// <summary>How many checks ran.</summary>
      public int Checks { get; private set; }

      /// <summary>How many downloads completed.</summary>
      public int Downloads { get; private set; }

      /// <summary>How many installs were started.</summary>
      public int Applied { get; private set; }

      /// <summary>Whether the last download was given a progress callback.</summary>
      public bool ProgressRequested { get; private set; }

      /// <inheritdoc/>
      public bool IsInstalled => Installed;

      /// <inheritdoc/>
      public string? CurrentVersion => Installed ? Version : null;



      /// <inheritdoc/>
      public Task<string?> CheckAsync()
      {
         Checks++;
         if(CheckFailure is not null)
         {
            return Task.FromException<string?>(CheckFailure);
         }
         return Pending?.Task ?? Task.FromResult(Found);
      }



      /// <inheritdoc/>
      public Task DownloadAsync(Action<int>? progress, CancellationToken cancellation)
      {
         ProgressRequested = progress is not null;
         if(DownloadFailure is not null)
         {
            return Task.FromException(DownloadFailure);
         }
         progress?.Invoke(0);
         progress?.Invoke(100);
         Downloads++;
         return Task.CompletedTask;
      }



      /// <inheritdoc/>
      public void ApplyAndRestart()
      {
         Applied++;
         if(ApplyFailure is not null)
         {
            throw ApplyFailure;
         }
      }
   }
}
