# Windows release signing

Application releases require Authenticode signing. The caller enables `sign: true`
with an explicit six-file ownership catalog and pins the reviewed standards v1.1.0
builder. Configuration and final-signature checks stop publication on failure.
The source template's content-only NuGet package uses a separate release workflow.

## Configure the protected environment

Use an existing authorized Artifact Signing account and Active Public Trust
profile. Verify its deployed region/endpoint, actual tenant GUID and full
certificate publisher subject. A subscription ID or directory domain is not the
tenant GUID. Each repository needs an approved CI identity, GitHub OIDC trust
restricted to the repository's actual release-environment OIDC subject, and the Artifact Signing
Certificate Profile Signer role scoped to its profile. Owner/Contributor alone
does not grant signing permission. Preserve other identities and assignments.

Set these non-secret variables in the protected `release` environment, permitting
only the intended `v*` release tags:

| Variable | Verified value |
| --- | --- |
| `AZURE_TENANT_ID` | Tenant GUID |
| `AZURE_CLIENT_ID` | Approved CI application/client GUID |
| `AZURE_SUBSCRIPTION_ID` | Signing resource's subscription GUID |
| `ARTIFACT_SIGNING_ENDPOINT` | Actual regional HTTPS endpoint |
| `ARTIFACT_SIGNING_ACCOUNT` | Existing signing account name |
| `ARTIFACT_SIGNING_PROFILE` | Actual active certificate profile |
| `ARTIFACT_SIGNING_SUBJECT` | Exact verified certificate subject |

Legacy `TRUSTED_SIGNING_*` configuration names remain accepted for the last four
values. Do not copy another repository's client ID without verifying its trust
and signer assignment. Template consumers do not inherit access to KofTwentyTwo's
signing service. Never export a PFX or put administrative passwords in CI.

## Enable the reviewed builder

The release caller pins standards v1.1.0 at
`93b1d6163a4c466ae443e98b62786fc4d80d33bd`. Its reviewed builder supports the
explicit ownership catalog and verifies final shipped bytes/signatures. The
configured catalog is:

```yaml
sign: true
signing-files: |
  app/MarkdownViewr.Windows.exe
  app/MarkdownViewr.Windows.dll
  app/MarkdownViewr.Windows.Core.dll
  app/KofTwentyTwo.AppKit.dll
  app/KofTwentyTwo.AppKit.Updates.dll
  app/KofTwentyTwo.AppKit.WinUI.dll
```

For a CLI-enabled app, include `cli/markdownviewr-windows.exe`. Adapt names to the generated
project. Keep signing confined to protected Windows release jobs using the
official pinned Azure login/signing actions and their CLI-backed OIDC credential.
Pull requests receive no signing access. Preserve third-party signatures; sign
owned binaries before packaging and generated helpers/installers through the
packaging hook. Checksums, updater metadata and attestations follow signing.

## Verify before publication

The configuration preflight does not prove authorization or a valid signature.
Run representative real signing and `signtool verify /pa /all /v <artifact>` on
the exact shipped binaries and installers. Require the expected publisher,
trusted signatures, SHA256 digests and RFC3161 timestamps. Verification failures
must stop publication. For future MSIX packaging, its Publisher must match the
verified subject. Local signing uses Microsoft's Artifact Signing Client Tools
and the installed SignTool/dlib paths with an authorized identity.

MarkdownViewr-specific verified OIDC claims and the precise remaining Azure
administrator steps are in [azure-signing-admin.md](azure-signing-admin.md).
This repository uses immutable owner/repository IDs in its subject; a copied
name-only trust from gclo would not match. Azure resource/identity/role and protected release variables are verified. The
local six-binary and final installer/portable/update-package signing proofs pass.
The new CI identity and production artifacts still require the protected release
run; local administrative signing does not prove CI OIDC.
See the administrator runbook for actual evidence and remaining dependencies.
