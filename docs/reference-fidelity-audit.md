# Reference fidelity audit

Reference: [Darin Kelkhoff’s markdownViewr](https://github.com/darinkelkhoff/markdownViewr/tree/6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021).
This records evidence and compatibility boundaries; it does not certify release readiness.

## Rendering and CSS

The upstream `Resources/template.html` and `CSSHelpView.swift` were retrieved
by immutable Git blob IDs and verified against their Git hashes. Previous cached
copies differ only in newline/BOM representation. Verified template blob: `4af334740dc345ad1292e3afac06bc9c12020a8b`;
CSS-help blob: `9b85b1fac8aca3e8542375b2420a6e0a0bbdbadc`. The screen-CSS declaration audit
retains `artifacts/pinned-css-audit.json`: all 47 shared selectors have matching
upstream declarations, accounting for later rules that override individual properties.
The Windows task-checkbox rule is an additional GFM adaptation.

`#content` remains the scroll host; `#content-inner` remains the document body.
Original palette/font variable names remain available in all 20 themes. Theme/global
CSS and reading-width overrides cascade separately. `DocumentStylesTests` checks
actual parsed declarations, containers, passive classes and print isolation;
those checks do not prove every browser’s painted layout.

## Native Windows panes

Contents, source and dividers are native WinUI controls. Upstream `#toc`,
`#raw-source`, `.raw-h`, `.raw-ellipsis` and browser-resizer CSS selectors do not
style these controls. Document CSS styles the rendered reader; source colors/fonts
come from the document palette and Windows light/dark/high-contrast handling.
Do not advertise arbitrary upstream pane-CSS compatibility.

## Heading, fold and find evidence

Parser identities retain original UTF-16 offsets through frontmatter, Setext,
raw multiline/same-line HTML and fenced pseudo-headings. A reproduced multiline
header-fold defect is fixed in `bd3d72a`; 589 core tests and 100% measured lines pass.
Its focused hosted suite passes 21/21 and full native 99/99, zero skips, including actual raw-heading jumps,
literal folds, paired find/cancel and watched reload. That head’s published suite timed out after two initial print-fixture failures;
installed lifecycle acceptance was not reached. The next pushed53755ab passes
focused21/21 and native99/99; its installed job remains live at this observation.

## Remaining release evidence

Complete current distributed acceptance, actual Default Apps/Explorer selection,
concurrent stable/dev isolation and owner design review. Retain real screenshots
and print/PDF output. Final signing, genuine maintainer review and publication
remain required; fixtures and CSS comparisons do not replace them.

## Owner-provided Mac design reference

Eight screenshots clarify the General/Editors/Themes/Markdown/Printing grouping,
aligned settings values, short explanations, palette swatches and Enabled/default
choices. Verified Windows gaps are tracked in issue #21: effective default-viewer
status in General, configurable Contents wrapping/depth markers, clearer theme
management/default semantics and a dedicated Printing page. Preserve the owner’s
Windows toolbar/default-pane choices. Private screenshots and document contents
are not reproduced here; this comparison does not certify the redesign.

A valid multiline raw heading within a Markdown paragraph exposed a separate
source-fold defect. Actual inline parser nodes now retain the complete header;
592 core tests pass with100% measured line coverage. The existing runtime fixture
now exercises this variant, with fresh hosted execution still required.

## Installed registration and coexistence evidence

Exact53755ab / CI38182128299 passes native/published99/99 and installed
stable/dev98/98 each, zero failures/skips. Actual registration readbacks verify
five extensions before/after each real AppKit update and owned removal with
existing defaults preserved. Separate concurrent stable/dev windows/profiles,
isolated updates and stable uninstall preserving the live dev edition pass.
Retain the actual TRXs and coexisting-channels.json under
artifacts/current-installed-evidence-38182128299. The fixtures are unsigned and
do not establish OS chooser/Explorer interaction or production signature trust.

## Current native regression findings

Exactb5e519e focused19/22 and native98/100 fail, zero skips; the new General/
Contents options case passes. A line-start closing raw tag is a separate Markdig
HTML block; a reproduced correction now retains complete inline title syntax
across consecutive blocks.598 core tests pass with100% measured line coverage.
A tooltip appeared during the later Contents click; interception was not proved
and placement changes did not resolve the failure. Local trim-only tips retain
the intended full-title UX. Exact picker1148/Edit exists in owned enumeration despite
the provider lookup failure; local acceptance uses that same owned identity.
Retain current-focused-b5e519e.log/current-native-b5e519e.log and focused TRX/
Settings screenshot bundle. Fresh hosted runtime remains required.

Printing now has a locally prepared native Settings page sharing the existing
output editor and checked saved defaults. Apply/Restore/closing and concurrent
sibling draft preservation have compiled actual native cases, not execution
proof. Full owner design acceptance and production signing remain outstanding.

Exactb5e519e distributed job is now terminal: published98/100, zero skips, with
the same raw-inline fold and physical source-click/alignment failures; installed
lifecycle phase not reached. Retain its actual published TRX under
artifacts/current-installed-evidence-38184378762. Signed localec78b50/7fa9f77
require fresh exact-head native/distributed acceptance after the next push.

## Theme manager and current acceptance

The pinned Mac Default checkbox writes ThemeManager.activeThemeName; its windows
can keep their own current selection. Windows continues its existing shared saved
DocumentTheme contract, applying committed choices to open readers and new
windows. The native manager now makes that behavior explicit through a default
radio control, five palette previews and textual state labels. No second saved
preference or silent migration is introduced. Actual native palette-pixel/default/
save/reopen acceptance is compiled and pending, including a Settings screenshot.

Exactb2ecc8c focused23/24 and native101/102 fail, zero skips; only physical
source/reader alignment fails. Both Printing cases, raw-inline folding, picker
cancellation and Contents presentation pass in those completed suites. Published
acceptance exceeds the unchanged600-second bound with no final TRX; installed
lifecycle is not reached. Actual logs/artifacts are retained. Closing all fixture-
owned windows before FlaUI's unchanged close/kill fallback addresses repeated
five-second teardown waits; fresh whole-run evidence is required.

The remaining repeated-activation candidate restores the trusted DocumentHeading
Tag on the new Contents content Grid. ItemClick identifies its clicked native
FrameworkElement; an already selected row produces no SelectionChanged event.
The unchanged actual physical source/Contents regression must verify this on the
next head. Pointer diagnostics are client coordinates, not screen-space bounds;
the earlier direct coordinate/tooltip-interception inference was not established.

## Exact current native proof and placement finding

Exact9d5c0d1 / CI38188145397 focused26/26 and native106/106 pass, zero skips.
Actual repeat-click/source geometry, palette pixels/default/save/reopen, future
schema export, trimmed tooltip/physical activation, Printing and raw-inline cases
pass. Retain actual TRX/screenshots under reader-regressions-38188145397 and both
current native/focused logs. Current distributed acceptance remains live.

Actual Settings screenshots reveal the window extends beyond the monitor despite
its bounded size. Local FitWindow now centers the resized window in its monitor
work area; the existing native layout test adds desktop containment. Build/format
pass; fresh rendered visibility and full runtime acceptance remain required.
