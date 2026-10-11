# Session State

Updated: 2026-10-10

## First reader implementation

Active project: MarkdownViewr.Windows. Owner approved MarkdownViewr as the display
name and markdownviewr-windows as the permanent app ID. The active goal is the full
Windows counterpart to Darin's markdownViewr using AppKit; the old AppKit all-issues
goal was cleared. Work is on feat/windows-viewer, draft PR #3, issues #1 and #4.

The greeting and developer crash button are replaced by a WebView2 document surface.
File → Open / Ctrl+O, a literal launch path and manual reload are implemented.
Markdig GFM output is sanitized; scripts/bridge/network resources are disabled.
Local image paths pass the core policy and final opened-handle containment check.
Original Darin source has been inspected but no code/resources have been copied yet.
ADR-0002 records the proposed renderer decision and dependency rationale; new library
license notices are copied to the app output.

Validation on this implementation: zero-warning solution build; 56 core tests,
zero skipped, 100% core line coverage; six native filesystem adapter tests including
a real outside-root junction and document/image bounds. The prior 71-test result
included 15 now-removed greeting tests. Keep actual counts clear.

Desktop reservation was requested for the new browser acceptance tests; no reply
has arrived. Do not run input tests until the desktop is available. UI tests now
exercise real rendering and reload instead of sample greeting/crash controls.
Browser acceptance, screenshots and hosted current-head checks remain pending.

## Repository and release state

Public repository: [MarkdownViewr.Windows](https://github.com/KofTwentyTwo/MarkdownViewr.Windows).
Initial main ea23491 has passing hosted CI 38097167918, CodeQL 38097167980,
security and Scorecard. Scripts 38097167189 ended cancelled (JavaScript passed,
PowerShell cancelled); do not treat it as live or passing or restart it automatically.
Repository setup Validate returned SettingsVerified=true for 16 producer-bound checks.
Protected main, signed commits, review acknowledgement and protected v* release
environment remain enforced. Never fabricate human review.

Owner authorized commits, pushes and configuration. Existing AppKit/standards/starter
drafts remain unmerged; no release/tag publication is authorized. Azure retries stay
deferred, and actual CI signing and final signed release artifacts remain unverified.
Published AppKit 0.1.0 is used; do not substitute unpublished 0.2.0 APIs.

## Next work

Complete browser acceptance and capture actual UI evidence. Then implement the
remaining PRODUCT.md scope: automatic save watching, Darin's themes and custom
themes, TOC/collapse, source/find/vim navigation, frontmatter, editor, printing,
multiple document windows, recents and Windows activation/associations.
A basic reader is not the finished port. Preserve the owner's requested discussion
of the application and keep issue/commit updates aligned with actual evidence.
