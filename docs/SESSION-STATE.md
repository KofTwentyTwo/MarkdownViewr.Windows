# Session State

Updated: 2026-10-11

## Native client caret coordinates - 2026-10-11

Exact91877f7 CI38173447118 focused18/20,2failures,zero skips: outline
now passes; initial hover-fixture color and physical source focus remain red.
Focus probe returns selection2 at verticaloffset5297.759, disproving the
assumption that client hit-testing coordinates start at the visible viewport.

Microsoft's CTextBoxView::GetClientRect reports the full native extent; its
viewport/world-space conversion starts from the scroll offsets. Native caret
range hit-testing therefore needs content-client coordinates. Add the actual
horizontal/vertical scroll offsets to the client inset before GetRangeFromPoint.
Retain identical selection/read-only/physical click/wheel and exact glyph
alignment assertions. Evidence is retained in
artifacts/reader-regressions-38173447118/reader-regressions/reader-regressions.trx;
offscreen rectangles and unshifted client-point methods are both disproven.

Signed local5fa6754 fixes the separately proven foreground propagation ordering;
its local zero-warning Release/format/hooks pass. Preserve live91877f7 full
native/published jobs before pushing the new candidate; do not restart on timeout.
Hosted caret/color and complete installed/release verification remain required.

## Proven native foreground replacement - 2026-10-11

Exact04c1da8 CI38172448726 is terminal:18/20 focused,96/98 native,
94/98 published,zero skips. Published failures retain initial Light colors in
both palette and hover fixtures, nested outline and source-focus alignment.
Real color trace has snapshot1/paint3/tokens6 unchanged: paint complete
#FF75628B at20:37:33.320, after native queue #FF1F1F1F at20:37:33.389.
The hover fixture reproduces the same reset before its pointer step. This
proves native property propagation replaces successfully painted ranges;
classification and generation cancellation do not explain this variant.

Signed91877f7 is now pushed. Its actual native caret/outline acceptance is
running. Prepare the next source fix: after setting native Background/Foreground,
await an owned low-priority dispatcher callback before applying default and
semantic range formats; recheck snapshot/paint/disposal guards after the queue.
This orders the real pending inherited-property work before character formatting,
with no timer, repeated repaint, fake accessibility color or altered assertion.
Local zero-warning Release passes; formatting/hosted exact-color proof remains
required. Preserve the current91877f7 run before pushing the color correction.

Evidence: artifacts/current-native-04c1da8.log and
artifacts/current-installed-evidence-38172448726 retained actual published TRX.
All temporary [DEBUG-...] probes still require removal after diagnosis before
release. Installed stable/dev/Default Apps/Explorer, side-by-side, standards,
Azure/signatures and genuine human current-head review remain open. No release
publication or Azure login was attempted; owner preview55472 is untouched.

## Visible source caret and outline accessibility - 2026-10-11

Signed04c1da8 CI38172448726 focused20 cases:18pass,2fail,zero skips.
Source colors/physical hover pass this variant; full/published colors remain
required because metadata probes may change native propagation timing.

Exact native source-focus probe: offset5297.759, firstY5297.759, selection0
before and after focus. The offscreen first-character rectangle is zero before
adding the offset, so binary searching offscreen rectangles picks character0
and focus reveals the old caret. Replace that search with native
ITextDocument.GetRangeFromPoint at the visible client inset and select its
actual position. Keep original physical click/wheel and -2..36px glyph bounds.

Outline probe retains collapsed DOMindices[1] after child collapse, [0,1]
while parent is collapsed, and [1] after parent expands. The stale child UIA
peer is observed immediately after its sibling appears. Wait for the same
actual hidden child's peer to disappear within the existing10second helper;
retain identical absence checks through theme change and reload. No arbitrary
sleep, hidden-flag simulation, wider geometry bound or test exclusion.

Local zero-warning Release passes for the caret/outline correction. Current
04c1da8 full native/published jobs114567713082/114567712967 are still live;
retain their terminal variants before pushing the next head. Signing, installed
associations/Explorer/lifecycle and side-by-side release verification remain open.
Owner preview55472 is responsive and untouched. Tagged probes remain temporary
and must be removed after diagnosis before release.

## Native diagnostic correction - 2026-10-11

Current dbda6d4 CI38171433458 focused gate is terminal17/20,3failures,
zero skips. Initial Light source palette still becomes#1F1F1F rather than
#75628B after an actual UIA Toggle; the queued theme repaint is not proven.
Nested outline expansion still exposes the collapsed child's native peer.
Physical source-focus alignment still fails, but its failure-report geometry
call throws E_UNEXPECTED and masks the timeout before saving the focus logs.

Preserve the original assertions and bounds. Read native logs/tree before
optional glyph geometry and catch only the observed diagnostic Win32Exception.
Add temporary opt-in [DEBUG-source-colors] before/after actual painting and the
native queue, plus [DEBUG-outline-collapse] generated hidden-section indices
following actual jump/toggle commands. These probes log no document text or
paths and must be removed after diagnosis before release. Native/published
jobs114564687334/114564687101 remain live; do not cancel or restart them just
because job logs are not yet available. Full installed/default/Explorer and
stable/dev lifecycle acceptance remains pending. Owner preview55472 is untouched.

## Installed association acceptance boundary - 2026-10-11

The installed harness now supplies a saved/restored test-only installation
marker after its actual manifest/version/channel/update proof, and explicitly
marks the published copy as portable. The native Default file types case must
find this exact installed executable's expected channel registration and an
enabled Windows chooser action. Previously a missing registration and disabled
button could agree and pass the installed test. Portable evidence must instead
show no registration. This marker does not change application behavior, create
registry keys or simulate successful hooks; actual installed execution is still
required. Windows selection/Explorer/update/uninstall acceptance remains#14.

Prior CI38170170551 is terminal: native93/97 and published93/97,4failures,
zero skips. Published8m56s now completes inside the existing600second bound;
its red tests prevent installed outcomes. No deadline was raised. Current native
and published failures include the reproducible pipe bug, initial source colors,
nested outline expansion and physical source focus alignment. Exact evidence:
artifacts/current-installed-9fdfc2d.log and retained installed-update-results.
Locally signed2fe5853/efe14af prepare the tested IPC correction and queued theme
repaint; next exact pushed head must verify these and the association boundary.

