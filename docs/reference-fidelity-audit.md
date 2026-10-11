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
Actual tooltip placement covers a later Contents click; local placement/non-hit-
testing is corrected. Exact picker1148/Edit exists in owned enumeration despite
the provider lookup failure; local acceptance uses that same owned identity.
Retain current-focused-b5e519e.log/current-native-b5e519e.log and focused TRX/
Settings screenshot bundle. Fresh hosted runtime remains required.

Printing now has a locally prepared native Settings page sharing the existing
output editor and checked saved defaults. Apply/Restore/closing and concurrent
sibling draft preservation have compiled actual native cases, not execution
proof. Full owner design acceptance and production signing remain outstanding.
