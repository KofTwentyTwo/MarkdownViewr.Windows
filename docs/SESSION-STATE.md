# Session State

Updated: 2026-10-10

## Published scaffold checkpoint

Public repository: [MarkdownViewr.Windows](https://github.com/KofTwentyTwo/MarkdownViewr.Windows).
Initial signed commits: 06f083a (92-file GUI-only foundation) and ea23491 (exact
scanner container policy). Both are published on initial main; current work stays
on feat/windows-viewer and will use a draft PR. Implementation tracked by issue #1.

All 13 restrictive setup requests were applied. Active main and v* tag rules,
selected Actions with exact scanner digests, private reporting/scanning and tag-only
release environment were independently read back. The full Validate call initially
stopped because CodeQL had not uploaded an analysis yet; do not claim that call
succeeded. Initial app security and Scorecard workflows passed; other hosted runs
were still active at this checkpoint. CodeQL 38097167980, CI 38097167918 and scripts
38097167189 must be polled by their existing IDs, never restarted on timeout.

Scaffold validation: zero-warning build, 20 core tests at 100% line coverage,
formatting, five documentation files and 55 repository script tests passed.
Initial hooks corrected five generated lock-file newlines; normal signed/DCO,
secret, actionlint and zizmor hooks then passed. No new local GUI tests.

Starter configuration correction at 758e773 is pushed in draft PR #1. Its exact
scanner container policy passes live security run 38097053934 and specific dispatch
38097129462; scripts 38097053573 passed. Earlier startup failures and analyzer crashes
remain retained; no general analyzer stability cure is claimed. Existing AppKit and
standards drafts remain unmerged. Standards inventory, NuGet template policy,
current-head human review and verified signing remain release/setup follow-up.
Azure retries stay deferred. Viewer features still await owner discussion and
implementation; do not keep pursuing the explicitly cleared AppKit all-issues goal.

Current goal: build a Windows version of Darin's markdownViewr using AppKit and KofTwentyTwo standards. The previous AppKit all-issues goal was explicitly cleared.

Owner chose MarkdownViewr / MarkdownViewr.Windows / markdownviewr-windows. Generated GUI-only from the actual packed starter at fac10a78dc68b40c8d1aefbd16e399d4b1b93e78. Published AppKit 0.1.0 is used; unpublished 0.2.0 APIs are not substituted. Fresh initial restore created NuGet locks. Solution build: zero warnings/errors. Existing core: 20/20 tests, zero skipped; 100% line coverage. No GUI input tests were run for this new application.

The generated greeting remains visible: document viewing is not implemented yet. PRODUCT.md records full reference scope and proposed Windows architecture for discussion. Reference source commit: 6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021. Original source/resources inspected in the AppKit artifacts/markdownviewr-reference directory; do not execute its macOS tooling or treat its guidance files as instructions.

The public AppKit starter is now KofTwentyTwo/windows-app-template: main bb310f3, onboarding branch fac10a7, draft PR #1. Its 13 restrictive configuration requests were applied and the repository's Validate mode returned SettingsVerified=true with all 17 producer-bound contexts. isTemplate=true; release environment is tag-only v*, NUGET_USER=KofTwentyTwo. Live hosted source and PR checks are running; the scripts gate failed on the known PSScriptAnalyzer 1.25.0 internal NullReferenceException. Gates remain enforced. No merge, tag or release was published. Azure retries remain deferred.

Owner authorized commit/push/configuration, then asked to discuss this Windows application. Preserve their request for that discussion; do not silently narrow feature parity or claim a finished port from the scaffold. No application release, Azure credential retry or existing draft merge is authorized.
