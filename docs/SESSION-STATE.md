# Session State

Updated: 2026-10-11

## Active work and authority

Build the full Windows counterpart to Darin's markdownViewr with published
AppKit 0.1.0. Identity: MarkdownViewr / MarkdownViewr.Windows /
markdownviewr-windows. PRODUCT.md remains the full scope. Topic feat/windows-viewer,
draft PR #3. Owner authorizes signed/DCO commits, pushes, configuration and tickets.
No merges, tags/releases, new Azure sign-ins or fabricated human review.
Hosted Windows UI tests are authorized; no current local desktop reservation.

## Printing native baseline verified; installed baseline live

Native File/Print/Ctrl+P, copied themes/layout preferences and explicit native PDF
saving are implemented. Core363/100% measured lines;55 scripts; build/format pass.
Signedec5f98f / filtered38125751638 passes all3 original cases, zero failures/skips:
keyboard draft cancel, actual Windows print UI cancel and real default/themed PDFs.
Actual pixels prove text, expanded folded image, white ink-saving page, hidden
images and active-theme background. Exact actual glyph/view rectangles prove
unchanged nonzero reader position/zoom, source text and collapsed content after
both exports. Evidence: artifacts/print-exact-filename-38125751638.

Hosted probes proved41pixel PDF reading movement. SavePdf restores validated
vertical position in finally only for the same ready navigation; no completion is
guessed for void native print UI. Filename typing waits for exact full native
readback. Stale UIA200% values are replaced by actual glyph/view assertions.
Cleaned300e145 / CI38126078284 native52/52 passes, zero failures/skips; format,
core, scripts/security/PR gates pass. Installed job114430656496 remains live.
Native log: artifacts/print-clean-native-38126078284.log. Preserve this installed
producer before pushing further changes; require published52/stable-dev51.

Two additional native-output cases now compile/format locally: first printed
heading glyph ratios for standard/matching zoom, real40px padding shift and paired
find/source selection/screen paint after each PDF; saved user-theme240px block
width versus full page and actual optional border rows. New helper edits actual
native options, including padding NumberBox; all generated PDF/PNG bytes retained.
These cases have not run. Full expanded suites expect54 native/published and53 per
installed channel. No new dependency or local GUI input. ADR-0003 remains proposed.

Live settings validate. Official conformance34PASS/1FAIL/1WARN/3REVIEW: required
standards inventory lacks app, confirmed live. Exact reviewed addition prepared,
not applied/merged; see docs/standards-readiness.md. Actual signing remains deferred.

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

1. Require full cleaned printing gates; retain actual TRX/PDF/PNG and update evidence.
2. Complete source raw-heading audit and broader zoom/theme/find/collapse proof.
3. Complete print #12 output/state acceptance, windows/recents/activation and remaining theme/render/link parity.
4. Finish standards inventory; exact signed stable/dev artifacts remain deferred.

Update TODO and tickets with measured results; keep the PR draft until reviewed.
Current exact commit/job IDs are in ignored artifacts/CURRENT-CHECKPOINT.json.
Historical details: [reader/source notes](history/reader-source-session-2026-10-11.md).
