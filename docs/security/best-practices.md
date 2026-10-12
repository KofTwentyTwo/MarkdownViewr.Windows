# OpenSSF Best Practices assessment

## Scope and enrollment

MarkdownViewr.Windows is a public open-source Product; K22-REPO-12 requires real enrollment and an evidence-backed assessment. Maintainer: @KofTwentyTwo (James Maes). This entry covers the Windows repository, separately from Darin Kelkhoff's macOS project.

Registered on 2026-10-12 as [project 15417](https://www.bestpractices.dev/en/projects/15417), with repository and home page both pointing to this GitHub repository. The Metal passing assessment was initially **18% complete** after provider import. Registration and a progress badge do not establish passing status.

Reassess by 2027-01-10 and before each MINOR/MAJOR release. The maintainer must confirm actual operating practices and incomplete criteria.

## Evidence

| Area | Evidence and limits |
| --- | --- |
| Identity/license | README, MIT LICENSE, CONTRIBUTING, retained author/vendor notices; standards inventory merged in PR #36. |
| Documentation/change control | User/developer guides, PRODUCT/ADRs, linked issues, signed/DCO commits in app PR #3. Final-head human acknowledgement remains required. |
| Security reporting | SECURITY.md, private GitHub reporting, response/support policy, and threat model. Actual response practices require maintainer attestation. |
| Build/test | Locked restore, zero-warning build, analyzers, 100% measured core coverage, parser campaigns, native/published/installed acceptance. Tests do not certify arbitrary documents. |
| Supply chain | Full-SHA action pins, dependency review/audit, secret/workflow/CodeQL scans, component credits, bundled notices. |
| Releases/crypto | Current installed fixture updates/coexistence pass. Local signing/distribution proof passes; production CI OIDC signing and published stable/dev assets remain pending. |
| Governance | Single-maintainer policy is explicit. Automated review is not independent human review; acknowledgements must be genuine. |

## Remaining assessment actions

1. Review each provider criterion against repository evidence and actual operations; mark incomplete items honestly.
2. Confirm security-response and review practices personally.
3. Complete production release verification before claiming release-related criteria.
4. Save the evidence-backed assessment on the provider, with any required agreement confirmed by the maintainer.

No passing badge, approved exception, independent reviewer, or completed production release is claimed by this document.
