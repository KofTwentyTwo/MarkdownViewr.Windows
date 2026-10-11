# Session State

Updated: 2026-10-11

## Active work and authority

Build the full Windows counterpart to Darin's markdownViewr using published AppKit
0.1.0. Approved identity: MarkdownViewr / MarkdownViewr.Windows / markdownviewr-windows.
Topic feat/windows-viewer, draft PR #3; PRODUCT.md remains the complete scope.
Owner authorized signed/DCO commits, pushes, repository configuration and ticket updates.
No merges, tags/releases or new Azure attempts. Human review acknowledgement remains
required. Local desktop reservation is unanswered; hosted runner UI tests are authorized.

## Current source synchronization increment — 2026-10-11

Previous pushed b0f3b8638f16d75e4dce5dc6f2cb585fb5214927 is fully verified:
CI 38113042618, CodeQL 38113042570, scripts 38113041887, security 38113042545 and
PR gates 38113042454 SUCCESS. Native job 114392386224 and installed job
114392386003 succeeded; retained TRX proves published 44/44 and stable/dev 43/43,
zero failures/skips, at artifacts/source-anchors-verified-installed-38113042618.
Fixtures remain synthetic/unsigned; all prior producers are terminal.

The current increment implements literal nested source folding and bidirectional
heading-anchor scroll mapping. Canonical snapshots remain immutable; native CR
and folded offsets retain exact find selections. Browser geometry is bounded,
validated and current-page/query guarded; feedback holds preserve explicit find
viewports. Microsoft TextBoxView content coordinates do not receive a duplicate
scroll offset. No new dependency or authored scripts/messages/host objects.
300 core tests/100% measured lines pass at artifacts/source-sync-core. Two new
hosted tests assert actual native/browser heading bounds, physical source scrolling
and nested fold/find/reload restoration: 46 native/published and 45 per installed
channel expected, not yet verified. No local desktop input occurred.

Next: commit/push this reviewed increment, observe its exact producer runs, retain
actual TRX/logs and address failures. Keep #10 open until acceptance and raw-heading
reference audit pass. Full PRODUCT scope remains: broader editor/theme/rendering/
link parity, printing, windows/recents/activation, standards inventory/current-head
human review and exact signed stable/dev artifacts. Published AppKit 0.1.0 remains;
no Azure attempt, merge/tag/release or fabricated human acknowledgement.

## Verified reader and editable themes

77dd04da96b75938a1c1d38c8bebe4851c11a011 CI 38102249889 SUCCESS: build/test,
format, native/UI 26/26, published 26/26, installed stable/dev 25/25 each, no skips.
Four fixture byte gates verified 536 files in both payloads; AppKit probes applied and
restarted within both channels. Fixtures are synthetic and unsigned, not production
signing evidence. CodeQL/security passed. Artifacts: verified-reader-38102249889.

Reader provides picker/Ctrl+O/literal launch paths, sanitized GFM, bounded passive
local images, debounced saves/replacement/deletion recovery and relative scroll.
Native status declaration before WebView2 solved embedded-provider traversal; exact
missing/recovery assertions retained. Initial window fits the monitor's work area.

20 original JSON themes retain Darin's MIT notice. Selection persists separately from
AppKit shell themes; actual dark/light paint checked. 30ca0943c09a8662794c2d400f1b5c49de426942
CI 38102867556 fully SUCCESS, including 29 native/UI tests, editable colors/code font/CSS,
validation/restoration and both installed channels. CodeQL/scripts/security passed.
Later size-17 edit also passed among 29/30 UI tests. Theme import/export/delete and
additional font preferences still need a full reference-parity audit.

## Frontmatter and outstanding diagnostic

3b6ebf83d0d88729887ce7678b0e3bf5cbe85989 implements hidden default, escaped line-based
metadata and sanitized Markdown modes; saved native choices, CRLF/delimiter fidelity.
149 core tests/100% coverage. CI 38103609174 failed accessible-table lookup, 29/30 UI;
CodeQL/security/scripts passed. a141929820694976ab5ab13b2a9a86e687284bf4 adds semantic
caption and row headers. CI 38104015508 now passes table presence but fails table
removal after Markdown selection, still 29/30. Installed producer finished FAILURE at the same
mode-switch assertion; CodeQL 38104015459/scripts/security passed. No run restarted.
New diagnostic keeps the exact assertion and records fixture settings/log/UIA tree.

