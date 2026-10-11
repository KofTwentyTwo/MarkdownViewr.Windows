# Development Tasks

- [x] Review all eight Mac screenshots and record verified Settings gaps in issue #21 without private content.
- [ ] Implement/accept the native Windows Settings organization, General default-viewer status, Contents options, theme previews/defaults and Printing page.
- [x] Reproduce/correct multiline inline raw-heading fold loss;592 core tests/100% measured lines and zero-warning Release pass.
- [ ] Verify the updated inline raw-heading runtime fixture and complete distributed acceptance on its exact pushed head.

- [x] Exact6374611: native97/98 and published97/98, focused19/20, zero skips; actual sustained colors/hover/focus and outline pass.
- [x] Exact9994eda: offscreen native geometry correction passes focused20/20 and full native98/98; original focus/both-direction glyph/wheel assertions unchanged.
- [x] Exact9994eda: published98/98 and installed stable/dev97/97 each, real update/apply/restart and target hashes pass; zero skips.
- [x] Exact764c7ac: retain focused20/21, native98/99 and published98/99; only the new raw multiline-heading fold fails, zero skips. Installed lifecycle phase is not reached.
- [x] Reproduce multiline raw-header loss in core tests and correct parser token end spans;589 core/100% measured lines, zero-warning Release/format pass.
- [x] Exactbd3d72a: raw-HTML correction passes focused21/21 and native99/99; retain published process log with two real initial print-fixture failures and600-second outer timeout/no TRX.
- [ ] Diagnose actual published print startup failures using owned logs/tree/screenshots and per-case progress; preserve deadlines/assertions.
- [ ] Verify full published/installed suites and new registration lifecycle/coexistence readbacks on the next exact head.

