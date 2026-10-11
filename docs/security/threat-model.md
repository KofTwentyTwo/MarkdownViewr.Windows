# Threat model: MarkdownViewr

- **Version reviewed:** first reader implementation
- **Last reviewed:** 2026-10-11
- **Method:** data flow and STRIDE; review again for each attack-surface change.

## Product and boundaries

A per-user WinUI application reads user-selected local Markdown. AppKit supplies
settings, logging, crash handling and Velopack updates. The UI-free core parses and
sanitizes Markdown. WebView2 displays the sanitized result through an intercepted
HTTPS origin. No application HTTP server or scriptable command-line head exists.

Document text, HTML, links, images and filesystem aliases are untrusted. The user
selects the document; this authorizes reading it and passive images within its
physical directory. It does not authorize native commands or remote resource access.

## Interfaces and controls

| Interface | Control |
| --- | --- |
| Picker / literal process argument | Read-only bounded snapshot; no shell interpolation |
| Frontmatter | Bounded by document limit; complete leading delimiters only; metadata values HTML-escaped; Markdown mode uses the same sanitizer; no YAML execution or native commands |
| Markdown / raw HTML | Explicit tag/attribute/scheme allowlist; disabled checkbox inputs; no authored scripts, CSS, IDs or classes |
| Native print/PDF | Deliberate File/Print/Ctrl+P; ready passive document only; copied bounded settings; Windows destination picker; JSON-serialized CSS text in host-owned print stylesheet; same navigation checked after preparation; no authored command/output path; no document/path logs |
| File change notifications | Directory subscription scoped to selected path; 350 ms quiet-period debounce; replacement/deletion recovery; bounded transient retries; subscriptions released on window close |
| Theme JSON / CSS | Native editor and saved user overrides use the same bounded validation; corrupt theme settings are discarded before startup; restoration keeps separately named custom themes; embedded original palettes; bounded schema-version-one parser; validated hex colors and finite sizes; code-point escaped font names; less-than signs escaped before style embedding; no network capability |
| Browser viewport | Fixed origin-checked native query; validate two finite fractions; invariant numeric restore only; authored scripts/messages stay disabled |
| Source pane | Bounded immutable snapshot plus literal native fold projection; original UTF-16 find positions map to visible native CR text; HTML/CSS/script text never enters a renderer or command interpreter; hidden pane removed from accessibility surface |
| Find | Bounded literal query; pure source positions and native WebView2 finder, with duplicate browser UI suppressed; no query enters executable code or activity logs. Serialized queries and page/navigation checks reject stale completions; existing numeric section operations reveal/restore content |
| Fixture input diagnostics | Explicit opt-in environment switch; observes handled native pointer/drag events without changing routing; logs owned control names, coordinates and button state only, never document text or keystrokes |
| Browser surface | Scripts, messages, host objects, permissions, downloads, frames and popups disabled; restrictive CSP |
| Local images | Per-document URI prefix; one decode; filename/extension policy; final open-handle containment; 16 MiB limit |
| Remote resources | Denied by CSP and resource interception |
| External links | HTTP(S) only, actual browser user gesture, external Windows handler; no document navigation outside current page |
| Heading navigation | Labels extracted after sanitation; authored identity attributes stripped; generated numeric targets only. Fixed host operations reveal/jump/collapse on the current intercepted page; authored scripts, messages and host objects remain disabled |
| Settings / logs | AppKit sanitation; per-user data root; log operational events without document content or paths |
| Updates | Repository HTTPS feed, Velopack integrity checks and production signing preflight |
| Repository / CI | Protected main and release tags; producer-bound checks; no untrusted PR checkout with privileged review token |

## Threats and verification