## Previous full-run variants and queued source repaint - 2026-10-11

Exact9fdfc2d full native job114560940480 is terminal93/97,4failures,zero
skips (8m59s), despite focused14/15. Retain artifacts/current-native-9fdfc2d.log:
failures are disconnected peer, nested outline expansion, initial source Light
palette and source focus alignment. Initial-color failure now follows verified
UIA Toggle ThemeLight(before=Off), so it is not explained by mouse fallback.
Both exact highlighting cases passing the focused run do not establish full
acceptance. Installed job114560940485 is still live at last verified read.

Signed/DCO2fe5853 adds the locally proven native IPC cleanup and test-only source
coordinate probe (25runs/100cases,zero failures/skips); it has not yet been
pushed while the prior installed job remains live. A subsequent source change
queues semantic repaint until after native theme propagation, preserving range
classification, selection and read-only state. Existing actual color assertions
remain; no success claim before exact hosted proof. The focused gate also adds
the nested outline variant without excluding any prior case.

## Previous native result and deterministic IPC fix - 2026-10-11

Signed/DCO9fdfc2d is pushed. Exact CI38170170551 focused job114560940302
passes14/15,one failure,zero skips. Source reopen/50:50, both native highlighting
cases, four-file import and physical repeated-heading navigation now pass.
Build/core/format/scripts/security/PR gates pass; full native job114560940480
and installed job114560940485 were still live at the last verified poll.
Retained focused evidence: artifacts/reader-regressions-38170170551 and
artifacts/reader-regressions-9fdfc2d.log. The remaining focused failure is source
heading alignment after a physical click enters source, not the initial jump.

The reproduced published disconnected-peer failure is now deterministic locally.
A test context defers only the real server continuation until the real current-
user pipe client closes. Unfixed next-client ConnectAsync times out10seconds:
artifacts/pipe-closed-before/pipe-closed-before.trx. A broken synchronous read
clears IsConnected while the native connection still needs Disconnect. The fix
records accepted connection ownership and disconnects it even in Broken state;
expected shutdown disposal is handled without swallowing unrelated errors.
Unchanged regression plus three existing cases pass, then25runs/100tests pass
with zero failures/skips: artifacts/pipe-closed-after and
artifacts/activation-stress-after. This is local proof; hosted published and
installed verification remains required. No deadlines or assertions were relaxed.

Next source probe adds only numeric focus/viewport/caret coordinates under the
existing MARKDOWNVIEWR_INPUT_TRACE test opt-in, with [DEBUG-source-coordinates].
It tests double-counted native client coordinates versus caret restoration or
later scrolling feedback. The exact native alignment assertion remains. Remove
this tagged temporary instrumentation after diagnosis and before release.

Live protect-main ruleset24859282 now requires reader-regressions bound to
GitHub Actions15368. Exact before/payload/readback artifacts prove all prior
rules, bindings, review settings, conditions and bypass actors were preserved.
Owner preview55472 is responsive and preserved. Real installed chooser/Explorer,
source alignment, channel isolation, human current-head review and production
signing remain required; no Azure changes or release/signature claims.

## Previous native verification and action correction - 2026-10-11

