# Session State

Updated: 2026-10-11

## Active work and authority

Build the full Windows counterpart to Darin's markdownViewr with published
AppKit 0.1.0. Identity: MarkdownViewr / MarkdownViewr.Windows /
markdownviewr-windows. PRODUCT.md remains the full scope. Topic feat/windows-viewer,
draft PR #3. Owner authorizes signed/DCO commits, pushes, configuration and tickets.
No merges, tags/releases, new Azure sign-ins or fabricated human review.
Hosted Windows UI tests are authorized; no current local desktop reservation.

## Printing baseline verified; expanded geometry investigation

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
Cleaned300e145 / CI38126078284 fully passes: native/published52 each,
installed stable/dev51 each, zero failures/skips. Both AppKit apply/restart probes
reach expected versions without check/apply errors; fixtures are unsigned.
Retained native log and installed evidence: artifacts/print-clean-native-38126078284.log
and artifacts/print-clean-verified-installed-38126078284 (six real PDFs).

Two additional native-output cases now compile/format locally: first printed
heading glyph ratios for standard/matching zoom, real40px padding shift and paired
find/source selection/screen paint after each PDF; saved user-theme240px block
width versus full page and actual optional border rows. New helper edits actual
native options, including padding NumberBox; all generated PDF/PNG bytes retained.
Signedd0a225d / CI38127089763 finishes native/published53/54, zero skips.
Actual saved custom-width/border/full-page case passes. Font/padding case fails
before PDF output at a requested zoom wait; installed channels are not
reached. Exact expected/actual glyph rectangles and native state are now retained
on failure. Diagnostic6e17674 proves the second step: initial35px,121% actual44px,
expected42.35px from multiplying an already-rounded UIA text box. The new PDF
case now requires actual increasing screen glyphs and completed native percentage,
plus bounded final screen growth; precise standard/matched font ratios remain
asserted against real PDF glyph bands. No PDF/state assertion is removed.
Retained red log and published TRX:
artifacts/print-geometry-native-38127089763.log and
artifacts/print-geometry-installed-38127089763. Corrected head requires54
native/published and53 per installed channel. No local GUI input or new dependency.
Correctedbf4fcdc / CI38128311242 fully passes: native/published54 each,
installed stable/dev53 each, zero failures/skips. All5 Print_ cases pass in every
build, including actual PDF font/padding/paired-find and original cancellations/
image/background/nonzero-state cases. Retained24 PDFs/48 PNGs/TRXs:
artifacts/print-font-verified-installed-38128311242; native log:
artifacts/print-font-native-38128311242.log. Actual before.AppKitApplyCalled and
restarted=true in both channels; expected stable0.4.0/dev0.3.0-beta.2; all before/
after check/apply errors null. Fixtures unsigned; production signing not proved.
ADR-0003 remains proposed for maintainer review.

Live settings validate. Official conformance34PASS/1FAIL/1WARN/3REVIEW: required
standards inventory lacks app, confirmed live. Exact reviewed addition prepared,
not applied/merged; see docs/standards-readiness.md. Actual signing remains deferred.

## Native windows and recents milestone

New window-local initial paths replace rereading process arguments. Track native
XAML windows on one UI thread; independent watcher/browser/hooks/zoom; Ctrl+N/Ctrl+W
and Exit; shared log closes only after the last reader. Modern App SDK multiple-file
picker includes md/markdown/mdown/mkd/txt. Private recent history20, successful opens
only, duplicate focus, missing errors and clear. Source-generated delta merging
protects sibling settings and cleared history from stale window snapshots.

Signed/DCOe48a08c implements this milestone. Local376 core tests/100% lines and
zero-warning build/format/docs/hooks pass. Three new hosted-only
native lifecycle cases compile; no local GUI input. ADR-0004 and threat model
updated. 1cfc07f / CI38129552334 native54/57, zero skips: all original54 pass,
three new cases fail. First caches a glyph across zoom navigation; second requests
unsupported Invoke from SourceToggle after actual New/Recent/Close/missing/clear
steps; third cannot find Open picker's filename field. Fresh glyph lookup and
verified owned physical menu input retain all state/reload/history assertions.
Picker diagnostics now retain owned tree and generic exception/HRESULT; no selector
change is guessed. Actual native foreground focus and source preference persistence are required.
The missing-file window loaded nonempty history before Clear; its later preference
save must not resurrect that snapshot.
Retain native red log: artifacts/windows-first-native-38129552334.log. Published process timed out at300seconds after two recorded new-case failures;
no TRX/installed-channel outcomes. Retained partial output/screenshots:
artifacts/windows-first-failed-installed-38129552334. Outer full-UI-suite budget
now600seconds; all individual10/30second waits/assertions unchanged. Association/activation registration remains
required. Full reference audit also retains theme ordering/enable/delete/cycle,
JSON import/export conflicts, all typography/global CSS/content width and four
configurable rendering extensions in PRODUCT/TODO; none is claimed complete.

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

1. Require native/published57 and installed stable/dev56 on the window milestone; retain actual lifecycle/picker/update evidence.
2. Complete source raw-heading audit and broader zoom/theme/find/collapse proof.
3. Complete print #12 output/state acceptance, windows/recents/activation and remaining theme/render/link parity.
4. Finish standards inventory; exact signed stable/dev artifacts remain deferred.

Update TODO and tickets with measured results; keep the PR draft until reviewed.
Current exact commit/job IDs are in ignored artifacts/CURRENT-CHECKPOINT.json.
Historical details: [reader/source notes](history/reader-source-session-2026-10-11.md).