Issue #5 mitigation is verified: scripts 38104585530 captured InstalledUpdates.Tests.ps1 /
CommandInfo.get_Parameters -> UseCorrectCasing.AnalyzeScript, matching upstream #1708.
Signed ce9afc62378a1275dc723ee444066bd2c732cef5 isolates casing, then checks all other
unchanged rules. Both negative fixtures, 13-file baseline and bounded 20-case probe
pass locally. Actual 443b492 scripts 38104988748 SUCCESS: all 13 files and 55 Pester
tests, no failures/skips; JavaScript passed. No suppression, retry or version change.
PR #3 can close #5 on reviewed merge; upstream itself remains upstream.

## Outline milestone being prepared

Pushed outline head: 5c3988e012b46aff2959eabd16c6de2b7381ab04. Authoritative CI
38104585881 is terminal FAILURE: 31/33 native/UI pass, including actual heading jumps,
independent nested collapse and retained theme/reload state. Depth choices pass; physical
resize fails. Frontmatter still fails because Markdown click never dispatched: settings
remain Metadata, only one reload logged, submenu remains open. CodeQL 38104585849,
security and PR gates passed; scripts 38104585530 captured the race above.

Follow-up adds an explicit Thumb hit-test template. Physical mouse helper moves directly
to the requested control, verifies that exact target or its descendant, then clicks;
an animated path across parent menu rows can close a submenu before the click. Depth
popup is explicitly collapsed before real divider drag and hit identity checked.
Settings/table/resize assertions remain; no local GUI input. Hosted verification needed.

Issue #9: sanitized heading labels, GFM fragment mapping, generated numeric identities,
stack-free section construction, duplicate/nested headings and passive markup.
Native persistent sidebar/depth/drag width and section operations compile. Fixed host
scripts retain independent nested collapse across matching-heading reload/theme changes.
161 core tests pass with 100% line coverage; zero-warning build. Native resize/jump/
collapse/state tests added, not run locally. Observe the next exact hosted handles.

## Latest input acceptance

443b4928eed46641c759012e741a27723e943826 is pushed. CodeQL 38104989139,
security 38104989109, scripts 38104988748 and PR gates 38104989102 SUCCESS;
CI 38104989141 build/format pass, native/UI 31/33. Exact-target pointer verification
passes but Markdown choice stays Metadata, and physical resize stays unchanged.
Installed producer is still being observed; no cancel/restart. Captured UI log:
artifacts/pointer-ui-38104989141.log. Fixture screenshot from the prior run shows
Markdown hover while Metadata remains checked, consistent with input not dispatched.

New follow-up holds mouse-down until the native UI processes it before mouse-up;
real divider drag separately settles start/down/move/up. Outcome assertions and
10-second limits unchanged. Resize failures now capture saved width/rectangle/tree.
Zero-warning build and format pass; no local input. Push after existing producers end,
then observe its exact new handles. Full application scope remains active.

## Next and remaining scope

Finish frontmatter mode-switch diagnosis and hosted outline acceptance; update #4/#6
scoped acceptance and #1/#5/#7/#8/#9 accurately. Continue source pane/synchronization,
find/vim keys, external editor, print state/options, multiple windows/recents and
Windows file associations. Audit syntax-highlighting/local-link/theme parity, standards
inventory and reviewed AppKit API adoption. Verify real signed stable/dev final bytes
before production publication; active Azure profiles do not prove successful signing.

Pinned reference: 6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021 under
AppKit/artifacts/markdownviewr-reference. Preserve original MIT attribution on adaptations.

## Literal source and precise native event follow-up

Latest prior head d59dba2c96d126730f491720fd548c43c30dd97b: CI 38105483808
terminal FAILURE (31/33 loose native/UI); CodeQL 38105483850 and scripts
38105483489 SUCCESS. Mouse down/up settling did not solve the two input cases.
Retained log: artifacts/pointer-states-ui-38105483808.log. No local GUI input.

