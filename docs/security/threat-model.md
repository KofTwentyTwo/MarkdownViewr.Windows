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
| Source pane | Same bounded committed snapshot in a native read-only TextBox; literal HTML/CSS/script text never enters a renderer or command interpreter; hidden pane removed from accessibility surface |
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

This is a first reader, not a completed port. Native frontmatter/outline/literal-source acceptance passed CI 38106991467 in loose, published and both installed builds. New find acceptance, synchronized source/collapse, external editor commands,
print and any future browser bridge need their own controls and tests. Save watching is
implemented; hosted real browser replacement/deletion/scroll and remote-image denial pass on 77dd04d.
Local links to other documents and mailto links are currently blocked. WebView2 uses
the installed evergreen runtime; published and both installed channels passed reader acceptance on 77dd04d and editable themes on 30ca094. New paired-find keyboard/source/viewport/hostile-query acceptance remains required.

Historical EX-0002 does not authorize unsigned production publication. Azure setup
and real final-artifact signature validation are deferred; an active profile alone
does not establish working release signing.

## Review log

| Date | Reviewer | Changes |
| --- | --- | --- |
| 2026-10-10 | Codex (GPT-6) | Added directory watching and guarded numeric viewport preservation; hosted reload acceptance pending |
| 2026-10-10 | Codex (GPT-6) | Replaced template-only interfaces with actual document and browser boundaries; recorded separate unit/native/browser evidence |

## Owned-window keyboard boundary

Browser-focus shortcuts cannot rely on XAML menu/root acceleration alone. The
Windows adapter uses a current-UI-thread `WH_GETMESSAGE` hook, requires this
foreground window or its children, recognizes only Ctrl+F without additional
modifiers, and queues the existing native find command. Other messages pass
through; closing the window removes the callback. No global keyboard registration,
other-process input capture, arbitrary command dispatch, browser bridge or text
logging is introduced. Actual hosted browser-focus acceptance is still required.
See Microsoft's [thread-hook contract](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexw).
