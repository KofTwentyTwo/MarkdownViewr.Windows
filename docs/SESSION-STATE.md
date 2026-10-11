# Session State

Updated: 2026-10-10

## Active work and authority

Build the full Windows counterpart to Darin's markdownViewr using published AppKit
0.1.0. Approved identity: MarkdownViewr / MarkdownViewr.Windows / markdownviewr-windows.
Topic feat/windows-viewer, draft PR #3; PRODUCT.md remains the complete scope.
Owner authorized signed/DCO commits, pushes, repository configuration and ticket updates.
No merges, tags/releases or new Azure attempts. Human review acknowledgement remains
required. Local desktop reservation is unanswered; hosted runner UI tests are authorized.

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