Issue #10 records the full source/sync/find/keys acceptance. The next implementation
adds a native read-only literal source pane (including hidden frontmatter), persisted
visibility/preferred width, both-side width bounds and the same committed reload snapshot.
This does not complete synchronized headings/scroll/collapse, paired find or vim keys.
The browser's scripts/messages/host objects remain disabled. New actual UI test checks
literal hostile source, read-only state, saved settings, reload and hiding.
166 core tests/100% line coverage, zero-warning build and formatting pass locally.
Hosted acceptance and physical source resize remain required.

Fixture-only MARKDOWNVIEWR_INPUT_TRACE=1 records native pressed/released/capture-lost,
outline drag events and frontmatter clicks without changing routing or logging content.
This tests whether WinUI receives the exact physical events; no third timing workaround
or weakened outcome assertion. The next exact hosted handles must be observed.

## Native events captured and movement correction

9ff820f4773c54573c1d24028ba489328bf2e9c6 CI 38106466328 loose native/UI
finished 31/34: two prior input failures plus SourceText accessibility lookup.
Build/format/scripts/security/CodeQL passed; installed producer now terminal FAILURE.
Retained exact log: artifacts/native-event-source-ui-38106466328.log.

Trace shows Markdown press handled and release unhandled with no click. Outline
DragStarted and DragCompleted occur, but no DragDelta. The pinned FlaUI 5.0.0
Mouse.Position/MoveTo implementation uses SetCursorPos, while Down/Up use SendInput.
Follow-up injects a real absolute MOUSEEVENTF_MOVE through the existing FlaUI Win32
API, with virtual-desktop bounds/normalization and failure checks. Button states,
exact target checks, 70-pixel resize and all saved-mode/width assertions remain.
Entered/exited/outline-moved traces will distinguish the next delivery path.

Declare native source controls before WebView2 while retaining Grid.Column positions,
as the earlier verified StatusText accessibility fix did. Source test now includes a
real 70-pixel source-divider drag; normalize only native line-ending presentation.
Source/outline resize and frontmatter remain unverified until exact hosted results.
All 9ff820f producers are now terminal; the follow-up may be pushed without cancelling them.
Native paired-find research retained in artifacts/native-find-notes.md; full scope active.

## Verified native input/source and paired find increment

4bf0b81b85e99a9f583c5e60b7276e49b423b114 CI 38106991467 completed SUCCESS.
Exact TRX: 34/34 loose native/UI, 34/34 published, stable/dev 33/33 each; no skips.
Real frontmatter switching, outline resize and literal-source/read-only/reload/physical
resize all pass. CodeQL 38106991641/scripts 38106991033/security 38106991494 passed.
Four payload gates verified 536 files; stable 0.1→0.4 and dev beta.1→beta.2 probes
called AppKit apply/restarted within their channels. Retained artifacts:
artifacts/native-move-installed-38106991467, plus native/installed verified job logs.
These synthetic unsigned fixtures remain distinct from production signing proof.

Next increment implements paired find: pure bounded literal source positions/counts,
native WebView2.Find with suppressed default dialog, Ctrl+F toolbar, wrapping controls,
Enter/Shift+Enter/Escape, actual source selection and query/page lifecycle guards.
Search temporarily reveals sections; closing restores prior/explicit choices. Browser
feature accelerators are disabled so the host owns its commands; scripts/messages/host
objects stay disabled. No new dependencies. 179 core tests pass with 100% lines; build
zero warnings and format pass. New hosted tests exercise physical Ctrl+F typing from
browser focus, source provider ranges, native viewport wrap, collapse restoration,
literal hostile-looking terms and reload/empty counts. Physical local-fragment reveal
also added. No local UI input. Observe exact new hosted handles after signed push.

Source heading/scroll/collapse synchronization and vim keys remain issue #10 scope.
Full PRODUCT.md, editor/print/multiwindow/activation/reference/standards/signing work
remain active. Do not claim new find acceptance or completed port before its evidence.

