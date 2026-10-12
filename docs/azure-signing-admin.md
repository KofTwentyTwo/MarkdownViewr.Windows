# MarkdownViewr signing administrator actions

No Azure changes have been applied: the available CLI has no signed-in account.
Reuse the existing subscription/account/validation/profile. Coordinate identity
changes with gclo and other app owners; retain their applications, credentials and
role assignments. Never export keys or add administrative passwords to CI.

## Verify the deployed resource

Public Microsoft OpenID discovery on2026-10-11 resolves the supplied directory
`koftwentytwooutlook.onmicrosoft.com` to tenant GUID
`4ebeb0ad-5782-487a-a1b4-5080255774cf`. Its
[discovery metadata](https://login.microsoftonline.com/koftwentytwooutlook.onmicrosoft.com/v2.0/.well-known/openid-configuration)
returns issuer `https://login.microsoftonline.com/4ebeb0ad-5782-487a-a1b4-5080255774cf/v2.0`.
This identifies the directory only; it does not establish the signing subscription's
tenant, regional endpoint, active profile or verified publisher subject. The
existing CLI context still has no authenticated account. Confirm the deployed
resource below before setting any CI values; no login retry or Azure change was made.

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
