# ADR-0007: Releases and updates from Gitea

- **Status:** Accepted
- **Date:** 2026-11-10

## Context

The project moved from GitHub to the Gitea server at `git.kof22.com`, and GitHub is no
longer used. The app's links, its update check and its download instructions all
pointed at the GitHub repository.

The update check came from AppKit's `VelopackUpdateService`, which can only read
GitHub Releases. Where an installed copy looks for updates is hard to change later:
a copy can only learn about a new location from an update it finds at the old one.

## Decision

**One repository address.** `AppIdentity.Info.RepositoryUrl` is the Gitea repository.
Every page the app opens is derived from it: the project page, new issue, issue list,
latest release, installer, user guide and license.

**Updates read the Gitea releases.** `ReleaseUpdateService` in the core library
implements AppKit's `IUpdateService`, so AppKit's coordinator and dialogs are used
unchanged. It holds the decisions (is this copy installed, was an update found, turn
every failure into a message) and is fully tested against a scripted updater. The
app project supplies `GiteaReleaseUpdater`, a pass-through to Velopack's update
manager over its Gitea release source. Releases are read without credentials; the
repository is public.

**Channels are unchanged.** A stable build ignores prereleases; a development build
(a prerelease version) reads them. Velopack then matches packages to the installed
channel by its own channel name. Package ids, channel names and asset names stay as
they were, so the existing releases, which are also present on Gitea, remain a valid
update history.

**Asset names are part of the contract.** The download links in the README and in the
invitation email use Gitea's `releases/download/latest/<asset>` form with the names
Velopack gives the stable edition, for example
`markdownviewr-windows-stable-Setup.exe`. A release pipeline must keep those names.

## Consequences

- Copies installed from release 1.0.3 or earlier look at GitHub for updates. They will
  not find releases published only on Gitea and must be reinstalled once from the new
  location, unless a release that contains this change is also published on GitHub.
- The Gitea host is now the trust anchor for updates. Signing and package integrity
  checks are unchanged.
- Gitea has no discussions area. **Help → Community support** opens the issue list.
- The update decisions exist twice, here and in AppKit. They should move back once
  AppKit accepts an update source other than GitHub.
- The installed-update acceptance harness (`build/Test-InstalledUpdates.ps1` with
  `tests/MarkdownViewr.Windows.UpdateProbe`) still exercises AppKit's GitHub service
  against a local fixture. It no longer covers the path the app uses and needs porting
  to the Gitea source.

## Options considered

- **Keep the update check on GitHub and move only the links.** Rejected: no further
  release will be published there, so installed copies would never update.
- **Change AppKit first.** The better home for the code, but it needs an AppKit
  release before the app can move, and the app's links cannot move without the
  update source moving too: both come from the one repository address.