| Threat | Mitigation and evidence |
| --- | --- |
| Document script or native-command execution | Sanitizer regressions pass; host disables scripts/bridge; actual browser malicious-script and reader acceptance passed on 77dd04d |
| Image traversal, device/ADS paths or encoded separators | Core path-policy tests pass; denied before filesystem access |
| Junction/symlink escape or check/reopen race | Check normalized physical path on opened image handle; read same handle; real junction rejection test passes |
| Remote image tracking / resource exfiltration | No-referrer header, no document networking; actual listening-server browser denial passed on 77dd04d |
| Oversized input / image denial of service | Pre-parse character bound and image byte bound; native limit tests pass; performance acceptance remains |
| Settings corruption / UI exception | AppKit shell sanitation and crash handling; inherited foundation tests |
| Tampered installer/update | Fail-closed signing/provenance/checksum release pipeline; real Azure CI authorization and signed artifacts remain unverified |
| Privileged repository review abuse | Trusted review workflow only; current-SHA human acknowledgement remains required |

## Remaining risks and work

This is a first reader, not a completed port. Native frontmatter/outline/literal-source acceptance passed CI 38106991467 in loose, published and both installed builds. Paired find and configured editor launch passed hosted and installed acceptance; new synchronized source/collapse, broader editor parity,
print and any future browser bridge need their own controls and tests. Save watching is
implemented; hosted real browser replacement/deletion/scroll and remote-image denial pass on 77dd04d.
Local links to other documents and mailto links are currently blocked. WebView2 uses
the installed evergreen runtime; published and both installed channels passed reader acceptance on 77dd04d and editable themes on 30ca094. Paired-find keyboard/source/viewport/hostile-query acceptance passed on 5a9af63. New source synchronization acceptance remains required.

Historical EX-0002 does not authorize unsigned production publication. Azure setup
and real final-artifact signature validation are deferred; an active profile alone
does not establish working release signing.

## Review log

| Date | Reviewer | Changes |
| --- | --- | --- |
| 2026-10-10 | Codex (GPT-6) | Added directory watching and guarded numeric viewport preservation; hosted reload acceptance pending |
| 2026-10-10 | Codex (GPT-6) | Replaced template-only interfaces with actual document and browser boundaries; recorded separate unit/native/browser evidence |

## Owned-window keyboard boundary

Browser-focus shortcuts use a current-UI-thread `WH_GETMESSAGE` hook requiring the
active owned window/child. Ctrl+F remains a native window command. Vim keys require
the actual passive-browser/read-only-source focus; native text controls retain input.
Alt/Windows and extra Ctrl modifiers do not become reading commands. Only bounded
enums leave the key model; prefixes expire/reset and held prefix keys do not complete
sequences. Layout translation uses ToUnicode flag 4 to preserve dead-key state;
non-reading input is not translated. Dispatch rechecks focus. Other messages pass
through; closing removes the callback. No global hook, other-process capture,
arbitrary command, browser bridge or text logging is introduced. Fixture-only focus
traces contain native control names/types, never query or source text. Actual hosted
physical keyboard acceptance is required.
See Microsoft's [thread-hook contract](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexw) and [layout conversion contract](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-tounicode).

## Configured editor boundary

Only deliberate native configuration selects existing, bounded, fully qualified
`.exe` editors. AppKit sanitizes at most 32 named stable-ID choices, discarding
invalid/duplicate IDs, corrupt names and executable paths. Legacy single-path
settings migrate into one entry; cancel edits only a copied list. Opening the editor requires a current
existing document and passes its full file path or selected parent folder as exactly
one `ArgumentList` value with
`UseShellExecute=false`; Markdown cannot choose an executable or supply switches.
The chosen application runs with the user's permissions and receives the document.
Missing files and native launch errors leave the viewer open; logs omit paths and
contents. Core tests cover metacharacters/Unicode, relative/script paths, corruption
and bounds. Actual fixture-process/UI acceptance passed on 5a9af63 in native, published and
installed channels; new named-list/submenu order, cancellation/removal and actual folder/file probe
launch acceptance remain pending.

## Source provenance boundary

