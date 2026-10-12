# MarkdownViewr signing administrator actions

Verified on2026-10-12 using authenticated Azure CLI and ARM resource readback.
The existing account, validation and profile were reused. A separate secretless
MarkdownViewr CI identity, exact release-environment trust and profile-scoped
signer assignment are created and independently read back. All seven protected
GitHub release variables match the verified deployment. gclo's identity,
credentials and assignments were preserved.

## Verified deployment and local signing proof

| Setting | Verified value |
| --- | --- |
| Subscription | `8ef2479e-3346-4cba-93ad-101c6cd237a0` (`Kof22 Signing Account`) |
| Tenant | `4ebeb0ad-5782-487a-a1b4-5080255774cf` |
| Account / region | `kof22signing` / `eastus` |
| Regional endpoint | `https://eus.codesigning.azure.net/` |
| Profile | `releases`, Active, PublicTrust |
| Certificate subject | `CN=James Maes, O=James Maes, L=Chester, S=il, C=US` |
| CI client ID | `8f51ec7a-5d91-45db-a374-c628566fc383` |
| Application object ID | `1640971f-8e5c-4f3a-802e-8633630e544c` |
| Service-principal object ID | `4e9e7d2b-5df4-4312-9d01-9845922c4d92` |

The sole CI federation uses the exact immutable subject shown below. Its signer
role is limited to the `releases` certificate profile; no Owner/Contributor role
or client secret was added. The protected GitHub environment permits only `v*`
tags. No tenant security controls changed.

A local representative test signed disposable copies of all six owned binaries
from `321816c` with Microsoft.ArtifactSigning.Client `1.0.146`, SHA256 and RFC3161
SHA256 timestamping. The client NuGet package's Microsoft author and NuGet
repository signatures verify. Each binary passes `signtool verify /pa /all /v`
with zero warnings/errors, the exact certificate subject above, and Microsoft
Public RSA Time Stamping Authority. The existing strict signature helper also
passes all six files, including its SHA256 digest check. Logs, original/final
hashes and signature receipts are retained locally in
`artifacts/signing-proof-321816c/`; running owner previews were untouched.

This test uses the authorized interactive administrator's existing signer role.
It does not prove the new CI identity's OIDC exchange, installer signing or final
shipped artifacts. Complete standards PR#31 review/publication, adopt the
published catalog builder, and run the protected release integration before
claiming production readiness. No stable 1.0 or dev release was published.

## Interactive sign-in when device flow is blocked