## Find shortcut and fragment acceptance correction

4f4280921cfc87551843a03e4067e6dfe7456ceb CI 38108596329 is terminal FAILURE:
36/38 native/UI and published tests pass. Installed channels were not reached.
Build/test, format, CodeQL 38108596346, scripts 38108595778, security 38108596376
and PR gates pass. Source selections, literal queries, real viewport wrap, reload
and temporary section restoration pass. Ctrl+F from browser focus did not open find;
fragment testing selected the visible sidebar label instead of its hidden browser
heading. Retained logs: artifacts/paired-find-ui-38108596329.log and
artifacts/paired-find-installed-38108596329.log.

Follow-up moves Ctrl+F to the always-loaded root accelerator and keeps the physical
browser-focus key test. Fragment lookup now requires the generated browser heading
ID and name; physical link click/reveal assertions remain. Also correct one ADR
heading's Markdown spacing. Neither correction is verified until hosted acceptance.
Full PRODUCT.md scope remains active; no local desktop input or release publication.

## Owned browser keyboard integration

7d7c712c6365fd6a036c572de3b51b49c6daa1ea CI 38109196556 is terminal FAILURE:
36/38 native/UI and published tests; installed channels not reached. Ctrl+F still
never opens find, despite a loaded root accelerator. Fragment collapse is now
correctly observed, but pointer hit is not the exact link/text child. Keep those
outcome assertions and retain owned-window screenshot, UIA, hit/focus diagnostics.

The follow-up installs WH_GETMESSAGE only on the current app UI thread, requires
the active owned window/child and Ctrl without Shift/Alt/Windows, and consumes
only that find command before browser dispatch. It queues the existing OpenFind,
forwards every other message, and removes the callback on window closure. There
is no global hook, browser bridge or search/document text logging. Host keyboard
acceptance remains pending. Exact logs: artifacts/window-find-ui-38109196556.log
and artifacts/window-find-installed-38109196556.log.

Separate local editor work is tracked by issue #11: literal ProcessStartInfo
ArgumentList, persisted executable selection and native controls. Its 195 core
tests/100% lines pass; that work is not included in this keyboard correction.

## Configured editor increment prepared locally

Issue #11 tracks editor launch and the broader reference list/folder parity audit.
The local increment adds a sanitized persisted executable path, native configure
Save/Cancel and open commands, missing-editor/document status, and one literal
ProcessStartInfo.ArgumentList value with UseShellExecute=false. No arguments or
source content determine executable selection; operational logs omit paths/content.
195 core tests pass with 100% measured lines; final native build has zero warnings
and formatting passes. Three new hosted tests use the existing test-only UpdateProbe
in an environment-selected capture mode; it never reads document contents. They
verify actual process arguments/working directory, saved configuration, invalid
selection/cancel and an editor removed after selection. No local GUI input occurred.

This editor work remains separate from pushed keyboard head 2292603 until its
live CI 38109719180 ends. CodeQL 38109719207 remains under observation;
scripts 38109718804, security 38109719182 and PR gates 38109719194 passed.
Keyboard/fragment and editor hosted acceptance remain unverified; full scope active.

## Browser link provider correction

2292603569d5a78b214933e185842f6cf233010f native/UI job 114382576572
finished 37/38: physical Ctrl+F from browser focus, actual paired source selections,
wraparound, viewport navigation and reload/collapse cases pass. Fragment diagnostics
show pointer (311,128) inside the exact link bounds (266,119,91,19), while UIA
ElementFromPoint returns owned DocumentView rather than its composed hyperlink.
Retained log: artifacts/thread-find-ui-38109719180.log. Installed producer still
requires observation before claiming channel acceptance.

The fixture now requires the pointer inside that exact link rectangle and the hit
provider to be the owned DocumentView or exact link/text. It retains the physical
Down/Up click, hidden generated-heading lookup and revealed heading/body assertions.
Those actual navigation outcomes must pass; this does not substitute bounds or
provider identity for successful link activation. No local GUI input occurred.

