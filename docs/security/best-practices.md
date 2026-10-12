# OpenSSF Best Practices assessment

## Scope and ownership

MarkdownViewr.Windows is a public open-source Product; K22-REPO-12 requires real
enrollment and an evidence-backed assessment. This is the Windows repository,
not Darin Kelkhoff's original macOS project. Maintainer: @KofTwentyTwo (James Maes).
Prepared 2026-10-11 by Codex; maintainer/provider assessment has not been completed.
Reassess by 2027-01-09, and before each MINOR/MAJOR release.

No project URL/ID is established. `assurance.json` correctly records required
scope and a null project URL; the README has no invented Best Practices badge.
Enrollment remains a failing requirement, not an exception or passing badge.

## Evidence prepared for enrollment

| Area | Repository evidence and precise limit |
| --- | --- |
| Identity/license | README, LICENSE (MIT), CONTRIBUTING, public source and retained Darin/vendor notices. Shared inventory entry is proposed in standards PR #36, not merged. |
| Documentation/change control | PRODUCT/ADRs, usage/build/replay commands, linked feature/bug issues and signed/DCO topic commits in PR #3. Final-head maintainer acknowledgement remains required. |
| Security reporting | SECURITY.md supplies private GitHub vulnerability reporting, response targets and supported-version policy. Threat model records browser/filesystem/IPC/editor/update boundaries. Maintainer must attest actual operating practices. |
| Build/test | Locked dependencies, zero-warning build, formatter/analyzers, core coverage, hosted native/published/installed acceptance and the prepared parser fuzzing gate. Unsigned fixture tests do not prove signatures or arbitrary document coverage. |
| Supply chain | Full-SHA action pins, dependency review/audit, secret/workflow/CodeQL scans, linked component credits and bundled license notices. |
| Releases/crypto | Stable/dev update isolation and byte checks pass synthetic installed acceptance. Real Azure signing authority, final shipped signature/timestamp verification and signed 1.0/dev releases remain incomplete. |
| Governance | Single-maintainer policy is explicit. Automated review does not constitute independent human review; every current-head finding/acknowledgement must be genuine. |

## Maintainer actions

1. Sign into [OpenSSF Best Practices](https://www.bestpractices.dev/en) and create
   the project for `https://github.com/KofTwentyTwo/MarkdownViewr.Windows`, named
   MarkdownViewr for Windows. Preserve any existing project if enrollment already
   exists; verify its repository identity instead of creating a duplicate.
2. Supply the actual returned project URL/ID. Set `badge.projectUrl` in
   [assurance.json](assurance.json), then link its real badge from README.
3. Review every current criterion against the evidence above and actual operations.
   Record incomplete items honestly; registration alone is not a passing assessment.
   Update this document with the assessed URL/date and remaining criterion actions.
4. Resolve signing/release and other required criteria before claiming conformance.
   An enrollment obstacle needs a reviewed dated standards exception; no exception
   is approved by this document or by an agent posting a proposed draft.
