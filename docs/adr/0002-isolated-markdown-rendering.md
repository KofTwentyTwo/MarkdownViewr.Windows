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
Document-authored styles, IDs and classes are removed. Application theme CSS is generated from validated six-digit colors, bounded finite sizes and escaped font names. Theme custom CSS escapes every less-than sign before HTML embedding, preventing style-element breakout; CSP and resource interception continue to deny remote resources.

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
The 20 original themes are embedded unchanged, with their MIT notice shipped in THIRD-PARTY-NOTICES.md. System/SF Mono defaults map to Windows font fallbacks. Document theme selection persists separately from the AppKit shell theme and retains the viewport. Editable themes and native heading/section controls are implemented and verified; full syntax-color parity and synchronized source navigation remain pending. Automatic save
reload uses fixed native ExecuteScriptAsync queries on the checked current document;
this API works while authored scripts remain disabled. Browser results must be exactly
two finite numeric fractions, clamped to 0–1; only invariant numeric arguments enter the
fixed restore expression. No strings from documents become executable script.

Any later document-created JavaScript or native message bridge requires a revised CSP,
origin/nonce/schema checks and tests before enabling it. Custom CSS must not introduce network or file access.

## Native source and find

The optional source surface is a native read-only TextBox holding the same bounded,
literal snapshot as the reader. It does not parse HTML. Core find uses case-insensitive,
nonoverlapping UTF-16 source positions and pairs counts with the rendered surface.

Use the pinned runtime's [native finder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2find.startasync), suppress its default dialog, and provide one native Ctrl+F toolbar.
Query strings enter the native API directly; no search term becomes executable script.
Disable browser feature accelerators so the host owns find/print/reload commands;
[standard movement, selection and copy shortcuts remain available](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2settings.arebrowseracceleratorkeysenabled?view=webview2-dotnet-1.0.4191.47).

Typing is debounced and native searches serialized. Check the current page/navigation
before applying results; repeat the current term after reload/theme navigation. Search
reveals sections temporarily and restores their saved choices on close, incorporating
explicit native section commands during search. Source synchronization and mirrored collapse
remain separate work. Authored scripts, messages and host objects remain disabled.

Reading keys use the current app UI thread's existing message hook, bounded native
command enums and exact browser/read-only-source focus. Find/dialog controls retain
normal input. Translate only reading input with the active keyboard layout and
ToUnicode flag 4, preserving dead-key state; Alt/Windows/extra Ctrl modifiers are
ignored. Timed prefixes reset on focus/page/window transitions; held keys cannot
imitate gg/heading sequences. Dispatch rechecks focus. Browser scrolling and visible
heading operations use fixed host scripts and generated numeric identities only.
No content script, native bridge or dependency is added. Physical-key browser and
text-entry acceptance remains distinct from pure key-model tests.

## Parser-derived source provenance

The source synchronization foundation maps AST headings and raw HTML tokens to
original UTF-16 offsets. ATX/Setext, quote/list prefixes, fences, duplicate labels
and frontmatter retain parser identity rather than text/regex matching. Raw HTML
line lookup uses indexed offsets; source text remains immutable. Only headings
surviving the ordinary passive sanitizer receive a source identity.

A fresh per-render private attribute carries this provenance through sanitation;
its generated name is allowed only for the internal outline render, then removed
from every element before presentation. The public renderer never admits it, and
authored IDs/data attributes remain stripped. No source marker grants a browser
capability. Full source scroll/heading/collapse UI synchronization remains pending;
these parser tests do not prove browser behavior. ADR status remains proposed.

## Verification and follow-up

Core regression tests cover GFM, hostile HTML and resource paths at 100% core line
coverage. Native adapter tests cover BOM decoding, byte limits, exact image bytes and
a real junction pointing outside the document folder. Browser/UI acceptance is a
separate gate; these unit tests do not prove browser behavior or release signing.

- [ ] Review this decision and complete document UI acceptance.
- [x] Complete hosted save/replacement/deletion/scroll, editable-theme, outline/frontmatter and literal-source acceptance.
- [ ] Complete paired find, synchronized source/vim and print acceptance from PRODUCT.md.
- [ ] Verify WebView2 runtime deployment and exact signed installed release artifacts.

## References

- [Markdig](https://github.com/xoofx/markdig)
- [HtmlSanitizer](https://github.com/mganss/HtmlSanitizer)
- [Microsoft WebView2 security guidance](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security)
- [Native WebView2 script execution](https://learn.microsoft.com/en-us/microsoft-edge/webview2/reference/winrt/microsoft_web_webview2_core/corewebview2)
- [GetFinalPathNameByHandleW](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfinalpathnamebyhandlew)
