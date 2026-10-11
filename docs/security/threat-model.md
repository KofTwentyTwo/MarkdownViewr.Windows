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
| Markdown / raw HTML | Explicit tag/attribute/scheme allowlist; disabled checkbox inputs; no authored scripts, CSS, IDs or classes |
| Browser surface | Scripts, messages, host objects, permissions, downloads, frames and popups disabled; restrictive CSP |
| Local images | Per-document URI prefix; one decode; filename/extension policy; final open-handle containment; 16 MiB limit |
| Remote resources | Denied by CSP and resource interception |
| External links | HTTP(S) only, actual browser user gesture, external Windows handler; no document navigation outside current page |
| Settings / logs | AppKit sanitation; per-user data root; log operational events without document content or paths |
| Updates | Repository HTTPS feed, Velopack integrity checks and production signing preflight |
| Repository / CI | Protected main and release tags; producer-bound checks; no untrusted PR checkout with privileged review token |

## Threats and verification

| Threat | Mitigation and evidence |
| --- | --- |
| Document script or native-command execution | Sanitizer regressions pass; host disables scripts/bridge; actual browser acceptance pending |
| Image traversal, device/ADS paths or encoded separators | Core path-policy tests pass; denied before filesystem access |
| Junction/symlink escape or check/reopen race | Check normalized physical path on opened image handle; read same handle; real junction rejection test passes |
| Remote image tracking / resource exfiltration | No-referrer header, no document networking; browser network acceptance pending |
| Oversized input / image denial of service | Pre-parse character bound and image byte bound; native limit tests pass; performance acceptance remains |
| Settings corruption / UI exception | AppKit shell sanitation and crash handling; inherited foundation tests |
| Tampered installer/update | Fail-closed signing/provenance/checksum release pipeline; real Azure CI authorization and signed artifacts remain unverified |
| Privileged repository review abuse | Trusted review workflow only; current-SHA human acknowledgement remains required |

## Remaining risks and work

This is a first reader, not a completed port. User themes, external editor commands,
save watching, print and any future browser bridge need their own controls and tests.
Local links to other documents and mailto links are currently blocked. WebView2 uses
the installed evergreen runtime; installed deployment acceptance remains required.

Historical EX-0002 does not authorize unsigned production publication. Azure setup
and real final-artifact signature validation are deferred; an active profile alone
does not establish working release signing.

## Review log

| Date | Reviewer | Changes |
| --- | --- | --- |
| 2026-10-10 | Codex (GPT-6) | Replaced template-only interfaces with actual document and browser boundaries; recorded separate unit/native/browser evidence |