Local editor increment signed as 574b57432a11b04d67085cbb0203b757ea85b1f4,
not pushed while the existing producer is live. 195 core tests/100% line coverage,
zero-warning build/format/docs/hooks pass. Headless capture-fixture self-check
preserves one literal Unicode/metacharacter argument; native launch/UI acceptance
still requires the prepared three hosted tests. Full PRODUCT.md scope remains.

## Same-document fragment event correction

9edd982 native job 114383828733 in CI 38110140930 completed 40/41:
all configured-editor launch/validation/missing-file tests and paired-find tests
pass; physical fragment click does not reveal the hidden heading. Microsoft's
navigation contract says same-document fragments do not raise NavigationStarting.
Use SourceChanged for those events and a pure exact-document/query-free/in-range
generated-heading policy. Core now passes 206 tests/100% lines. Authored scripts,
messages and host objects stay disabled; fragments select only bounded heading reveals.

After a valid reveal, remove only this document's fragment using guarded host
history.replaceState so clicking the same link after recollapse can reveal again.
The physical test repeats collapse/link/reveal and retains owned point/bounds checks
and actual heading/body outcomes. Hosted acceptance remains required. Retained log:
artifacts/editor-find-ui-38110140930.log. Existing installed producer is still
being observed; no new push supersedes a live producer. Full product scope active.

## Vim increment prepared after fragment correction

Pushed fragment-event head 5a9af6328f490eda4c67d1ab6aa58a161bab46a5:
CI 38110726381 native/UI job 114385547259 SUCCESS, 41 tests. This includes
same-fragment reveal after recollapse and all configured-editor/paired-find cases.
Build/format, CodeQL 38110726336, scripts 38110725744, security 38110726260
and PR gates 38110726369 passed. Installed producer 114385547073 now SUCCESS;
retained TRX proves published 41/41 and stable/dev 40/40 each, zero skips. Four
payload gates verify 536 files per payload. Evidence is retained in
artifacts/fragment-installed-verified-38110726381 and verified job logs; fixtures
remain synthetic/unsigned and do not prove production signing. Prior 9edd982 is terminal
40/41 native/published; installed channels were not reached.