Exact signed/DCO8abbd4d / CI38168946288 is terminal: full native93/97,
4failures,zero skips; focused10/12,2failures,zero skips. Build/core/format
pass. Retained logs: artifacts/current-native-8abbd4d.log and
artifacts/reader-regressions-8abbd4d.log; actual focused TRX/screenshots/PDFs:
artifacts/reader-regressions-38168946288. The full failure set is source
reopening, repeated heading navigation, four-file import and source focus
alignment. Exact hover passes in both; focused palette fails on initial Light
choice (#75628B expected versus actual#1F1F1F). Do not claim all highlighting
accepted. Installed-updates job114557287398 again exceeded600seconds in the published
suite before any installed outcomes. Published process15 evidence is retained
in artifacts/current-installed-evidence-38168946288. It reproduces the same
source reopen/inset/import/focus failures plus initial hover-color and
intermittent disconnected-pipe timeout; no deadline was raised.

Next bounded correction uses WinUI's real Toggle automation pattern for
checkbox/radio menu items, whose native implementation invokes the same menu
action. Physical source hover/focus assertions remain. Four-file import now
navigates the actual fixture folder before entering all quoted relative names:
the previous native filename field retained only260characters of the expanded
absolute-path batch. Imported/skipped/failed counts and exact exported bytes
remain required. Heading target comparisons include the pinned 1rem scroll
inset, preventing repeated selection of the same heading; its exact native
landing assertion becomes16px plus/minus2, preserving original CSS behavior.
The focused job includes all remaining full-run failures without exclusions.

The required-check setup list includes reader-regressions;55 Pester boundary
tests pass (artifacts/reader-accessibility-script-tests.xml), and both analyzer
partitions pass for the module. Live main ruleset must retain all review,
signature/security rules while adding this GitHub Actions-bound check.
Owner preview55472 remains responsive and preserved; no local physical input,
Azure mutation, human review acknowledgement or release claim.

## Previous release verification - 2026-10-11

Signed/DCO05bf612 is pushed to PR#3. Exact CI38167591076 full native run
completed84/97,13failures,zero skips; previous0ed608e was74/97. Current
focused12-case TRX is retained in artifacts/reader-regressions-38167591076:
Settings panel defaults, toolbar alignment, picker cancellation, custom theme
paint, source theme/mode and exact copying pass. Source-hover passes the full
run but has a focused initial-color timeout before pointer input; retain the
variant instead of calling all highlighting accepted. Build/core/format pass.
Published/installed job114553267448 is terminal: published dotnet tests again
exceeded600seconds before any installed outcomes; retain
artifacts/current-installed-05bf612.log. No deadline was increased.
Exact full-native log: artifacts/current-native-05bf612.log.

Next patch gives the original #content div a named, focusable main landmark so
Chromium exposes the actual inner scroll surface. The prior root reports Scroll
support but no vertical extent, while the unnamed inner container is omitted.
Native menu lookup now selects visible owned peers and dismisses leaf View
menus; UIA Invoke no longer forces reader foreground over Settings/menu owners.
Recent activation defers until the invoking menu finishes restoring focus.

Existing exact font/pixel/print assertions remain: typography fixture requests
28px body because24px produces20→28px glyph heights, exactly40%, rejected by its
strict greater-than40% comparison. The narrow print fixture explicitly centers
its custom body before testing the Full-page margin/width reset. Palette checks
sample the owned empty8px reading gutter rather than a glyph/find mark; no
color expectation is changed. Source color/picker failures retain actual values.
No test is skipped or weakened; actual hosted behavior must prove these changes.

Local zero-warning Release build,586 core tests/100% measured lines pass in
artifacts/reader-accessibility-core. Real pipe stress25runs/75tests passes,
zero skips; the older intermittent disconnect failure did not reproduce, and
no speculative pipe change was made. Formatting passes.
Owner preview55472 stays responsive and preserved. Azure access unavailable;
[azure-signing-admin.md](azure-signing-admin.md) provides exact immutable OIDC,
resource/profile verification and profile-role commands. No signing/release,
review acknowledgement, Azure mutation or merge is claimed.

## Current owner review launch - 2026-10-11

Owner-requested latest self-contained build is open from
artifacts/owner-1.0-review-publish/MarkdownViewr.Windows.exe, PID55472,
with title Review-1.0.md — MarkdownViewr. Separate persistent review profile:
artifacts/owner-1.0-review-profile. Launch metadata is retained in
artifacts/owner-1.0-review-launch.json. Do not overwrite this publish output or
interrupt the owner preview while reviewing. Initial process/title/Responding
readbacks succeeded; no physical desktop input or visual acceptance performed.

Current local gates: zero-warning Release build,582 core tests,100% measured
core line coverage,25 adapter tests and full formatting verification pass.
Default-file registration has five passing isolated registry tests in
artifacts/release-review-adapters/release-review-adapters.trx. Install/update/
uninstall hooks and Settings → Advanced → Choose default file types are
implemented. User chooses defaults in Windows; portable previews do not register.
Actual installed chooser, Explorer double-click and lifecycle acceptance remain
pending under issue #14. Source-hover regression and signed release acceptance
remain open; latest review launch does not establish release readiness.

## Committed implementation and edition follow-up - 2026-10-11

Signed/DCO0eb3a0e and841798c are pushed to feat/windows-viewer/PR#3.
All normal commit hooks passed. Vendor notices retain original bytes through
scoped Git/hygiene attributes; secret/workflow checks remain enabled.
Exact hosted CI38164841448 is live on0eb3a0e. Build/core/format, scripts,
security and CodeQL have passed; native and published/installed acceptance
are still running. The current source-hover case has not yet emitted a verdict.

Subsequent local work implements a distinct installed dev identity and package ID,
with edition-specific title/settings/log variables and edition-aware isolated
smoke fixtures. The new host composition reads the actual Velopack channel before
activation/UI/log services. Core586/586 and100% measured lines, zero-warning
Release, format and55 script tests pass; changed script passes both analyzer
partitions. Separate stable/dev IDs are prepared for the release caller; signing
remains fail-closed until the reviewed/published catalog builder is available.

A distinct gclo-family navy Markdown/reading vector was generated through AppKit
BrandTool; all nine ICO frames16..256 are valid and the256px frame was visually
inspected. The app gradient now matches it. WinUI3 receives a source-project MIT
credit in Components while proprietary binary-package license notices remain in
the audit/distribution inventory. Final icon/credit/edition build has zero warnings/errors;586 core tests and
100% measured lines pass with TRX/Cobertura in artifacts/edition-final-core.
Formatting passes. A focused hosted source-hover job is prepared to produce a
fast actual pointer/color verdict and retained TRX, alongside full acceptance. The owner preview PID55472 still
runs from its preserved earlier output; it has not been replaced.

## Active work and authority

Build the full Windows counterpart to Darin's markdownViewr with published
AppKit 0.1.0. Identity: MarkdownViewr / MarkdownViewr.Windows /
markdownviewr-windows. PRODUCT.md remains the full scope. Topic feat/windows-viewer,
draft PR #3. Owner authorizes signed/DCO commits, pushes, configuration and tickets.
The owner now authorizes completion and stable1.0/dev publication following gclo.
Preserve review/security/signing gates; no fabricated human review or new Azure sign-ins.
Hosted Windows UI tests are authorized; no current local desktop reservation.

## Active 1.0 and dev release work - 2026-10-11

The owner cleared the prior goal and explicitly resumed work to finish and release
stable1.0 and dev versions following gclo. The paused queue below is historical,
not a current stop instruction. Full PRODUCT scope and owner feedback remain required.

Plan: (1) fix owner regressions and UI requirements; (2) pass core/build/style plus
actual current native/published/installed stable/dev acceptance; (3) verify and
configure repository-specific Azure OIDC/signing and gclo-style isolated editions;
(4) complete review/checks, signed commits/push and publish verified releases.

Commit authentication retry succeeded. Commit 841798c is signed and DCO-signed;
it preserves the original four-file native-window correction and passed hooks.
The remaining implementation is being committed after local verification.
Implemented locally: removed toolbar depth duplication, labeled Contents/Print;
explicit Contents/Source new-window defaults independent of local toggles;
open-source-only About view with full license audit catalog preserved; accurate
disabled-editor native tooltip; loaded-handle timestamp/stale/folder footer.
Long theme/editor forms now use native Settings pages, Back and fixed Save/Cancel;
theme management uses adaptive list/detail layout and preserves copied draft loops,
import/export/newer-schema conflicts and validation. First full build passed with
zero warnings/errors. New default-persistence tests and native hover regression
are prepared; source highlighting itself is deliberately not changed before a
real red-capable pointer test runs.

gclo's existing release environment confirms tenant4ebeb0ad-5782-487a-a1b4-5080255774cf,
kof22signing/releases/EastUS and publisher James Maes, but these are reference
configuration, not actual Azure resource verification for this repository.
MarkdownViewr release variables are absent. Current Azure CLI has no login; no
new sign-in initiated. Official desktop-tool initialization reports missing
native pipe; no local GUI fallback/input performed. Hosted testing follows push.

## Historical paused owner review - 2026-10-11

Historical checkpoint only: the owner paused work at this point and subsequently
resumed the full release goal recorded above. The following text records that pause.
Earlier signed-commit/push authorization remains bounded by the pending
1Password-ready reply. The original staged four-file patch remains82+/10-.

New tickets: #26 right footer timestamp/stale indicator/Open folder; #27 source
highlighting disappears permanently after pointer hover (owner report, not yet
reproduced); #28 disabled Open in editor hover explanation; #29 gclo-family icon
redesign. Existing #18 now requires removing only the toolbar H1-H6 dropdown and
consistent Contents/Print icon+text labels. #21 records owner rejection of the
crowded inner modals: propose adaptive Settings list/detail pages, brief native
confirmations, preserved theme/CSS drafts and native pickers. Add explicit new-window
Contents/Source defaults, both off; current-window toggles must not rewrite them.
Issue #24 now calls for actual shipped open-source credits/project/license links only
in Components, with Darin in Contributions and all required notices preserved.

These requirements are queued, not implemented. Source unit/color coverage does
not disprove the owner-reported pointer regression. Do not claim an accepted
toolbar/settings layout or full application completion.

## Reference document CSS checkpoint - 2026-10-11

Before the pause, the document-only stylesheet was ported from Darin's pinned
6d08bc9d27d7d7b2c2fa95bb282da330b8cf9021 template, retaining MIT headers.
Embedded Themes/Document.css and DocumentPage provide #content/#content-inner;
legacy custom variables remain alongside original aliases. Reading width applies
to the inner container after custom CSS. Browser scrolling/navigation/source
coordinates now use #content. Sanitization retains only bounded code-language
and fixed document classes; metadata keeps accessible headers. Print resets
scrolling containers and applies page padding once.

Completed local gates: zero-warning Release build,580/580 core tests,100% measured
core line coverage, formatting and self-contained win-x64 publish. Evidence:
artifacts/reference-css-verified/reference-css-verified.trx and
artifacts/reference-css-verified/e1f564a9-bb6e-4628-8d9a-a2e1fa5c0df6/coverage.cobertura.xml.
Ten new style/page/security regressions pass. Two existing actual-paint UI cases
were adapted to the original document container; neither was run. UI discovery
remains93 cases (69 desktop/24 adapter), expected92 per installed channel.

Review-reference-css.md was launched from artifacts/reference-css-publish using
its separate review profile (launch recorded as119988). Initial startup/document
opening was observed before pause; this is not full visual/native acceptance.
The fixture deliberately forced Source on and does not define production defaults.
No current native/distributed/install/signature or hosted-CI acceptance exists for
this CSS work. Global/per-theme selector parity for native TOC/source, raw HTML/
heading fidelity and actual layout/paint remain open. Update the CSS attribution
inventory and threat model when implementation resumes. Code remains local and
unstaged; no new commit, push, merge, tag, release or Azure sign-in.

## Default-file Settings action - 2026-10-11

Issue #14 follow-up is implemented locally: Settings → Advanced has Choose default
file types and optional-.txt guidance. Only actual installed copies with an
unchanged owned command/RegisteredApplications value enable it. Native click
revalidates ownership. Windows 11 23H2+ opens the exact registeredAppUser page;
earlier versions use general Default Apps and .md instructions. Portable previews
show install/repair guidance. Launch failure gets manual instructions and
content-free diagnostics; defaults are still selected by the Windows user.

Separate-output full Release build passes with zero warnings/errors at
artifacts/default-files-build/bin. Eleven real isolated registry/activation tests
pass (two new route/ownership/default-preservation cases);570 core tests pass and
100% measured core line coverage gate passes. Formatting passes. A native page
acceptance case compiles, not run;93 UI-project cases are discovered only
(69 desktop/24 adapter; expected92 per installed channel). Actual installed
chooser/selection/Explorer and hook lifecycle remain unverified. No owner preview replaced or new local desktop
input, sign-in, commit/push or publication. Original staged four files82+/10-
remain unchanged; the existing signing-authentication reply is still pending.

Historical default-file checkpoint preceded the local document CSS port recorded above.
Full PRODUCT scope remains active, including current native/distributed acceptance,
Print/PDF, reference fidelity, standards and signed production artifacts.

## Windows activation and registration - 2026-10-11

Issue #14 has local implementation: Velopack install/update/uninstall callbacks
advertise md/markdown/mdown/mkd/txt under HKCU with quoted stable current-exe
commands, Default Apps capabilities and a REG_NONE AllowSilentDefaultTakeOver
marker. Channel/executable-specific ProgIDs, unchanged-command checks and owned
cleanup preserve other installs, viewers and user defaults. No real registration
or installer was executed locally; only UUID-subtree registry fixtures were used.

SDK AppInstance chooses a primary per executable/profile. Sender-canonicalized
paths travel through a current-user-only generated-schema pipe to the native UI
queue.128-path/1MiB/10second/16-pending bounds; unknown/null/relative/oversized
payloads rejected; no command protocol. Initial/duplicate/empty/error requests
reuse native reader paths; foreground rights are delegated by the launcher.
Invalid startup notification survives browser initialization. Existing pending
startup path duplication detection is retained.

Actual adapter tests initially8/9: InvalidDataException derives SystemException,
so an oversized frame escaped the original catch and stopped the listener. The
explicit catch fix preserves all assertions; final9/9 pass, including malformed
recovery and idempotent shutdown/rebinding. Red/fixed/final TRXs are retained under
artifacts/activation-boundaries. No diagnostic probes remain.

Fresh full separate-output Release build/publish pass;570 core tests/100% measured
lines, formatting and docs lint pass.90 UI-project cases discovered only
(68 desktop/22 adapter; expected89 per installed channel). Two new real native
secondary-process/relative-Unicode/profile/duplicate/watcher cases compile, not
run. Controlled desktop/distributed/Explorer/installed acceptance remains needed.

Velopack1.2.161 verified the corrected Program.Main Build/Run callback chain and
packed a local unsigned stable fixture in artifacts/activation-package. Both
portable/full payloads retain all543 published files byte-for-byte;11 notice files
match source bytes. Publish evidence and unsigned-only flag are retained. No
installation, actual hook lifecycle, signed-artifact claim or publication occurred.

Owner preview103040 remains Review-toolbar-splash.md using the earlier preview
output/profile; it has not been replaced by activation-build. Original staged
four files82+/10- remain unchanged; new work is unstaged. Existing1Password ready
reply still pending; no signing retry/push/new Azure/merge/tag/release. Full
PRODUCT requirements, native UI, Print/PDF, original CSS/render/activation parity,
standards and exact production signatures remain open.

## Splash and toolbar owner preview - 2026-10-11

Owner screenshot follow-up #18: reduce toolbar height, align TOC/depth/Print
and reading controls, remove the wide gap and group commands. Native CommandBar
now uses a48-DIP row with consistent40-DIP fixed controls and natural left
alignment; Print remains fixed while reading commands retain dynamic overflow.
Actual geometry assertions are prepared; native acceptance is still pending.

New #25: reused the published AppKit branded splash already connected to initial
launch. Settings → Advanced now has gclo-style Show splash/duration controls
(500–30000ms,250ms steps) and startup update checks, moved out of View. Checked
saves update only startup fields, broadcast to all reader baselines and preserve
other preferences/CSS drafts. Existing saved durations remain unchanged.
A real shared-preference/native-controls regression is prepared, not executed.

Fresh separate-output Release build: zero warnings/errors.570 core tests pass,
100% measured lines; format/docs lint pass.79 UI-project cases discovered only
(66 desktop/13 filesystem; expected78 per installed channel). No local input
acceptance ran. Computer Use passive observation was attempted, but its native
pipe was unavailable; no screenshot or input action occurred.

Owner preview opened successfully as Review-toolbar-splash.md — MarkdownViewr,
process103040, with source visible and a5000ms splash in its separate
artifacts/splash-toolbar-profile. Startup update checks are disabled in that
review profile. Other owner windows were preserved. Output:
artifacts/splash-toolbar-review/bin/x64/Release/net10.0-windows10.0.19041.0/
MarkdownViewr.Windows.exe. Evidence: artifacts/splash-toolbar-core/
53b3f3ce-c62e-40f9-afac-cfcf1a2ed905/coverage.cobertura.xml and
artifacts/splash-toolbar-ui-discovery.log. Actual toolbar, Settings and splash
visual/duration/distribution proof remains pending; launch logs prove only
startup/document opening. Signed commits/push await the existing1Password
ready reply; no signing retry, merge, tag, release or Azure change occurred.

## Latest owner review changes - 2026-10-11

Owner feedback is implemented locally: #24 About has Contributions below the
AppKit header/license, with Darin's repository/site/license links. Components
individually links 23 application/platform and 29 development dependencies;
independent tests compare every package/version with actual restored assets.
Eleven distinct packaged SDK/WebView/tensor license/notice files are retained
verbatim and match copied review-output bytes. Microsoft binary license terms
are identified separately from MIT components.

Issue #21 replaces the rejected modal Pivot dialog with a separate resizable WinUI
Settings window: adaptive left NavigationView, native pages/controls and owned
theme/editor dialogs/pickers. Routine width preferences save immediately;
advanced CSS has Apply, and closing discards unsubmitted CSS. Shared saved
appearance refreshes open Settings baselines without overwriting its CSS draft.
Issue #18 Print stays in the fixed toolbar area even when other commands overflow;
Theme uses the native Color glyph rather than Highlight.

Issue #16 is implemented locally: native read-only RichEditBox with literal SetText,
passive parser character spans, contrast-adjusted document accents, Windows
light/dark/high-contrast updates, stale parse/paint guards and exact original
visible source copying with no rich clipboard formats. Native Text/Value
accessibility, find/fold/reload and existing scroll behavior remain test targets.

Latest evidence: full separate-output Release build zero warnings/errors;
570 core tests,100% measured lines;78 UI-project cases discovered only
(65 desktop/13 filesystem, expected77 per installed channel). Native source
color/selection/clipboard and standard Settings acceptance cases compile; no
local desktop automation ran. Initial review startup failures were diagnosed:
unsupported HighContrastChanged registration now uses UISettings color changes;
read-only RichEdit character formatting uses a synchronous writable guard that
restores read-only mode before returning/yielding. The corrected review opened
Review-updated.md successfully, most recently in process 94448. Owner windows
were preserved; source color/clipboard acceptance is still pending. Final native and
distributed acceptance, missing system Print/PDF handoff, original CSS/activation,
standards and production signature proof remain required. Original four staged
corrections are unchanged; signed commit/push still awaits the pending1Password
ready reply. No signing retry, merge, tag, release or Azure changes occurred.

Review output: artifacts/source-highlight-fixed/bin/x64/Release/
net10.0-windows10.0.19041.0/MarkdownViewr.Windows.exe.
Evidence: artifacts/source-highlight-about-core/38ccc305-1af2-4456-9380-a7420b4850f0/
coverage.cobertura.xml, source-highlight-ui-discovery.log,
about-component-inventory.json and review-license-verification.json.
Earlier sections describe historical implementations and acceptance milestones.

## Owner build handoff - 2026-10-11

The owner requested another build to try. A fresh full Release build passed
with zero warnings/errors, then opened README.md in a new responsive process
(PID 107716). The older Debug window showing AGENTS.md (PID 91712) was preserved.
This verifies build and launch only; no automated desktop input or acceptance
ran. Settings, source split, toolbar, attribution/Help and Print/PDF options are
available for owner review. Source highlighting has a compiling core draft but
is not connected to the native source control yet. The previous 538-test/100%
coverage result predates that draft; new highlighting tests remain required.
Signing/push and runtime Print/PDF verification remain pending.

## Owner requests and local implementation - 2026-10-11

Every current owner request is tracked: #16 source syntax highlighting, #17
50/50 draggable source split, #18 Mac toolbar parity, #19 Darin attribution,
plus #20 gclo Help/defaults, #21 dedicated Settings, #22 system print/sizing,
and #23 PDF verification/open actions; #6 holds automatic reload/fallback requirements.
Source highlighting, the reworked Settings window, source split, toolbar and linked About credits are implemented locally; current native acceptance remains pending.

Superseded first #21 implementation: File → Settings with native Appearance, Themes and
Editors tabs. Width/global CSS edit a copied draft; explicit Save merges against
the latest store and broadcasts to open readers. Theme/editor actions retain
separate saves and return to the same parent draft; no overlapping dialogs.
Removed six theme/appearance editing entries from View and moved editor
configuration into Settings. Everyday reading controls remain in View. Native
menu tests now use actual Settings tabs; a new parent/child cancellation case
is prepared. Keyboard entry uses File/Settings access keys.

Locally implemented #17: source opens at50/50 in fresh windows and when reopened,
excluding the outline/dividers. Drag changes proportional widths without the
old600px cap. Left/Right resize the focused divider; Home resets equal columns,
with a visible keyboard focus border. Ratio is window-local, not persisted;
legacy SourceWidth remains readable for older settings.21 layout regressions
cover narrow/large windows, drag direction, both bounds and invalid inputs.
The real physical-resize case now checks actual equal columns, growth, keyboard
reset/resize and reopening rather than the obsolete saved pixel preference.

Latest gates: zero-warning full Release build;538 core tests,100% measured lines;
format verification passes. Headless UI-project discovery finds75 cases
(62 desktop and13 filesystem; installed expectation74). New native Settings/
source/toolbar acceptance has not run. Original staged four-file signing
correction remains unchanged; existing1Password authentication and local GUI
reservation replies are still pending. The older Debug window predates these changes; the owner now has a fresh Release window.
Evidence: artifacts/settings-source-core and toolbar-ui-discovery.log.

Locally implemented #18: native CommandBar preserves the reference order: TOC,
H1–H6 depth, flexible space, source, minus/percentage/plus zoom, enabled theme
picker and editor. Print is in native More overflow. Narrow layouts use the
built-in dynamic overflow; accessible labels/tooltips/toggle state remain tied
to existing handlers. Depth is disabled with hidden TOC; zero existing editors
is disabled, one existing editor launches directly by stable identity, multiple
existing editors offer ordered file/folder choices. Menu/theme broadcasts and
active-document changes refresh toolbar state.

Editor saves now merge against their copied baseline and check persistence
before applying. An explicit save updates each open reader and its saved
baseline; later local saves cannot restore a stale editor list. Four actual
UI cases cover reading/menu synchronization, file/folder/direct editor process
arguments and Print cancel, narrow overflow, and sibling editor launch/stale
source save. These compile/discover only; no local input ran. Build/format pass;
core is unchanged since the measured538-test/100% coverage gate.

Locally implemented #19/#20: AppInfo description/component credit, accessible
About links to the original Mac repository and verified [kelkhoff.com](https://kelkhoff.com/),
README/product/distributed notices. Help matches gclo main198a56213b: repository,
issue chooser and Community support. These use the Windows repository, never
Darin's support destinations. App/starter Discussions enabled and read back true.
AppKit#28 and windows-app-template#3 track shared defaults in isolated worktrees:
AppKit/artifacts/help-menu (feat/help-menu from main839fb72) and
AppKit/artifacts/starter-help-menu (feat/help-menu from onboarding758e773).
Both WinUI/WPF samples and starter use existing APIs; no package version changes.

Print options now use a responsive wider layout and opt-in Open PDF/Open folder
checkboxes. PDF export loads the generated file through Windows' PDF API and
requires pages before reporting success or starting either native action. Handler
failures remain independent and do not misreport successful creation. Print-phase
logging distinguishes preparation/selection/export and records runtime version
without private paths/content. The missing system-dialog root cause remains
unproven; earlier hosted success does not resolve the owner's report. #22/#23
remain open. GUI reproduction reservation and PDF-outcome questions are pending;
no desktop input ran. The owner is still using the earlier Debug build; these
changes compile in separate Release output and are not in the running process.

Live-reload native filesystem tests pass6/6 without an app/desktop launch. A failed
refresh now explicitly shows the retained previous snapshot and File → Reload.
A real prolonged-lock/automatic-recovery acceptance case compiles but has not run.
Current UI count becomes70 native/published and69 per installed channel; all
new actual About/print/fallback assertions still require execution.

App517 core tests/100% measured lines; Release full build zero warnings/errors.
Shared default builds pass; AppKit115 unit tests with100% core/updates lines,
starter20 core +14 CLI tests/100% core lines,56 Pester tests and changed-script
all-rule two-pass analysis pass. All three formats and the final app build pass; nine-doc lint passes.
Fresh headless discovery confirms70 cases; no actual desktop acceptance ran. The actual packed
starter generated both changed-owner CLI and GUI-only apps with correct Help URLs;
locked restore/build and20 core tests pass for each, plus14 CLI tests in the CLI
variant. No generated desktop UI was run. Evidence lives in app artifacts/
owner-followups-core and starter `artifacts/help-template-*` / `help-generated-*`.

Original four staged corrections remain unchanged (82 additions/10 deletions).
All new changes remain uncommitted/unpushed while the existing1Password signing
question awaits authentication. Do not bypass signed/DCO commits or rerun signing
until the owner confirms readiness. Azure remains deferred; no release/merge.

## Requested local launch - 2026-10-11

Owner requested a build and interactive launch. The full solution rebuilt with
zero warnings/errors; the Debug x64 executable opened a responsive native
MarkdownViewr window (PID91712). No automated mouse/keyboard tests ran.
This launch does not provide new feature or distributed-artifact acceptance.

## Current local renderer/theme milestones and signing dependency

Pushed head remains 1cfc07f. Four window-test corrections remain staged and
validated; 1Password signing authentication is still required after two failed
attempts. No bypass, corrected commit/push or fresh hosted result exists.

Independent unstaged work adds four enabled-by-default Markdown extensions and
native View toggles using the existing Markdig parser. Immutable options are
captured before asynchronous reads; separate saved fields merge sibling changes
individually. Passive mark/ins markup passes the existing sanitizer; selected
palette styles apply to highlights/underlines. Parser AST normalization preserves
literal code/newline boundaries and reference no-whitespace caret/tilde semantics
without rewriting original UTF-16 source positions.

All native typography controls now expose body/heading/code font families,
body/code and h1–h6 sizes, and line height. ReaderTheme.Edit creates a copied JSON
schema, preserves unrelated metadata, rejects unknown fields/nonfinite or invalid
sizes, and applies the existing font/CSS validation. Invalid drafts and cancellation
leave originals unchanged.18 focused theme cases pass. Existing automation IDs
remain; a new hosted-only case saves all fields, requires real heading/body/code
glyph growth and verifies cancellation preserves the accepted theme. It compiles,
not executed. Actual new desktop acceptance remains required.

Reader-wide appearance now provides native limit/width/global-CSS drafts. Defaults
match the reference (enabled,1000px); stored corruption is repaired, invalid drafts
remain open, and cancellation preserves settings. Global CSS precedes per-theme
CSS, with enabled width applied last and scaled by reading zoom. Screen-only styles
cannot change print layout. Saves merge against the latest store and broadcast to
all open readers; revision guards supersede older asynchronous updates and update
sibling save baselines.22 focused regressions cover bounds/defaults, CSS cascade,
HTML embedding, separate print output, source-generated JSON and field merging.

Theme management now provides a copied native list for ordering, enable/disable,
delete and restoration. Enabled choices alone populate reading menus/cycling;
Ctrl+Shift+Down/Up requires owned reading focus. Deleting a built-in removes its
user override and records a restorable tombstone. Reference restoration keeps
edited built-ins as uniquely named custom copies, updating saved selection/order,
then restores/enables originals. Theme commits merge unchanged fields from the
latest store and broadcast catalog/selection to open readers with save baselines
and asynchronous revision guards.23 focused cases cover order, wrapping, empty
lists, corruption, copied arrays, deletion, restoration collisions/metadata,
JSON defaults and sibling merges. No dependency/version changes.

Theme transfer now uses owned multiple-file JSON selection, serial bounded reads,
per-conflict overwrite/keep-both/skip and explicit compatible newer-schema consent.
Known schema/font/color/size/CSS/depth checks remain enforced. Strict Parse still
rejects newer versions; compatible saved/import parsing retains their original
version/metadata. Unconfirmed newer imports cannot be persisted. Export includes
disabled choices and writes exact validated JSON to the user's destination.
Invalid files do not prevent other batch entries from importing. Errors log only
exception kind/HRESULT, without paths/content. Manager New/Copy/Edit/Use selected
and import/export finish their secondary dialogs before reopening the copied
manager draft; only Save themes commits it. Rename collisions stay in the editor,
and owner closure stops reopening. No dependencies or workflow changes.

Local517 core tests/100% measured lines, zero-warning full solution build,
format and four-doc lint pass.
Latest coverage: artifacts/theme-transfer-core;46 focused transfer regressions
include all20 actual bundled JSON round-trips, bounds/read position, BOM/Unicode,
invalid input/cancellation, schema consent/security, conflict/version/metadata
retention, copied drafts and safe Windows filename suggestions. Earlier extension/
typography/appearance/list checkpoints retain32/18/22/23 focused cases.
Headless discovery (correct AnyCPU test-project mapping) finds69 UI cases; evidence:
artifacts/theme-transfer-ui-discovery.log. No app/desktop input was executed.
Twelve new runtime cases compile but have not run: six prior renderer/typography/
appearance/list cases plus six transfer/cross-dialog/cancel cases. Existing restoration
also requires retained override JSON/paint and explicit original selection.
Original staged window correction alone expects57 native/published and56 per
installed channel. All independent local milestones require69 native/published
and68 per installed channel. No new desktop/PDF/current-head hosted proof exists.
Remaining CSS-variable/DOM-selector fidelity, raw-HTML extension/source-heading
edges, local link/render behavior, Windows activation and standards/signatures
are not complete.

## Printing baseline verified; expanded geometry investigation

Native File/Print/Ctrl+P, copied themes/layout preferences and explicit native PDF
saving are implemented. Core363/100% measured lines;55 scripts; build/format pass.
Signedec5f98f / filtered38125751638 passes all3 original cases, zero failures/skips:
keyboard draft cancel, actual Windows print UI cancel and real default/themed PDFs.
Actual pixels prove text, expanded folded image, white ink-saving page, hidden
images and active-theme background. Exact actual glyph/view rectangles prove
unchanged nonzero reader position/zoom, source text and collapsed content after
both exports. Evidence: artifacts/print-exact-filename-38125751638.

Hosted probes proved41pixel PDF reading movement. SavePdf restores validated
vertical position in finally only for the same ready navigation; no completion is
guessed for void native print UI. Filename typing waits for exact full native
readback. Stale UIA200% values are replaced by actual glyph/view assertions.
Cleaned300e145 / CI38126078284 fully passes: native/published52 each,
installed stable/dev51 each, zero failures/skips. Both AppKit apply/restart probes
reach expected versions without check/apply errors; fixtures are unsigned.
Retained native log and installed evidence: artifacts/print-clean-native-38126078284.log
and artifacts/print-clean-verified-installed-38126078284 (six real PDFs).

Two additional native-output cases now compile/format locally: first printed
heading glyph ratios for standard/matching zoom, real40px padding shift and paired
find/source selection/screen paint after each PDF; saved user-theme240px block
width versus full page and actual optional border rows. New helper edits actual
native options, including padding NumberBox; all generated PDF/PNG bytes retained.
Signedd0a225d / CI38127089763 finishes native/published53/54, zero skips.
Actual saved custom-width/border/full-page case passes. Font/padding case fails
before PDF output at a requested zoom wait; installed channels are not
reached. Exact expected/actual glyph rectangles and native state are now retained
on failure. Diagnostic6e17674 proves the second step: initial35px,121% actual44px,
expected42.35px from multiplying an already-rounded UIA text box. The new PDF
case now requires actual increasing screen glyphs and completed native percentage,
plus bounded final screen growth; precise standard/matched font ratios remain
asserted against real PDF glyph bands. No PDF/state assertion is removed.
Retained red log and published TRX:
artifacts/print-geometry-native-38127089763.log and
artifacts/print-geometry-installed-38127089763. Corrected head requires54
native/published and53 per installed channel. No local GUI input or new dependency.
Correctedbf4fcdc / CI38128311242 fully passes: native/published54 each,
installed stable/dev53 each, zero failures/skips. All5 Print_ cases pass in every
build, including actual PDF font/padding/paired-find and original cancellations/
image/background/nonzero-state cases. Retained24 PDFs/48 PNGs/TRXs:
artifacts/print-font-verified-installed-38128311242; native log:
artifacts/print-font-native-38128311242.log. Actual before.AppKitApplyCalled and
restarted=true in both channels; expected stable0.4.0/dev0.3.0-beta.2; all before/
after check/apply errors null. Fixtures unsigned; production signing not proved.
ADR-0003 remains proposed for maintainer review.

Live settings validate. Official conformance34PASS/1FAIL/1WARN/3REVIEW: required
standards inventory lacks app, confirmed live. Exact reviewed addition prepared,
not applied/merged; see docs/standards-readiness.md. Actual signing remains deferred.

## Native windows and recents milestone

New window-local initial paths replace rereading process arguments. Track native
XAML windows on one UI thread; independent watcher/browser/hooks/zoom; Ctrl+N/Ctrl+W
and Exit; shared log closes only after the last reader. Modern App SDK multiple-file
picker includes md/markdown/mdown/mkd/txt. Private recent history20, successful opens
only, duplicate focus, missing errors and clear. Source-generated delta merging
protects sibling settings and cleared history from stale window snapshots.

Signed/DCOe48a08c implements this milestone. Local376 core tests/100% lines and
zero-warning build/format/docs/hooks pass. Three new hosted-only
native lifecycle cases compile; no local GUI input. ADR-0004 and threat model
updated. 1cfc07f / CI38129552334 native54/57, zero skips: all original54 pass,
three new cases fail. First caches a glyph across zoom navigation; second requests
unsupported Invoke from SourceToggle after actual New/Recent/Close/missing/clear
steps; third cannot find Open picker's filename field. Fresh glyph lookup and
verified owned physical menu input retain all state/reload/history assertions.
Picker diagnostics now retain owned tree and generic exception/HRESULT; no selector
change is guessed. Actual native foreground focus and source preference persistence are required.
The missing-file window loaded nonempty history before Clear; its later preference
save must not resurrect that snapshot.
Retain native red log: artifacts/windows-first-native-38129552334.log. Published process timed out at300seconds after two recorded new-case failures;
no TRX/installed-channel outcomes. Retained partial output/screenshots:
artifacts/windows-first-failed-installed-38129552334. Outer full-UI-suite budget
now600seconds; all individual10/30second waits/assertions unchanged. Association/activation registration remains
required. Full reference audit also retains theme ordering/enable/delete/cycle,
JSON import/export conflicts, all typography/global CSS/content width and four
configurable rendering extensions in PRODUCT/TODO; none is claimed complete.

## Current outcome

Signed 690be07 fixes repeated outline activation. Native ItemClick supplies the
TextBlock content, not its ListViewItem container. Both now carry the bounded
numeric heading tag; activation accepts tagged FrameworkElement content while
SelectionChanged retains keyboard navigation. No title lookup or source mapping change.

The original source regression passed on 690be07: source-diagnostics38121389088,
job114416668837, including source focus preservation, actual source/reader middle
glyphs, repeated last-heading activation and the final physical-wheel assertion.
TRX retained under artifacts/source-clicked-content-verified-38121389088.
Temporary workflow, tagged probes and source geometry logs are removed.
Cleaned2639e2d fullCI38121762124 passed: native49/49,published49/49, installed
stable/dev48/48, zero failures/skips. All other producers pass. Evidence retained
under artifacts/source-clean-verified-installed-38121762124. New print acceptance remains pending.

Font zoom and named editors are implemented. Prior016c745 native/published48/49,
zero skips; only repeat navigation failed. Zoom actual glyph/menu/toolbar/Ctrl/
reset/reload and named-editor cases pass. Core363 tests/100% measured lines,
script boundary55 tests; zero-warning build and formatting verified.

## Verified baseline and evidence

Previous fully accepted b0f3b86/CI38113042618: published44/44, installed stable/dev
43/43, zero failures/skips. Actual AppKit apply/restart uses synthetic unsigned
fixtures; it does not prove production signing. Published predecessor failure
TRX: artifacts/source-physical-reactivation-failed-installed-38120651378.
Red clicked-content trace/TRX: artifacts/source-activation-boundary-38121148429.
Superseded diagnostic jobs are not fixed-head acceptance proof.

## Next steps

1. Require native/published57 and installed stable/dev56 on the window milestone; retain actual lifecycle/picker/update evidence.
2. Complete source raw-heading audit and broader zoom/theme/find/collapse proof.
3. Complete print #12 output/state acceptance, windows/recents/activation and remaining theme/render/link parity.
4. Finish standards inventory; exact signed stable/dev artifacts remain deferred.

Update TODO and tickets with measured results; keep the PR draft until reviewed.
Current exact commit/job IDs are in ignored artifacts/CURRENT-CHECKPOINT.json.
Historical details: [reader/source notes](history/reader-source-session-2026-10-11.md).


## Exact current native failure evidence - 2026-10-11

CI38164841448 is terminal on0eb3a0e:72/97 native cases pass,25 fail,zero skips.
Build/core/format/scripts/security/CodeQL pass. Published acceptance exceeds the
600-second process budget and is terminated before installed-channel outcomes;
this is not installed acceptance. Native/installed logs and artifacts are retained
under artifacts/current-native-0eb3a0e.log, current-installed-0eb3a0e.log and
current-installed-evidence-38164841448. The actual native suite took11m42s with
25 failures; investigate failures before making a measured process-budget change.

Source hover is reproduced exactly: initial heading color#75628B becomes#000000
at the first post-pointer assertion (SourceHighlighting.cs line37, before focus).
Ranked hypotheses shared: native whole-document hover foreground, focus/selection
reset, asynchronous repaint cancellation. Pinned WinUI generic.xaml confirms
PointerOver/Focused/Disabled storyboards target ContentElement.Foreground. The
first candidate removes only those foreground animations on template application,
retaining native chrome and semantic range formatting. It requires the focused
hosted regression before being called fixed; no local GUI input performed.

Other failures include document scroller/paint/typography/source geometry, native
picker filename discovery, print-width output, new-window outline assertion,
recent-window foreground and one pipe disconnect recovery case. Full scope and
assertions remain; no failures skipped or weakened. The next focused hover job
retains a real TRX alongside the full suite.

Actual GitHub OIDC readback uses immutable IDs:
repo:KofTwentyTwo@47367449/MarkdownViewr.Windows@1413777106:environment:release.
A name-only copied trust is invalid here. Administrator runbook reads actual
account/profile/subject via stable ARM2025-10-13, retains existing identities,
assigns profile-scoped signer permissions and documents required actual signature
proof. Azure authentication remains unavailable; no new sign-in or cloud mutation.