Source anchors originate from Markdig heading spans and AngleSharp raw HTML token
positions, with original newline/frontmatter/UTF-16 offsets. Repeated titles or
fenced heading-like text cannot select another heading's identity. A fresh private
attribute name is admitted only during the internal render and removed before any
HTML is presented; public renderer policy and authored ID/data-attribute denial
remain unchanged. Positions pair only with sanitized surviving headings and do
not grant scripts, messages, native access or resources. The original snapshot is
immutable. Core regressions cover nested raw markup, Setext, fences, Unicode,
frontmatter and forged attributes; source scroll/folding UI acceptance is pending.

## Source synchronization boundary

Only fixed host operations capture numeric scroll positions, generated heading IDs
and hidden generated bodies on the matching ready document. Exact JSON shape,
bounded array lengths, unique in-range integer IDs and finite coordinates are
required. No source text, heading label or query enters executable code. The native
adapter serializes polling, rejects stale snapshot/query/page completions and
suppresses feedback around find selections and its own viewport movements. Closing
stops the timer; hidden panes do not synchronize. Literal ellipses only affect the
presentation; canonical document bytes and independent nested fold choices remain.
Opt-in fixture geometry logs contain numeric positions/counts only.

330 core tests cover editor migration/persistence/folder arguments, folding, original/native offsets and malformed numeric frames
at 100% measured line coverage. Hosted actual glyph/viewport and fold/find/reload
acceptance is pending; raw multiline/same-line heading parity still needs audit.

Entering read-only source now defers restoration of its prior viewport after native
focus/caret revelation, guarded by the current snapshot/window and excluding active
find. An unselected read-only caret is positioned at the first visible native character
before focus, preventing the observed later caret reset. The original post-focus
glyph assertion passes on 1c4d1ad; source-to-reader acceptance remains open. Hosted acceptance must verify actual glyph positions before/after
focus and native source-to-reader movement; numeric fixture logs contain no text.

## Reading zoom

Zoom remains window-local and receives only explicit owned native commands. Its
finite 0.3–5 scale multiplies validated font sizes with invariant numeric formatting;
line height, colors, theme JSON and custom CSS retain their existing semantics.
No authored script, message bridge or new resource capability is enabled. Navigation
uses existing matching-page viewport/collapse guards and ready-page find replay.
Ctrl zoom commands require reading focus; editor/theme/find text input retains
native ownership. 330 core tests/100% measured lines pass; actual glyph/key/reload
acceptance is pending. Native menus/toolbar do not imply production release proof.

## Print lifecycle

Reading and print styles have separate `screen`/`print` media attributes. Even
custom CSS braces cannot escape a stylesheet's media restriction. Printing never
changes reading theme data, source text, find state or generated collapse flags;
print CSS exposes folded bodies only in print media. Native source feedback pauses
through the settings/PDF operation, and geometry queries reject print-media frames.
WebView2 native print UI has no completion callback: opening it is not proof of a
successful job. Native PDF output is awaited and written only to the user-chosen
Windows picker destination. PDF export disables headers/footers; fixture acceptance
cancels print UI and renders owned generated PDFs, never submits a physical job.
The existing CSP, denied remote requests and opened-handle image containment apply.
See ADR-0003; full hosted/output/layout verification remains pending.

## Document windows and recent files

Only literal process arguments and deliberate native picker/recent commands open
documents. The App SDK multiple-file picker is bound to its owning AppWindow.Id.
A new reader receives its own initial path, browser, watcher and keyboard/window
lifetime; document content cannot open windows or invoke native menus. Reuse/focus
is restricted to matching canonical paths within the current process. Browser CSP,
local-image handle containment and passive-content policy apply independently to
every reader; closing one stops only its owned callbacks and hooks.

Recent paths remain private in the local AppKit settings file, capped at20 and
32767 characters per entry; relative paths, URLs/control characters and duplicates
are discarded. Record only successful parsed opens, never missing/unreadable files.
No document content or paths enter activity logs. Native Clear removes history,
not files. Source-generated property-delta merging preserves sibling preferences
and prevents an older window snapshot from restoring cleared paths. Core tests
cover corruption, bounds, ordering, independent arrays and stale-window merging.
Actual hosted lifecycle/clear/reload tests are pending; association/activation
registration has not been implemented or represented as verified. See ADR-0004.