The owner-reported Azure CLI error530035 means Security Defaults blocked
access. [Microsoft's current guidance](https://learn.microsoft.com/en-us/entra/fundamentals/security-defaults#block-device-code-flow)
explicitly states that Security Defaults block device-code authentication. Keep
those protections enabled. Use normal interactive Windows authentication rather
than repeating `--use-device-code`:

```powershell
az login --tenant koftwentytwooutlook.onmicrosoft.com --output none
```

Complete account selection and MFA personally. Current Azure CLI uses Windows
[Web Account Manager](https://learn.microsoft.com/en-us/cli/azure/authenticate-azure-cli-interactively#sign-in-with-web-account-manager-wam-on-windows)
by default. If interactive sign-in also fails, inspect that sign-in's actual
error and Entra sign-in log; do not assume device enrollment or broader resource
roles will resolve it. Authentication alone still does not establish access to
the signing account/profile or grant CI signing permission.

If Windows account selection cannot use the administrator's passkey provider,
use Microsoft's supported browser flow with a process-only environment setting:

```powershell
$env:AZURE_CORE_ENABLE_BROKER_ON_WINDOWS = 'false'
az login --tenant koftwentytwooutlook.onmicrosoft.com --output none
```

Run this in a dedicated PowerShell process; it does not persist CLI settings or
change tenant policy. Use the owner's browser with the enabled/unlocked
[1Password extension](https://support.1password.com/save-use-passkeys/) for its
stored passkey. Complete MFA personally. Do not export passkeys or repeat the
blocked device-code flow. If browser authentication also fails, inspect its new
error rather than assuming the passkey or tenant registration is at fault.

## Verify the deployed resource

Public Microsoft OpenID discovery on2026-10-11 resolves the supplied directory
`koftwentytwooutlook.onmicrosoft.com` to tenant GUID
`4ebeb0ad-5782-487a-a1b4-5080255774cf`. Its
[discovery metadata](https://login.microsoftonline.com/koftwentytwooutlook.onmicrosoft.com/v2.0/.well-known/openid-configuration)
returns issuer `https://login.microsoftonline.com/4ebeb0ad-5782-487a-a1b4-5080255774cf/v2.0`.
This identifies the directory only; it does not establish the signing subscription's
tenant, regional endpoint, active profile or verified publisher subject. The
authenticated ARM readback above now confirms this directory is the signing
subscription's actual tenant. The commands below remain the repeatable inspection
procedure; recheck them if the deployed configuration changes.

With an authorized interactive Azure session, inspect the actual tenant, regional
account URI and profiles. The account/subscription below are owner-provided setup
values; the returned resource must confirm them before configuration.

```powershell
$subscription = '8ef2479e-3346-4cba-93ad-101c6cd237a0'
az account set --subscription $subscription
$account = az account show --query '{subscription:id,tenant:tenantId,name:name}' -o json | ConvertFrom-Json
$resourceId = "/subscriptions/$subscription/resourceGroups/rg-code-signing/providers/Microsoft.CodeSigning/codeSigningAccounts/kof22signing"
$resource = az resource show --ids $resourceId --api-version 2025-10-13 -o json | ConvertFrom-Json
$profiles = az rest --method get --url "https://management.azure.com$resourceId/certificateProfiles?api-version=2025-10-13" -o json | ConvertFrom-Json
$resource | Select-Object name,location,@{Name='Endpoint';Expression={$_.properties.accountUri}}
$profiles.value | Select-Object name,id,@{Name='Type';Expression={$_.properties.profileType}},@{Name='Status';Expression={$_.properties.status}}
```

Check every command's exit status. Confirm the tenant GUID from `account.tenant`,
not the directory domain or subscription GUID. Select the actual Active PublicTrust
profile; `releases` is expected, not assumed. Read its issued certificate's full
`properties.certificates[].subjectName` and compare it with the profile's verified
publisher in the portal. If the API has no issued certificate, inspect the profile
and associated completed identity validation in the portal; do not invent a
subject. Use `resource.properties.accountUri` as the signing endpoint. gclo's
configured publisher name is reference evidence only; the new builder requires
the full X.500 subject rather than just the publisher's display name.

[Microsoft's account/profile schemas](https://github.com/Azure/azure-rest-api-specs/tree/main/specification/codesigning/resource-manager/Microsoft.CodeSigning/CodeSigning/stable/2025-10-13)
define these inspection fields.

## Existing gclo signing reference

Read-only release inspection on2026-10-11 confirms that
[gclo v1.1.0](https://github.com/KofTwentyTwo/gclo/releases/tag/v1.1.0)
ships a valid timestamped CLI signature. The archive matches its published
SHA256SUMS (`46f02d89c9d8a459e5b02598f9791fa7aab5c16b09c254bc997f8f235a6a49e2`).
Microsoft SignTool `verify /pa /all /v` on the extracted `gclo.exe` succeeds with
one verified signature, zero warnings/errors. Its exact certificate subject is
`CN=James Maes, O=James Maes, L=Chester, S=il, C=US`; the timestamp chain is
Microsoft Public RSA Time Stamping Authority. Local reference evidence is retained
in `artifacts/gclo-signing-reference/verify.log`; the application was not executed.

This confirms the existing release recipe and shipped publisher, not the current
Azure account/profile mapping or MarkdownViewr CI authorization. Recheck the
actual deployed profile subject before using it here. gclo's client identity has
its own repository trust; do not copy that client ID without an approved reuse
and a separate exact MarkdownViewr federated credential/signer assignment.

## Configure the CI identity and trust

Inspect existing CI applications and their owners first. Reuse an approved suitable
identity; otherwise create a repository-specific single-tenant Entra application
and service principal named `MarkdownViewr.Windows release signing`. No client
secret is needed. Record its application/client GUID and service-principal object
GUID separately. Preserve all existing credentials.

The repository OIDC customization readback on2026-10-11 is `use_default=true`,
`use_immutable_subject=true`, with prefix
`repo:KofTwentyTwo@47367449/MarkdownViewr.Windows@1413777106`. Recheck it before
creating trust:

```powershell
gh api repos/KofTwentyTwo/MarkdownViewr.Windows/actions/oidc/customization/sub
```

In Entra → App registrations → the selected CI app → Certificates & secrets →
Federated credentials, add the following exact credential after confirming the
current prefix. Do not copy gclo's credential or broaden trust to pull requests.

```json
{
  "name": "markdownviewr-release",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:KofTwentyTwo@47367449/MarkdownViewr.Windows@1413777106:environment:release",
  "audiences": ["api://AzureADTokenExchange"]
}
```

For CLI administration, save that JSON locally, then use
`az ad app federated-credential create --id <application-object-id> --parameters <file>`.
Inspect existing credentials before creating; retain any unrelated entry and do
not overwrite a same-name credential whose fields differ.
[Microsoft's immutable-subject guidance](https://learn.microsoft.com/en-us/entra/workload-id/workload-identities-github-immutable-subjects)
explains the repository/owner ID components.

Assign only the signing permission at the verified profile scope:

```powershell
az role assignment create --assignee-object-id <service-principal-object-id> --assignee-principal-type ServicePrincipal --role 'Artifact Signing Certificate Profile Signer' --scope <verified-profile-resource-id>
```

Read back the assignment. Owner/Contributor permissions do not provide signing
rights. [Microsoft's role guidance](https://learn.microsoft.com/en-us/azure/artifact-signing/tutorial-assign-roles)
supports profile-scoped signing access.

## Configure and prove release signing

The protected GitHub `release` environment permits intended `v*` tags. Set the
verified values through `gh variable set NAME --repo KofTwentyTwo/MarkdownViewr.Windows
--env release --body VALUE` for `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`,
`AZURE_SUBSCRIPTION_ID`, `ARTIFACT_SIGNING_ENDPOINT`, `ARTIFACT_SIGNING_ACCOUNT`,
`ARTIFACT_SIGNING_PROFILE` and `ARTIFACT_SIGNING_SUBJECT` (full subject).

The reviewed/published catalog builder remains a dependency (standards PR#31).
Current verified official pins are azure/login@v3.1.0 commit
`a641126d1b8aa4d1fa005f4f92df94a3a4c4c906` and
azure/artifact-signing-action@v2.0.0 commit
`c7ab2a863ab5f9a846ddb8265964877ef296ee82`. Its Windows release job uses
`id-token: write` and Azure CLI-backed credentials, SHA256 and RFC3161 timestamping.
[The official signing README](https://github.com/Azure/artifact-signing-action)
describes this OIDC integration.

Follow [release-signing.md](release-signing.md) for the owned-file catalog and
local SignTool requirements. A real representative signing run must verify the
exact shipped executables, managed binaries and installers with
`signtool verify /pa /all /v`, checking full publisher, trust and timestamp.
Generate final checksums/feed metadata after signing and fail publication on any
required signing/verification error. Active profile/configuration alone is not
proof. No account recreation, new identity validation or private-key export is
part of these actions.
