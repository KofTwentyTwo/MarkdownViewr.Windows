# Session State

Updated: 2026-10-10

## Live reload milestone

Active project: MarkdownViewr.Windows, feat/windows-viewer, draft PR #3.
Owner approved MarkdownViewr / MarkdownViewr.Windows / markdownviewr-windows.
The goal remains the full Windows counterpart to Darin's markdownViewr using AppKit.

First reader commit f1756d21ede53e95a7ff5ef8bd1490ef9495cb25 is published. Its hosted
CI 38098965780 fully passed: build/test, format, 13 actual native/UI tests and both
installed update channels. CodeQL 38098965738 and security 38098965817 also passed.
Scripts 38098965365 failed inside PSScriptAnalyzer 1.25.0 with a null-reference engine
exception, tracked in #5. No scripts/settings/workflow were changed; no retry or
suppression. The initial scripts run 38097167189 is terminal cancelled, not live.

Current next implementation adds a directory watcher with 350 ms debounce. It
survives atomic replacement, rename-away and deletion/recreation. Missing files keep
the last displayed snapshot; brief editor locks get bounded retries. Watchers dispose
on close and ignore stale selected-document callbacks. A core load scheduler coalesces
reloads while preserving picker selections and refreshes after a failed new open. Scroll capture/restore matches
Darin's relative scroll behavior when document height changes. Authored scripts and
native messaging stay disabled; fixed native queries check the current document URI,
and results are validated finite fractions before numeric-only restoration.

Local evidence: zero-warning solution build; 82 core tests, zero skipped, 100% core
line coverage; 12 native filesystem/watcher tests passed. Browser replacement,
deletion/recreation, editor lock, relative-scroll and remote-image denial tests have
been added but not run locally. Desktop reservation remains unanswered; do not drive
this desktop without availability. Hosted current-head acceptance will be the next
runtime proof. Issues #4 (reader boundary), #6 (reload) and #1 (full scope) remain open.

## Repository and authorization

Public repository: [MarkdownViewr.Windows](https://github.com/KofTwentyTwo/MarkdownViewr.Windows).
Setup Validate verified 16 producer-bound contexts, protected main and release tags,
selected Actions and the protected release environment. Signed/DCO topic commits,
current-SHA human acknowledgement and required checks remain enforced.

Owner authorized commit/push/configuration, not merges, tags or releases. Existing
AppKit/standards/starter drafts remain unmerged. Azure retries stay deferred; actual
CI signing and final signed release artifacts remain unverified. Published AppKit
0.1.0 is used; do not substitute unpublished 0.2.0 APIs.

## Next work

Verify reload/browser network acceptance and preserve its exact hosted handles.
Then port Darin's themes and implement TOC/collapse, source/find/vim navigation,
frontmatter, editor, print, multiple windows, recents and Windows associations.
Original reference is pinned at 6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021 under
AppKit/artifacts/markdownviewr-reference. No original code/resources copied yet;
retain Darin's MIT notices when adapting them. Full PRODUCT.md scope remains open.
