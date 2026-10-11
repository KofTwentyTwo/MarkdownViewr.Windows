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
| Markdown / raw HTML | Explicit tag/attribute/scheme allowlist; disabled checkbox inputs; no authored scripts, CSS or IDs; only bounded code-language and fixed document classes survive |
| Native print/PDF | Deliberate File/Print/Ctrl+P; ready passive document only; copied bounded settings; Windows destination picker; JSON-serialized CSS text in host-owned print stylesheet; same navigation checked after preparation; no authored command/output path; no document/path logs |
| File change notifications | Directory subscription scoped to selected path; 350 ms quiet-period debounce; replacement/deletion recovery; bounded transient retries; subscriptions released on window close |
| Theme JSON / CSS | Native editor and saved user overrides use the same bounded validation; corrupt theme settings are discarded before startup; restoration keeps separately named custom themes; embedded original palettes; bounded strict version-one parsing and validated compatible saved/import schemas with explicit newer-import consent; validated hex colors and finite sizes; code-point escaped font names; less-than signs escaped before style embedding; no network capability |
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

## Configurable Markdown extensions

Only deliberate native menu commands change the four saved rendering preferences;
document content cannot modify them. Each asynchronous read captures immutable
options. Existing Markdig parsing generates passive mark/sup/sub/ins nodes and
preserves original source offsets. Reference caret/tilde whitespace and protected
code/newline boundaries remain literal. The sanitizer still removes authored
styles, identity/event attributes and unsafe links; mark/ins grant no native or
network access. Host-generated theme CSS uses validated palette variables.

Separate saved fields preserve sibling preference edits. Core tests cover all16
option combinations, code/escapes, hostile attributes, original source provenance
and old JSON defaults. Actual native repaint/toggle/reload acceptance is prepared
for hosted runners and remains unverified. Raw authored-HTML extension edge cases
remain part of the reference audit; no broader renderer-parity claim is made.

## Complete native typography editing

All font families/sizes and line height use native controls backed by a copied
core theme edit. Known field names, finite/ranged numbers, bounded escaped font
strings and hex colors pass the existing renderer validation before saving.
Unknown fields and invalid drafts are rejected; unrelated schema metadata is
preserved. No document content can invoke these edits. Custom CSS still cannot
close its style element, and existing CSP/resource limits apply. Core tests prove
invalid input leaves originals intact, successful edits retain all fields, and
hostile font/CSS strings preserve the HTML boundary. Actual glyph/save/cancel
acceptance compiles for hosted runners and is not yet verified.

## Shared reading appearance

Only deliberate native settings commands save global CSS and content width.
Copied drafts validate finite200–4000px widths and a128Ki-character CSS bound;
corrupt persisted values fall back safely. Settings are merged against the latest
store before broadcasting to open readers. Revision checks reject obsolete
asynchronous application; updating sibling baselines prevents later local saves
from restoring an older global preference.

Global CSS is escaped for HTML style embedding and remains in screen-only media.
Per-theme CSS retains precedence; an enabled width limit is applied last, scaled
with reading zoom. Print CSS uses its existing independent theme/layout path.
No new script, host object, message bridge or remote-resource permission is enabled;
existing CSP and denied network requests still apply.448 core tests/100% measured
lines pass. Two new hosted cases compile but have not run; actual sibling paint,
wrapping and PDF isolation are not yet declared verified.

## Shared theme management

Only owned native manager/menu/cycling commands change saved theme lists or the
active palette. Copied drafts retain cancellation; names are bounded, deduplicated
and reject control characters. Disabled themes remain editable while disappearing
from reading menus. Built-in deletion only removes settings overrides and records
a restorable name; shipped resources and document files are untouched. Restoring
originals preserves edited JSON/metadata as uniquely named custom copies.

Latest-store field merging and reader save baselines preserve unrelated sibling
preferences. Catalog/selection broadcasts and browser revision checks discard
obsolete asynchronous theme applications. Cycling uses only enabled saved order
and exact Ctrl+Shift arrows with owned reading focus; text controls retain input.
Existing schema/font/CSS validation, screen/print separation, CSP and resource
denial remain enforced.471 core tests/100% measured lines pass. Native menu/paint/
keyboard/sibling and revised restoration acceptance compile but remain unexecuted.

## Native theme transfer and copied manager dialogs

Theme JSON enters only through deliberate owned native file pickers. Serial reads
allocate at most the128Ki-character bound plus one before schema/depth validation;
UTF-8 and recognized BOM encodings retain Unicode. Invalid files fail independently
within a batch. Compatible newer schemas still validate all known colors/fonts/
sizes/CSS and require explicit native consent before import persistence. Strict
version-one parsing remains available. Saved approved versions and unknown metadata
survive rename/edit/restoration/export without enabling new rendering privileges.

Overwrite replaces only the named user override; keep-both reserves unique names
across the batch; skip/cancel leaves saved data intact. Native new/copy/edit/default
and transfer actions use copied settings across successive dialogs, with manager
Save required for persistence. A renamed editor cannot silently overwrite another
existing palette. Export writes exact validated JSON only to the user's chosen
native destination, including disabled choices. Suggested names exclude Windows
path characters and ordinary reserved device names. Errors log exception kind/
HRESULT only, never selected paths or JSON. Closing the owner stops reopening.

517 core tests/100% measured lines and zero-warning build pass.69 desktop cases
are discovered headlessly; six new actual picker/choice/export/draft/cancel cases compile
but have not run. Runtime proof remains pending. Existing CSP, resource denial and
screen/print separation remain in force; original CSS-selector compatibility is
still a required audit.

## Public Help links and post-export actions

