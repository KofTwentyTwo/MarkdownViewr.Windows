# Session State

Updated: 2026-10-11

## Active work and authority

Build the full Windows counterpart to Darin's markdownViewr with published
AppKit 0.1.0. Identity: MarkdownViewr / MarkdownViewr.Windows /
markdownviewr-windows. PRODUCT.md remains the full scope. Topic feat/windows-viewer,
draft PR #3. Owner authorizes signed/DCO commits, pushes, configuration and tickets.
No merges, tags/releases, new Azure sign-ins or fabricated human review.
Hosted Windows UI tests are authorized; no current local desktop reservation.

## Print implementation in progress

Native File/Print and Ctrl+P open a copied draft: print theme, images/backgrounds,
font zoom, full/document width, borders and pixel padding. Screen/print styles are
independent; no screen/DOM/find/collapse completion callback is guessed from void
ShowPrintUI. Save PDF uses Windows' picker and awaited WebView2 output. ADR-0003
is proposed for review. Core363/100% measured lines and build/format pass.
First hosted38123994631 passed native Windows cancel; wrong fixture find IDs
and UIA-only filename entry failed. Correction41a687d / filtered38124425486
passes both cancel cases and generates a real PDF: owned red image, predominantly
white page and actual text pixels pass. Retained PDF/PNG/TRX under
artifacts/print-path-verified-state-failed-38124425486. The subsequent unchanged
reader viewport assertion fails (observed87.0253%). New focused fixture probes
record before/options/PDF positions and wait for stable pre-print layout; no
product restoration change or relaxed after-print assertion yet.
Full suites now expect
52 native/published and51 per installed channel. Temporary filtered print workflow
is a feedback loop; all required full gates remain.

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
Cleaned2639e2d fullCI38121762124 passed: native49/49,published49/49, installed
stable/dev48/48, zero failures/skips. All other producers pass. Evidence retained
under artifacts/source-clean-verified-installed-38121762124. New print acceptance remains pending.

Font zoom and named editors are implemented. Prior016c745 native/published48/49,
zero skips; only repeat navigation failed. Zoom actual glyph/menu/toolbar/Ctrl/
reset/reload and named-editor cases pass. Core363 tests/100% measured lines,
script boundary55 tests; zero-warning build and formatting verified.

## Verified baseline and evidence

Previous fully accepted b0f3b86/CI38113042618: published44/44, installed stable/dev
43/43, zero failures/skips. Actual AppKit apply/restart uses synthetic unsigned
fixtures; it does not prove production signing. Published predecessor failure
TRX: artifacts/source-physical-reactivation-failed-installed-38120651378.
Red clicked-content trace/TRX: artifacts/source-activation-boundary-38121148429.
Superseded diagnostic jobs are not fixed-head acceptance proof.

## Next steps

1. Resolve actual PDF destination/keyboard acceptance on the printing head; retain TRX/PDF/PNG evidence.
2. Complete source raw-heading audit and broader zoom/theme/find/collapse proof.
3. Complete print #12 output/state acceptance, windows/recents/activation and remaining theme/render/link parity.
4. Finish standards inventory; exact signed stable/dev artifacts remain deferred.

Update TODO and tickets with measured results; keep the PR draft until reviewed.
Current exact commit/job IDs are in ignored artifacts/CURRENT-CHECKPOINT.json.
Historical details: [reader/source notes](history/reader-source-session-2026-10-11.md).
