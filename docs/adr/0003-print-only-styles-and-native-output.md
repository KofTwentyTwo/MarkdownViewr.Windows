# ADR-0003: Independent print styles and native Windows output

**Status:** Proposed for maintainer review; all5 native/published/installed print cases pass
**Date:** 2026-10-11
**Deciders:** James Maes, repository maintainer

## Context

Pinned markdownViewr `6d08bc9` temporarily replaces screen CSS and restores it in
its macOS modal print completion callback. Windows WebView2 `ShowPrintUI` returns
void and does not expose that completion lifecycle. Printing must retain the
reading theme, font zoom, scroll, find, source and nested collapse state after
success, cancellation or failure. Use published AppKit 0.1.0 and existing WebView2.

## Decision

Keep reading theme/custom CSS in a stylesheet with `media="screen"`. A deliberate
File/Print or Ctrl+P command generates a separate host-owned `media="print"`
stylesheet using validated saved print choices. Stylesheet media attributes also
contain custom CSS with unbalanced braces: print CSS cannot affect screen media.
Never replace reading CSS, mutate canonical content, expand DOM collapse flags,
stop find or navigate/reload merely to print. Print CSS shows generated collapsed
bodies; their original `hidden` attributes remain intact for the screen.

Provide Clean Printing (reference defaults), Active Theme, Plain HTML and named
built-in/user themes; backgrounds off/images on; standard/matching font zoom;
full/document width; optional borders and 0–96 pixels of page padding. Native
Windows print UI owns printer selection and cancellation. PDF saving uses an
explicit Windows App SDK destination picker and awaited WebView2 PDF output. PDF margins
are 0.5 inches, matching the reference's 36 points; scale is handled in font CSS.
No completion or successful printer job is inferred from opening native print UI.
Hosted PDF generation moved the same screen glyph41pixels after its awaited
operation; capture validated numeric reading geometry before PDF output and
restore its vertical position in finally, only for the same ready navigation.
Actual glyph/view rectangles verify restoration; the observed UIA scroll scalar
reported200% before printing and cannot serve as the viewport baseline.
Use the installed App SDK save picker with AppWindow.Id; do not introduce the
legacy WinRT picker/elevation constraint into the new PDF path.

Print/PDF uses the existing passive document, local image containment and resource
interception. No authored scripts/messages, remote resources, new native bridge,
output path from document content or private document/path logs. Fixed host script
assigns JSON-serialized CSS to a private stylesheet and validates the same ready
navigation afterwards. Pause native source feedback during the app's print draft/
PDF operation; geometry queries reject print-media frames while native print UI runs.

## Options considered

| Option | Benefit | Cost |
| --- | --- | --- |
| Separate media styles on the current passive page | Preserves screen state without a completion callback; existing image policy | Requires actual print-media/output and state tests |
| Replace screen styles, then restore them | Mirrors the macOS implementation | No reliable native print-UI completion signal; premature restoration changes output |
| A second print browser/window | Separate DOM snapshot | Duplicates page/resource lifetimes and navigation controls; extra browser state |

## Consequences and acceptance

Settings drafts are copied; cancel does not save them. No printer job is sent by
hosted acceptance: exercise native cancel and save owned fixture PDFs. Use Windows'
built-in PDF rasterizer to inspect real image/background/text output, with retained
PDF/PNG evidence. The test project targets the existing Windows SDK; no new PDF
library or runtime dependency is added. Keep 100% measured core coverage, full native/
published/stable/dev suites and the hostile-document controls. Original3 hosted native cases pass ec5f98f/38125751638, zero failures/skips:
actual default/themed PDFs and unchanged glyph/view/source/collapse state. Full
cleaned native/published/stable/dev gates and broader layout/font/padding/find
comparisons remain open. Retained PDF/PNG/TRX evidence is under
artifacts/print-exact-filename-38125751638.

Sources: [Microsoft printing overview](https://learn.microsoft.com/en-us/microsoft-edge/webview2/how-to/print?tabs=dotnetcsharp),
[App SDK save picker](https://learn.microsoft.com/en-us/windows/apps/develop/files/pickers-save-file),
[pinned print coordinator](https://github.com/darinkelkhoff/markdownViewr/blob/6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021/ContentView.swift).

## Expanded output proof in progress

The native fixture now compares actual first-heading ink bands for Standard and
Match Screen font scaling and40pixel padding. Another fixture creates a saved
user theme with240pixel content width and compares actual block-edge pixels with
borders off/on and full-page width. Each PDF is retained before raster assertions.
Successful exports also verify paired query/count, actual native source selection
and original reading paint/geometry. These new cases compile/format but have not
run; full expanded native/published54 and installed53 per channel remain required.

Full hosted acceptance `bf4fcdc` / CI38128311242 passes54 native/published and53
installed stable/dev tests with zero failures/skips. All5 print cases pass in
every build. Actual PDF glyph ratios,40px padding,240px custom width/full-page
edges, optional borders and paired find/source selection pass;24 generated PDFs
are retained. UIA screen text metrics round to integer font boxes (35px to44px at
121%), so the PDF case verifies real increasing screen glyphs/completed zoom and
measures precise font ratios in printed ink. Original exact screen restoration
and PDF pixel assertions remain. Evidence: artifacts/print-font-verified-installed-38128311242.
Unsigned fixtures do not prove production signing or a successful physical printer job.
