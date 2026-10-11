# Session State

Updated: 2026-10-10

## Active work

MarkdownViewr.Windows is the full Windows counterpart to Darin's markdownViewr,
using published AppKit 0.1.0. Approved identity: MarkdownViewr / MarkdownViewr.Windows /
markdownviewr-windows. Topic feat/windows-viewer, draft PR #3; do not shrink PRODUCT.md scope.

Published head 77dd04da96b75938a1c1d38c8bebe4851c11a011 completed CI 38102249889 SUCCESS:
build/test, format, 26 native/UI tests, published 26/26 and installed stable/dev 25/25
each, with no skips. Four byte gates verified 536 files in both Velopack payloads;
AppKit update probes applied/restarted within both channels. These are synthetic test
packages, not signed production releases. CodeQL 38102249846/security 38102249838 passed.
Scripts 38102249407 failed inside unchanged PSScriptAnalyzer 1.25.0; issue #5 remains open.
No rules suppressed or failed runs restarted. Local baseline all 13 scripts passed;
this demonstrates intermittent behavior, not an engine fix.

## Reader and theme evidence

Reader provides picker/Ctrl+O/launch paths, sanitized GFM, bounded passive local images,
debounced saves/replacement/deletion recovery and guarded relative scroll. Directory
callbacks dispatch only; bounded editor-lock retries and load scheduling preserve old
snapshots and picker requests. Native status declared before WebView2 resolved the
embedded-provider traversal issue. Initial window also fits DPI-scaled monitor work
area. Exact initial/missing/recovery assertions were preserved throughout diagnosis.
Hosted actual replacement/lock/deletion/relative-scroll/remote-image tests now pass.

All 20 original JSON themes are embedded unchanged with Darin's MIT notice shipped.
Built-in selection persists independently of the AppKit shell; actual dark/light paint
and screenshots passed. Current local change adds saved user overrides/custom names,
native color/code-font/size/CSS controls, invalid-input validation and built-in restoration
that retains custom names. AppKit saves preferences atomically; corrupt saved themes are
discarded before catalog loading. Core 149 tests/100% coverage and zero-warning build
pass. New dialog/browser tests added but not run locally; next hosted acceptance pending.

Editable theme commit 30ca0943c09a8662794c2d400f1b5c49de426942 is now pushed.
Its CI 38102867556 fully passed, including new theme editor/color/CSS/invalid-input/
restore tests and both installed channels. CodeQL 38102867583 and scripts 38102867251
also passed. New follow-up tests the NumberBox range-value edit to font size 17 and
adds analyzer filename/full-exception reporting without changing rules or retry policy.

Frontmatter #8 now implements the reference's hidden default, escaped line-based
metadata and sanitized Markdown display. Complete leading delimiters required;
Windows CRLF and body preserved. Native mode changes persist and retain relative
viewport; menu disabled while snapshot loading prevents stale mode changes. 149 core
tests pass with 100% line coverage; native switching/table disappearance test added,
not run locally. Darin's MIT notice and source attribution retained. Next push waits
for these final local gates; observe its exact CI handles.

## Authority and continuity

Owner authorized signed/DCO commits, pushes, repository configuration and ticket updates.
No merges, tags/releases or new Azure attempts. Existing AppKit/standards/starter drafts
remain unmerged; current-SHA human acknowledgement and producer-bound checks enforced.
Actual Azure signing/final signatures remain unverified. Desktop reservation is unanswered;
do not run local GUI input without availability. Hosted fixtures use an isolated runner.

Pinned reference: 6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021 at
AppKit/artifacts/markdownviewr-reference. Preserve original MIT notices when adapting.
Evidence: artifacts/verified-reader-38102249889 contains installed TRX/JSON and screenshots;
artifacts/user-themes-final-core contains the current 100% coverage report.

## Next

Commit/push the verified editable-theme milestone; observe its exact hosted handles.
Update #4/#6 with their completed scoped acceptance and keep #1 full port/#7 theme work
and #5 analyzer investigation accurate. Continue TOC/collapse/source/find/vim navigation,
frontmatter, editor launch, printing, multiple windows/recents and Windows associations.
Finish standards inventory and exact final signed delivery before claiming completion.
