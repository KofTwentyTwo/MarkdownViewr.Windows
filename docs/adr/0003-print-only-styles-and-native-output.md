# ADR-0003: Independent print styles and native Windows output

**Status:** Proposed; implemented on the draft branch, desktop acceptance pending
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
published/stable/dev suites and the hostile-document controls. Desktop results are
not yet established; broader print layout/font/padding comparisons remain open.

Sources: [Microsoft printing overview](https://learn.microsoft.com/en-us/microsoft-edge/webview2/how-to/print?tabs=dotnetcsharp),
[App SDK save picker](https://learn.microsoft.com/en-us/windows/apps/develop/files/pickers-save-file),
[pinned print coordinator](https://github.com/darinkelkhoff/markdownViewr/blob/6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021/ContentView.swift).