Help destinations are fixed application metadata: the Windows repository, its issue
chooser and Discussions. Original Mac/author links appear separately in About; no
authored document path, content or credential enters these URLs. Native browser
launches require deliberate menu/link actions; the renderer capability boundary
is unchanged. Starter setup now requires/readbacks Discussions for support.

Optional PDF/folder opening uses only the destination deliberately chosen in the
owned save picker. The native exporter must succeed and Windows' PDF loader must
read pages before success/actions. Cancellation/failure cannot launch them. The
PDF and folder actions are independent; missing native handlers report an action
failure while retaining the completed export status. Logs retain phase/runtime
or exception kind/HRESULT, never private PDF/document paths or content. New actual
print/PDF/credit acceptance is still pending; the owner's handoff report remains
unresolved despite earlier hosted PDF proof.

## Native highlighted source and linked component credits

Source is committed as literal text in a read-only RichEditBox; only native
character formats are applied. No RTF/HTML source loading or authored URL
attributes are used. Explicit Default input scope disables auto-URL detection.
Parser classification runs off-thread with snapshot/cancellation guards; native
paint batches discard stale snapshot/palette generations. Selected document
accents are contrast-adjusted for shell mode; high contrast uses system colors.
Copy is a deliberate native action and emits only visible original plain text,
including Unicode and original line breaks. Folding never copies hidden bodies.
Clipboard failures report a content-free status; closing cancels background work.
Core spans/copy/contrast are verified; native accessibility/colors/clipboard and
scroll/find/fold/reload acceptance remain pending.

About project/license links are curated from restored package metadata, never
from authored documents. Microsoft binary-package license terms are identified
separately from MIT libraries. Verbatim SDK/WebView/tensor notices are copied as
ordinary application data, independent of the XAML resource index. Native Settings
owns its theme/editor dialogs and pickers; routine checked layout saves preserve
unsubmitted CSS drafts and update reader/save baselines. UI acceptance remains
required for the new settings window and exact distributed artifacts.

## Windows file registration and process activation

Installed Velopack callbacks advertise five fixed file extensions under HKCU.
Commands quote the stable current executable and literal `%1`; no shell script
is involved. Channel/executable-specific identities and command ownership checks
bound uninstall cleanup. REG_NONE `AllowSilentDefaultTakeOver` prevents silent
selection as a default. Extension defaults, UserChoice, HKLM, other registrations
and portable/development-build registration are outside these operations.

The App SDK chooses a primary per executable/profile directory. The activation
pipe permits only the current Windows user; it is an additional native ingress
for literal file paths, never browser content or native command execution. Paths
are canonicalized at the sender. The generated schema rejects unknown fields,
relative/control-containing/null/overlong paths and more than128 paths; frames
are bounded to1MiB, connection/read waits to10seconds and pre-XAML queuing to16
requests. Normal document size/resource/sanitizer boundaries still apply.

Malformed/disconnected requests are rejected and the listener continues; owned
shutdown cancels the pipe. Fixed notifications and failure kind/HRESULT logging
do not expose paths or payloads. UI-dispatched duplicate activation preserves
existing readers, including pending initial loads. Nine actual isolated adapter
tests pass; native app, Explorer and installed lifecycle acceptance is pending.
See ADR-0005 and issue #14. Same-user processes can deliberately request document
opening; this is not an authorization boundary against an already compromised
account. No IPC operation executes arbitrary commands, loads plug-ins or changes
settings, credentials or updater feeds.

The default-file Settings button is a deliberate native user action. It reads the
owned installed registration again before launching a fixed `ms-settings` URI;
the query contains only the fixed channel/executable-derived registered value
name. No document path/content can select the target or invoke this action.
Portable/unregistered previews cannot initiate it. Selection remains in Windows,
with no writes to UserChoice/defaults and `.txt` explicitly optional. Eleven
isolated adapter tests now pass; OS chooser behavior still needs acceptance.


## Reference CSS, Settings drafts and document status

The attributed reference stylesheet is an embedded application resource. Rendered
Markdown remains sanitized before DocumentPage inserts it into #content-inner;
titles are HTML-encoded. Only fixed document classes and bounded code-language
classes survive sanitization. Theme/global CSS uses existing validation and
blocked-resource policies; it cannot close the style element, add native commands
or fetch unauthorized remote/local resources. Reading/print container changes do
not extend renderer message capabilities. Native TOC/source remain separate
controls; full reference selector parity still requires actual acceptance.

Long theme/editor drafts live inside the native Settings window. Back, Cancel and
window closure discard them. Validation gates explicit Save, manager child edits
remain inside its copied draft, and native file pickers/short confirmations retain
Settings ownership. A display-state change never grants document content access
to these native operations.

Footer metadata belongs to the same open handle used for the loaded snapshot.
Watcher notices, failed reloads and bounded metadata polling mark stale content;
only a successful new snapshot clears that indicator. The explicit Open folder
button uses StorageFolder and Launcher.LaunchFolderAsync with a literal document
parent directory; it does not construct shell commands. Diagnostics omit authored
content/paths. The disabled-editor tooltip reports native configuration state,
and its timer and footer polling stop on window closure. Native status/hover/
Explorer acceptance remains pending; passing pure or adapter tests is insufficient.


## Disconnected activation peers

An accepted native pipe connection remains owned until explicitly disconnected,
even when a broken read clears the managed IsConnected property. Each request
retains its current-user boundary, bounded frame/deadline and generated-schema
validation. The deterministic already-closed-peer test uses real pipe handles
and acknowledgements with a controlled continuation schedule: its pre-fix next
connection times out; after the cleanup correction it recovers. Four cases and
25runs/100tests pass locally. Exact hosted published/installed proof remains
required. No protocol, caller privileges or document/native capability expands.
