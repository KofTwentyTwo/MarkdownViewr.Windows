# ADR-0002: GFM rendering inside an isolated WebView2 document surface

- **Status:** Proposed; implemented on the viewer topic branch, pending review
- **Date:** 2026-10-10
- **Decider:** Project maintainer
- **Tracking:** [reader milestone #4](https://github.com/KofTwentyTwo/MarkdownViewr.Windows/issues/4)

## Context

Darin's document layout needs HTML/CSS, GFM tables, tasks and useful raw HTML.
Documents can contain hostile markup, resource paths and links. A Markdown parser
alone does not provide a native application security boundary.

## Decision

Use Markdig 1.4.0 (BSD-2-Clause) for GFM and HtmlSanitizer 9.2.1039 (MIT) for
an explicit passive-markup allowlist. AngleSharp and AngleSharp.Css are their MIT
dependencies. Remove the unused sample greeting and CommunityToolkit dependency.
Versions and transitive hashes are committed in the NuGet locks.

Host the sanitized result in WinUI WebView2. Serve an intercepted logical HTTPS
document, with a random resource namespace for each open; no local HTTP listener.
The document response carries a deny-by-default CSP and no-referrer/no-store headers.
Only application-generated CSS is allowed; authored styles, IDs and classes are removed.

Disable scripts, web messages, host objects, frames, downloads, popups and permission
requests. Reject all browser resources except the exact document and allowlisted
local image requests. External HTTP(S) navigation requires a real user gesture
and opens in Windows' external handler. Other links remain blocked in this slice.

Decode resource paths once; reject traversal, alternate streams, devices and Windows
filename aliases. Before reading an image, resolve its opened handle with
GetFinalPathNameByHandleW and compare it against the physical document directory.
Read from that same handle, preventing a check/reopen race. Remote images are blocked.
Bound documents to 16 Mi characters and individual images to 16 MiB.

## Options considered

| Option | Benefit | Cost |
| --- | --- | --- |
| Markdig, sanitization and WebView2 | Maintained parsing; compatible HTML/CSS surface | Requires explicit browser/filesystem containment |
| Parser plus unrestricted HTML | Small initial implementation | Exposes script, resource and native navigation risks |
| Entirely native text layout | Avoids an embedded browser | Reimplements tables, themes, print and navigation |

## Consequences

The reader supports a picker, literal launch arguments, manual reload and debounced save watching.
It does not yet reproduce Darin's themes, syntax colors or navigation. Automatic save
reload uses fixed native ExecuteScriptAsync queries on the checked current document;
this API works while authored scripts remain disabled. Browser results must be exactly
two finite numeric fractions, clamped to 0–1; only invariant numeric arguments enter the
fixed restore expression. No strings from documents become executable script.

Any later document-created JavaScript or native message bridge requires a revised CSP,
origin/nonce/schema checks and tests before enabling it. Custom CSS must not introduce network or file access.

## Verification and follow-up

Core regression tests cover GFM, hostile HTML and resource paths at 100% core line
coverage. Native adapter tests cover BOM decoding, byte limits, exact image bytes and
a real junction pointing outside the document folder. Browser/UI acceptance is a
separate gate; these unit tests do not prove browser behavior or release signing.

- [ ] Review this decision and complete document UI acceptance.
- [ ] Complete hosted save/replacement/deletion/scroll acceptance; add navigation, themes, source/find and print from PRODUCT.md.
- [ ] Verify WebView2 runtime deployment and exact signed installed release artifacts.

## References

- [Markdig](https://github.com/xoofx/markdig)
- [HtmlSanitizer](https://github.com/mganss/HtmlSanitizer)
- [Microsoft WebView2 security guidance](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security)
- [Native WebView2 script execution](https://learn.microsoft.com/en-us/microsoft-edge/webview2/reference/winrt/microsoft_web_webview2_core/corewebview2)
- [GetFinalPathNameByHandleW](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfinalpathnamebyhandlew)
