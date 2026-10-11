# Standards readiness

Read-only checks performed on 2026-10-11 against KofTwentyTwo/MarkdownViewr.Windows.
The full port and public release are not complete; do not add a conformance badge.

| Area | Evidence and remaining work |
| --- | --- |
| Repository settings | `build/Initialize-GitHubRepository.ps1 -Mode Validate` returns `SettingsVerified=True`: required contexts are bound to official Actions; protections, action policy and tag-only release environment read back successfully. |
| Official conformance checker | 34 PASS, 1 FAIL, 1 WARN, 3 REVIEW; full JSON retained at `artifacts/repo-conformance-2026-10-11.json`. |
| Required inventory | `K22-REPO-42` remains pending: signed inventory proposal6b4fd17 is in standards draftPR36; its checks pass. App/starter are Product with adoption-in-progress status. Review/merge is required before claiming registration in main. |
| Shared community defaults | `K22-REPO-10` warns: no owner `.github` repository. App governance/security/contributing files exist; shared owner defaults remain optional setup work. |
| Workflow reviews | `K22-CI-02/03/31` require review of PR/push tests, documented command alignment and full release gate reuse. Current callers visibly run PR/push(main) CI and the release reuses CI/security. No human review acknowledgement is inferred. |
| Test evidence | Exact9994eda passes586 core tests/100% measured lines, focused20/20 and full native98/98, zero skips. CI38176727468 is terminal: published98/98 and installed stable/dev97/97 each, zero skips; actual apply/restart passes both. Diagnostic-free764c7ac passes98/99 native/published and20/21 focused: the new multiline raw-header fold fails. Local correction passes589 core/100% lines; fresh native/installed registration acceptance remains required. |
| Release | Stable/dev synthetic installed updates call AppKit apply/restart successfully. Actual verified Azure identity/profile/signer permissions and exact signed distributed artifacts remain deferred. Owner authorized stable1.0/dev publication after all requirements pass; signing and review requirements remain unmet. |

Full acceptance remains in `docs/PRODUCT.md` and `docs/TODO.md`; maintainer review,
Best Practices owner attestations and final signing cannot be replaced by local
settings checks or green fixture tests.
