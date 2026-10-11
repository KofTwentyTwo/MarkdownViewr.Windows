# Session State

Updated: 2026-10-11

## Active work and authority

Build the full Windows counterpart to Darin's markdownViewr with published
AppKit 0.1.0. Identity: MarkdownViewr / MarkdownViewr.Windows /
markdownviewr-windows. PRODUCT.md remains the full scope. Topic feat/windows-viewer,
draft PR #3. Owner authorizes signed/DCO commits, pushes, configuration and tickets.
No merges, tags/releases, new Azure sign-ins or fabricated human review.
Hosted Windows UI tests are authorized; no current local desktop reservation.

## Current outcome

Signed 690be07 fixes repeated outline activation. Native ItemClick supplies the
TextBlock content, not its ListViewItem container. Both now carry the bounded
numeric heading tag; activation accepts tagged FrameworkElement content while
SelectionChanged retains keyboard navigation. No title lookup or source mapping change.

The original source regression passed on 690be07: source-diagnostics38121389088,
job114416668837, including source focus preservation, actual source/reader middle
glyphs, repeated last-heading activation and the final physical-wheel assertion.
TRX retained under artifacts/source-clicked-content-verified-38121389088.
Temporary workflow, tagged probes and source geometry logs are removed.
Full cleaned-head native/published/stable/dev acceptance remains required.

Font zoom and named editors are implemented. Prior016c745 native/published48/49,
zero skips; only repeat navigation failed. Zoom actual glyph/menu/toolbar/Ctrl/
reset/reload and named-editor cases pass. Core330 tests/100% measured lines,
script boundary55 tests; zero-warning build and formatting verified.

## Verified baseline and evidence

Previous fully accepted b0f3b86/CI38113042618: published44/44, installed stable/dev
43/43, zero failures/skips. Actual AppKit apply/restart uses synthetic unsigned
fixtures; it does not prove production signing. Published predecessor failure
TRX: artifacts/source-physical-reactivation-failed-installed-38120651378.
Red clicked-content trace/TRX: artifacts/source-activation-boundary-38121148429.
Superseded diagnostic jobs are not fixed-head acceptance proof.

## Next steps

1. Observe all full gates on the cleaned commit; retain actual TRX/update evidence.
2. Complete source raw-heading audit and broader zoom/theme/find/collapse proof.
3. Implement print #12, windows/recents/activation and remaining theme/render/link parity.
4. Finish standards inventory; exact signed stable/dev artifacts remain deferred.

Update TODO and tickets with measured results; keep the PR draft until reviewed.
Current exact commit/job IDs are in ignored artifacts/CURRENT-CHECKPOINT.json.
Historical details: [reader/source notes](history/reader-source-session-2026-10-11.md).