- [x] Generate and publish the GUI-only AppKit application with the approved identity.
- [x] Configure/read back repository protections and establish the initial hosted CI baseline.
- [x] Inspect pinned markdownViewr source and record the full parity scope in PRODUCT.md.
- [x] Implement GFM parsing, passive HTML sanitation and the isolated resource policy (ADR-0002).
- [x] Replace the greeting/crash controls with file opening, rendered Markdown and manual reload.
- [x] Test physical image containment, including junction escapes, and input size limits.
- [x] Update the threat model, renderer attributions and shipped library notices.
- [x] Verify first-reader hosted browser acceptance: 13 native/UI tests passed on f1756d2.
- [x] Verify live reload/browser network scenarios and retain actual theme screenshots on head 77dd04d.
- [ ] Discuss the desired first release and Windows-specific behavior with the owner.
- [x] Implement/test directory watching and validated relative viewport state for saves/replacements/deletion recovery.
- [x] Complete actual browser save/lock/replacement/deletion/scroll acceptance (issue #6): hosted native/UI 26/26; published 26/26; installed stable/dev 25/25 each.
- [x] Port Darin's 20 built-in themes and persistent document selection; validate schema, font escaping and invariant CSS.
- [x] Verify hosted built-in theme selection and actual painted colors on 77dd04d.
- [x] Implement persisted editable theme overrides/custom names, color/code-font/CSS controls and built-in restoration.
- [x] Complete hosted theme editor/validation/restoration acceptance (issue #7): CI 38102867556 passed; size 17 edit also passes in 29/30 later UI tests.
- [x] Implement persistent resizable TOC/depth, safe heading/local-fragment jumps and independent nested section collapse (issue #9).
- [x] Verify hosted heading jumps, independent nested collapse and theme/reload state on 5c3988e (31/33 UI pass).
- [x] Verify physical sidebar/source resize and frontmatter modes: CI 38106991467, 34 native/published and 33 per installed channel, no skips.
- [x] Implement literal read-only source pane, saved visibility/width and shared committed reload snapshots (issue #10).
- [x] Verify source visibility/read-only/literal/reload and physical resize in native, published and both installed builds on 4bf0b81.
- [x] Implement paired native browser/source find with bounded literal terms, shared counts, wraparound and reload guards (issue #10).
- [x] Verify paired find and repeated local-fragment reveal on 5a9af63: 41 native/published and 40 per installed channel, zero skips.
- [x] Implement all required vim reading/find/heading commands, timed prefixes and native focus/modifier guards (issue #10).
- [x] Verify physical vim viewport, collapsed-heading and query-typing acceptance on 6bf125d: CI 38112298176, 44 native/published and 43 per installed channel, zero skips.
- [x] Map sanitized surviving headings to original parser-derived source offsets (Setext/fences/raw HTML/frontmatter/duplicates); source UI synchronization remains pending.
- [x] Implement literal nested source folding and bidirectional heading-based scroll synchronization (issue #10), preserving canonical find/reload snapshots.
- [x] Verify actual native/browser heading positions, physical source scrolling and fold/find/reload restoration: `2639e2d` / CI38121762124, published49/49 and installed stable/dev48/48, no skips.
- [ ] Audit find/source collapse parity against the pinned reference, including multiline and same-line raw HTML headings (issue #10).
- [x] Implement and core-test hidden/metadata/Markdown frontmatter modes, with persisted native controls and attribution.
- [x] Verify actual code-font size 17 editing in hosted UI runs.
- [x] Verify actual hosted frontmatter mode switching on 4bf0b81 (including both installed channels).
- [x] Implement bounded persisted editor selection and deliberate native launch using one literal process argument (issue #11).
- [x] Verify actual editor fixture launch, validation/cancel and missing-executable acceptance on 5a9af63 in native/published/installed builds.
- [x] Audit and implement named editor lists, stable migration, selection/reordering/removal, cancel-safe drafts and folder-opening parity (issue #11).
- [x] Verify actual editor-list/menu order, file/folder probe arguments, cancellation and final removal: `2639e2d` native/published/installed stable/dev, no skips.
- [x] Implement native menu/toolbar per-window font zoom, 30–500% bounds and guarded Ctrl+plus/minus/0 commands (issue #13); 330 core tests/100% measured lines.
- [ ] Verify actual zoom glyph/key/reload/theme/find/collapse behavior in native, published and both installed channels.
- [x] Implement print-only themes/options, native Windows print UI and explicit PDF save (issue #12); core363/100% lines; no new renderer dependency.
- [x] Verify original3 focused native print cases on ec5f98f/38125751638: both cancels, real default/themed PDF pixels and unchanged actual glyph/view/source/collapse state.
- [x] Implement actual PDF font/padding/width/border comparisons and successful-export paired-find preservation; d0a225d native/published width/borders pass, zoom wait fails before font/padding output. Diagnostic6e17674 confirms rounded UIA metrics; corrected case measures precise font ratios in actual PDFs.
- [x] Verify clean original3 print cases in full300e145: native/published52 each, installed stable/dev51 each, zero failures/skips; real PDFs and update apply/restart retained.
- [x] Verify corrected native54/54 on bf4fcdc / CI38128311242: actual PDF font/padding/paired-find plus original cases, zero failures/skips.
- [x] Verify all5 print cases in full bf4fcdc / CI38128311242: native/published54, installed stable/dev53 each, zero failures/skips;24 actual PDFs/48 PNGs/TRXs retained, update apply/restart passes both channels.
- [x] Implement native independent document windows, multiple-file picker/all reference extensions and bounded private recent files/clear; source-generated sibling-safe preference merging. Local376 core/100% lines and zero-warning build pass.
- [ ] Verify actual hosted Ctrl+N/Ctrl+W, two-window zoom/watching, actual multiple-file picker/cancel, recent duplicate/missing/clear/stale-save and File/Exit acceptance.
- [x] Implement Windows file registrations and cross-process activation with channel/install/profile identities, quoted commands and preserved defaults;9 actual isolated adapter tests pass.
- [ ] Verify Explorer Open With, installed registration/update/uninstall and actual secondary-app/profile/duplicate/watcher behavior in all distributed builds (issue #14).
- [x] #14: Implement Settings → Advanced → Choose default file types. Read-only install ownership checks target this exact registered application on Windows 11 23H2+; older versions open general Default Apps. Portable previews show installation guidance; `.txt` remains optional. Eleven isolated registration/activation tests pass.
- [ ] #14: Verify the actual installed Settings action, Windows Default Apps selection and Explorer double-click for Markdown types; confirm optional `.txt` remains unchanged, stable/dev identities and older-Windows guidance. New native page case compiles, not yet run.
- [x] Expose all body/heading/code typography controls, including heading levels1–6 and line height, with copied core edit validation.18 focused theme regressions; combined426 core tests/100% lines, zero-warning full build/format pass. Unsigned worktree only.
- [ ] Verify real hosted complete typography save/heading-body-code glyph growth/cancel acceptance; new case compiles, not run.
- [x] Implement shared global CSS and bounded persisted content width, copied cancel-safe drafts, sibling broadcasts and separate print styles.22 focused regressions; combined448 core/100% measured lines, zero-warning build/format pass. Unsigned worktree only.
- [ ] Verify real hosted width wrapping/global CSS/theme changes/PDF isolation/cancel and two-reader broadcast/stale local save; two new cases compile, not run.
- [x] Implement copied native theme order/enable/delete/restore and shared selection, saved menu order and guarded Ctrl+Shift+Up/Down cycling. Restoration preserves edits as collision-safe custom copies.23 focused cases; combined471 core tests/100% measured lines, zero-warning build/format pass; local worktree only.
- [ ] Verify actual hosted menu order/disabled choices/physical cycle/focus/cancel, sibling deletion/restoration and updated retained-override restoration case. Two new cases compile, not run.
- [x] Implement bounded multi-file JSON import, explicit overwrite/keep-both/skip/newer-schema choices, metadata/version-preserving disabled-theme export and native manager New/Copy/Edit/Use selected with copied cross-dialog drafts.46 focused regressions; combined517 core/100% measured lines and zero-warning full build pass; local worktree only.
- [ ] Verify all six new hosted conflict/batch/future/disabled-export/new-copy-edit-cancel cases in native, published and installed builds; headless discovery finds69 total cases, runtime not executed.
- [ ] Audit original global/per-theme CSS variable/DOM selector compatibility; retain full reference stylesheet behavior.
- [x] Implement independent saved highlight/superscript/subscript/underline options and native controls; existing parser/sanitizer, exact source offsets, code/newline/whitespace guards and theme palette. Local408 core tests/100% lines and full build/format pass; unsigned worktree only.
- [ ] Verify hosted native extension glyphs/paint/toggles/persistence/source/reload in all distributed builds; one real acceptance case is prepared, not run.
- [ ] Audit remaining extension edge cases against raw authored HTML and complete local image/link/render fidelity.
- [x] Verify the all-rule two-pass PSScriptAnalyzer mitigation on scripts 38104988748 and 38105483489; closes #5 only on reviewed merge.
- [ ] Verify hosted checks on each actual implementation head.
- [ ] Finish standards inventory and reviewed/published AppKit/signing API adoption when available.
- [ ] Verify actual signed stable/dev installers and exact shipped bytes before release publication.

## Native and IPC follow-up - 2026-10-11

- [x] Exact9fdfc2d focused native acceptance14/15,zero skips: source reopening,
  highlighting, four-file import and repeated-heading navigation now pass.
- [x] Reproduce the disconnected-peer failure deterministically and correct
  owned native cleanup: actual pre-fix10-second connection timeout, then four
  cases pass and25runs/100tests pass after fixing Broken-state disconnection.
- [ ] Verify current full/published/installed results and the IPC correction on
  the exact next pushed head; do not substitute local stress for distributed proof.
- [ ] Diagnose source focus/scroll alignment using the retained numeric probe,
  preserve exact native assertions, and remove [DEBUG-source-coordinates]
  instrumentation before release.
- [x] Add/read back the real reader-regressions required check on live main,
  preserving every previous protection and GitHub Actions binding.

## Previous acceptance checkpoint - 2026-10-11

- [x] Exact8abbd4d hosted build/core/format pass; full native93/97,
  focused10/12,zero skips. Retained native/published and real PDF artifacts
  are documented in SESSION-STATE.md.
- [ ] Verify the next native Toggle/menu, four-file picker and heading-inset
  correction in all distributed builds; retain exact source-color and source
  focus/scroll failures. The focused job includes every remaining native case.
- [ ] Complete current published and installed stable/dev acceptance;8abbd4d
  again exceeds600seconds during published tests before installed outcomes.
  Investigate the reproduced disconnected-pipe timeout; local75-case stress
  did not reproduce it. Preserve owner preview55472 and all signing gates.

## Owner follow-ups - 2026-10-11

- [ ] #22: Fix the system print handoff and widen print options.
- [ ] #23: Diagnose PDF export and offer success-only open PDF/folder actions. Readable-output verification and options implemented locally; runtime acceptance pending.
- [ ] #19: Credit Darin Kelkhoff's original Mac app in About and documentation, with repository/site links. Implemented locally; actual About acceptance pending.
- [ ] #20: Match gclo Help commands and carry defaults into AppKit samples and the starter. Implemented locally; builds/generation/setup tests pass, native acceptance/signing/push pending.
- [x] #21: Replace owner-rejected modal tabs with a standard resizable WinUI Settings window, left navigation, native controls, immediate checked width preferences and explicit CSS apply. Theme/editor dialogs and pickers belong to Settings.
- [ ] #21: Verify native window resizing/navigation, immediate preferences, CSS apply/discard and owned theme/editor/picker dialogs in distributed builds.
- [x] #16: Implement native passive source highlighting, document/shell colors, high contrast and exact plain copying.570 core tests and100% measured lines pass.
- [ ] #16: Run real native color/mode/palette/selection/clipboard and existing find/fold/scroll/reload acceptance in distributed builds.
- [x] #17: Implement50/50 source opening/reopening, proportional mouse resizing and focused Left/Right/Home controls;21 core layout regressions pass.
- [ ] #17: Verify actual mouse/keyboard/outline/DPI/window-resize/reopening geometry in distributed builds.
- [x] #18: Implement reference-order native toolbar with dynamic overflow, shared reading handlers, H1–H6, enabled themes, valid editor/direct/choices and primary Print; Theme uses the native Color icon. Broadcast saved editor choices to all readers; build/format pass.
- [ ] #18: Verify four prepared actual toolbar reading/menu/editor/Print/narrow/sibling cases in native, published and both installed builds.
- [ ] #6: Verify automatic external-save refresh or a clear stale/reload notification in the current build. Native watcher6/6 passes; explicit fallback/new locked-save UI case implemented, actual acceptance pending.

- [x] #24: Move Darin attribution into About Contributions; link23 runtime/platform and29 development entries verified against restored package metadata; preserve11 distinct packaged license/notice texts.
- [ ] #24: Verify actual About scrolling, keyboard-accessible project/license links and Contributions placement in distributed builds.

- [x] #25: Add gclo-style native Advanced splash toggle/duration (500–30000 ms, 250 ms steps), startup update preference, checked saves and sibling broadcasts using the published AppKit splash.
- [ ] #25: Verify actual startup duration/enabled/disabled and shared preference persistence in distributed acceptance.
- [ ] #18: Review the tightened toolbar row, control alignment and grouped commands against the owner screenshot; geometry assertions are prepared.

## Active 1.0 release queue - 2026-10-11

Full stable1.0 and isolated development releases are authorized. Local implementation
checks below do not substitute for current native/distributed acceptance.

- [x] #27: Reproduce source-hover color loss and fix native foreground state assignments; exact hosted pointer/focus/selection regression passes on0ed608e/CI38166230860.
- [ ] #27: Verify all source color/theme/reload/copy/scroll behavior in current published and installed editions; full acceptance remains open.
- [x] #18: Remove the duplicate toolbar depth selector and label Contents/Print; retain TOC depth.
- [ ] #18: Verify current toolbar alignment, overflow and narrow-window behavior.
- [x] #21: Add independent new-window Contents/Source defaults, both off; core merge/copy regressions pass.
- [ ] #21: Verify persisted defaults and unchanged existing windows through actual Settings input.
- [x] #21: Move long theme/editor forms into native Settings pages with Back/Save/Cancel; preserve copied drafts/import conflicts/native pickers.
- [ ] #21: Verify the current Settings pages, draft cancellation/validation and imports at normal/narrow sizes.
- [x] #26: Add loaded-handle modification time, stale/reload status and native Open folder; real metadata adapter regression passes.
- [ ] #26: Verify actual footer changes during locked/replaced/missing files and independent windows.
- [x] #28: Add accessible/native delayed disabled-editor explanation with the actual unavailable reason and Settings guidance.
- [ ] #28: Verify actual disabled hover, mouse exit, state changes and keyboard accessibility.
- [x] #29: Generate distinct navy Markdown/reading mark through AppKit BrandTool; visually inspect the256px frame and validate all nine ICO sizes.
- [ ] #29: Verify icon in current distributed app/window/taskbar/splash and obtain owner design feedback.
- [x] #24: Limit Components to shipped open-source credits/project/license links; preserve full audit catalog and notices, with Darin in Contributions.
- [ ] #24: Verify current native About layout, Contributions and all credit links.
- [x] Port pinned document CSS/container/scroll/print semantics locally. Current combined582 core tests/100% lines,25 adapter tests,zero-warning Release/format/publish pass.
- [x] Verify immutable upstream template/CSS-help blobs and all47 shared screen CSS selector declarations; document native pane-CSS compatibility boundary in reference-fidelity-audit.md.
- [ ] Complete remaining rendering/native/distributed fidelity acceptance and owner design review; CSS declaration comparison does not establish painted-output or arbitrary native pane-CSS parity.

- [ ] Pass hosted CI on the actual current implementation; retain native/published/installed TRXs, screenshots and real PDFs.
- [x] Implement gclo-style separate stable/dev package IDs and installed-channel title/data identity;586 core tests/100% measured lines pass.
- [x] Prepare simultaneous owned stable/dev windows/profiles/registrations, real AppKit updates with a live unchanged sibling, and stable uninstall preserving dev;64 tooling tests/all configured analyzer rules pass.
- [ ] Verify the new simultaneous phase on a fresh exact hosted head; actual secondary activation/default chooser remain separate requirements.
- [ ] Diagnose dbda6d4 focused17/20 failures with retained native color/focus/hidden-section evidence; preserve exact assertions and remove temporary debug probes before release.
- [ ] Adopt the reviewed/published owned-file signing builder; verify actual Azure resource/profile/publisher and repository-bound OIDC/profile-signer permission.
- [ ] Obtain genuine current-head maintainer review acknowledgement, complete required checks and merge.
- [ ] Publish signed stable1.0/dev releases with verification of exact distributed artifacts, timestamp/publisher/checksums and installed update acceptance.