Local next increment implements j/k, Ctrl+d/u, gg/G, ]]/[[, slash, n/N and Escape;
Windows Ctrl+F owns native find and Ctrl+B reverse-page remains. Core now passes
239 tests/100% lines. Prefixes use injected monotonic milliseconds, expire after
500 ms and reject held-key imitation; lifecycle resets and focus/Alt/Windows/Shift
controls preserve native typing. The existing owned-thread adapter translates only
reading keys using the actual layout without mutating dead-key state, sends bounded
enums and rechecks focus on queued dispatch. No authored script/message bridge.
Three physical-key tests measure real browser viewport/heading/find behavior and
query typing; browser scroll provider lookup is now scoped inside DocumentView so
source scrolling cannot satisfy browser assertions. Hosted acceptance is pending.

No local GUI input, merge/tag/release or Azure attempt. Full PRODUCT.md remains;
source heading/scroll/collapse sync, broader editor/reference/theme/rendering/link
parity, print, windows/activation/recents, standards inventory/review and production
signed stable/dev final bytes remain. Preserve Darin's MIT navigation attribution.

## Vim producer focus evidence and physical fixture correction

Current dfc41a7 / CI 38111842026 native job 114388847556 finished FAILURE:
41/44 pass, zero skips. All existing tests passed; the three new vim cases failed
before their first actual reading command. Opt-in native focus logs report an
empty WinUI focused element or ToggleSection:Button while UIA reports a Pane.
UIA DocumentView.Focus alone did not establish WinUI reading focus. Exact native
log retained at artifacts/vim-ui-38111842026.log. Build/format, CodeQL, scripts,
security and PR gates passed. Installed producer 114388847436 now terminal
FAILURE; retain its log at artifacts/vim-installed-38111842026.log.

The fixture correction performs real MOVE/Down/Up inside the exact owned browser
bounds/provider before virtual-key sequences. All actual viewport, generated
heading, hidden descendant, paired find and native query-typing assertions remain.
No app focus guard is widened, no outcome is substituted and no local desktop
input occurs. The previous producer is terminal before the next push.
Full PRODUCT.md scope and deferred production signing remain unchanged.

## Source provenance foundation after native vim acceptance

6bf125d / CI 38112298176 native job 114390204742 now SUCCESS: 44/44,
zero failures/skips, actual physical reading clicks/keys and viewport/heading/find
outcomes. Duration 2m23s. Build/format, CodeQL 38112298279, scripts 38112297896,
security 38112298300 and PR gates 38112298282 passed. Exact native log retained
at artifacts/vim-focus-ui-38112298176.log; installed job 114390204514 is still
observed before any subsequent push. Previous dfc41a7 published suite timed out
at 180 seconds before final TRX; installed channels were not reached.

The next local increment maps DocumentOutline.SourceHeadings to original UTF-16
source ranges from Markdig AST headings and AngleSharp raw HTML tokens. It handles
Setext, fences, raw tags in quotes/lists/inline text, duplicate labels, native
newlines, Unicode and frontmatter. A fresh internal marker is allowed only during
mapped sanitation and stripped before output; authored markers/IDs stay denied.
Raw line lookup is indexed rather than rescanning all lines for each heading.
254 core tests/100% measured lines and zero-warning build pass. The original
snapshot and public renderer policy remain unchanged. Full scroll/heading/collapse
source UI remains unimplemented, not replaced by this foundation. No GUI input,
Azure attempt, merge/tag/release or claim of production signing.

## Verified vim channels and indexed source line ends

6bf125d / CI 38112298176 is fully SUCCESS. Retained TRX proves native/published
44/44 and installed stable/dev 43/43 each, zero failures/skips. Evidence retained
in artifacts/vim-verified-installed-38112298176, native and installed job logs.
Four payload gates verify 536 files per payload; update probes apply/restart both
channels. Fixtures remain synthetic/unsigned and do not prove production signing.
All exact prior producers are terminal before this source increment's push.

Local signed source-provenance a0d8fe7 has a further tested indexed-line-end fix:
255 core tests/100% lines and zero-warning build/format pass. A 300-heading raw
single-line regression verifies distinct offsets with one shared line end. Global
line ends are indexed once; per-heading lookup is logarithmic. This avoids a
quadratic full-remainder scan for adversarial long raw HTML lines. The source
snapshot remains immutable; no new capability/dependency is introduced. Update
PRODUCT/TODO with actual vim proof; source synchronization UI stays pending and
full scope remains active. No local GUI input, Azure attempt, merge/tag/release.

## Current native newline failure and correction

ddb98ac / CI 38114492408 native job 114396569276 finished FAILURE: 42/46,
zero skips. Existing paired source selections shift by two characters after two
line breaks; actual selections are pha** and *[x] followed by CR. WinUI's text
provider uses one CR per line break, not CRLF. The new model now normalizes display
to CR, subtracts only original CRLF extra characters and retains canonical source
positions. This also corrects heading geometry indices. The fold fixture compares
literal Setext text after newline normalization; all hide/reveal/find/reload and
actual viewport/physical-wheel assertions remain. Exact native log retained at
artifacts/source-sync-ui-38114492408.log. Build/core/format, CodeQL, scripts,
security and PR gates passed; installed producer is still observed before a push.
No local desktop input or weakening of coverage/selection/viewport checks.

## Native CR acceptance and remaining provider scroll

90b5406 / CI 38114958970 native job 114397920477 finished FAILURE: 45/46,
zero skips. Exact source find selections, nested fold/find/reload restoration and
reader-to-source heading alignment now pass. Only the source-to-reader assertion
after an unfocused native ITextRange.ScrollIntoView fails; the log reports no new
source geometry/event after that request. Evidence retained at
artifacts/source-sync-cr-ui-38114958970.log. The fixture now physically focuses
the owned SourceText and verifies its actual middle-heading glyph alignment
before requiring browser alignment. Original viewport bounds and physical wheel
assertions remain; numeric diagnostics also include source offset changes and
previous offsets. This is not yet verified. Observe installed producer
114397920229 before any next push. No local GUI input or Azure attempt.
