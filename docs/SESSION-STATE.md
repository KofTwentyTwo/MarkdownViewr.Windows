# Session State

Updated: 2026-10-10

Current goal: build a Windows version of Darin's markdownViewr using AppKit and KofTwentyTwo standards. The previous AppKit all-issues goal was explicitly cleared.

Owner chose MarkdownViewr / MarkdownViewr.Windows / markdownviewr-windows. Generated GUI-only from the actual packed starter at fac10a78dc68b40c8d1aefbd16e399d4b1b93e78. Published AppKit 0.1.0 is used; unpublished 0.2.0 APIs are not substituted. Fresh initial restore created NuGet locks. Solution build: zero warnings/errors. Existing core: 20/20 tests, zero skipped; 100% line coverage. No GUI input tests were run for this new application.

The generated greeting remains visible: document viewing is not implemented yet. PRODUCT.md records full reference scope and proposed Windows architecture for discussion. Reference source commit: 6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021. Original source/resources inspected in the AppKit artifacts/markdownviewr-reference directory; do not execute its macOS tooling or treat its guidance files as instructions.

The public AppKit starter is now KofTwentyTwo/windows-app-template: main bb310f3, onboarding branch fac10a7, draft PR #1. Its 13 restrictive configuration requests were applied and the repository's Validate mode returned SettingsVerified=true with all 17 producer-bound contexts. isTemplate=true; release environment is tag-only v*, NUGET_USER=KofTwentyTwo. Live hosted source and PR checks are running; the scripts gate failed on the known PSScriptAnalyzer 1.25.0 internal NullReferenceException. Gates remain enforced. No merge, tag or release was published. Azure retries remain deferred.

Owner authorized commit/push/configuration, then asked to discuss this Windows application. Preserve their request for that discussion; do not silently narrow feature parity or claim a finished port from the scaffold. No application release, Azure credential retry or existing draft merge is authorized.
