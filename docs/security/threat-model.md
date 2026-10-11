# Threat model: MarkdownViewr

- **Version reviewed:** first reader implementation
- **Last reviewed:** 2026-10-10
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

Only deliberate native configuration selects an existing, bounded, fully qualified
`.exe`. AppKit sanitizes the saved choice. Opening the editor requires a current
existing document and passes its full path as exactly one `ArgumentList` value with
`UseShellExecute=false`; Markdown cannot choose an executable or supply switches.
The chosen application runs with the user's permissions and receives the document.
Missing files and native launch errors leave the viewer open; logs omit paths and
contents. Core tests cover metacharacters/Unicode, relative/script paths, corruption
and bounds. Actual fixture-process/UI acceptance passed on 5a9af63 in native, published and
installed channels; reference editor-list
and folder-opening parity still require review.

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

300 core tests cover folding, original/native offsets and malformed numeric frames
at 100% measured line coverage. Hosted actual glyph/viewport and fold/find/reload
acceptance is pending; raw multiline/same-line heading parity still needs audit.
