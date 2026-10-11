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
literal folds, paired find/cancel and watched reload. Published/installed acceptance on that head is still running.

## Remaining release evidence

Complete current distributed acceptance, actual Default Apps/Explorer selection,
concurrent stable/dev isolation and owner design review. Retain real screenshots
and print/PDF output. Final signing, genuine maintainer review and publication
remain required; fixtures and CSS comparisons do not replace them.
