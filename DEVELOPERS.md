# Developer Guide

For installation and everyday use, see the [README](README.md) and [user guide](docs/user-guide.md). Contribution requirements are in [CONTRIBUTING.md](CONTRIBUTING.md).

## Environment and structure

Use Windows 10 1809 or later, Microsoft Edge WebView2 Runtime, and the .NET 10 SDK selected by `global.json`. The current release target is Windows x64.

- `src/MarkdownViewr.Windows.Core/`: testable document, rendering, theme, and view-model logic.
- `src/MarkdownViewr.Windows/`: WinUI views, WebView2 integration, Windows adapters, and `Assets/Brand/`.
- `tests/`: xUnit core tests, native/published UI acceptance, and installed-update probes.
- `build/`: coverage, repository setup, packaging, signing, and lifecycle validation.
- `docs/`: product scope, ADRs, security evidence, and release runbooks.

Keep core logic independent of WinUI. Treat documents, renderer messages, resource URLs, and imported themes as untrusted; review the [threat model](docs/security/threat-model.md) before changing those boundaries.

## Restore, build, and run

Run from the repository root:

```powershell
dotnet restore MarkdownViewr.Windows.slnx -p:Platform=x64 --locked-mode
dotnet build MarkdownViewr.Windows.slnx -p:Platform=x64 -warnaserror
dotnet run --project src/MarkdownViewr.Windows -p:Platform=x64
```

To isolate preview data, set `MARKDOWNVIEWR_WINDOWS_DATA_DIR` for that process to a disposable absolute folder. Do not use another person's profile or interfere with an existing preview.

## Tests and quality gates

```powershell
dotnet test tests/MarkdownViewr.Windows.Core.Tests --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults
./build/Assert-Coverage.ps1 -ResultsDirectory TestResults
dotnet format MarkdownViewr.Windows.slnx --verify-no-changes --severity warn --no-restore
npm ci --ignore-scripts
npm run lint
npm test
```

Core tests use xUnit and Coverlet; retain 100% measured core line coverage. Name tests `Method_Scenario_Expectation` and add meaningful regression coverage for fixes. Deterministic tests inject time and filesystem boundaries.

Native desktop tests use FlaUI/UIA3 and require an available interactive desktop:

```powershell
dotnet test tests/MarkdownViewr.Windows.UiTests
```

Reserve the desktop before running physical-input tests. Hosted CI also exercises published output and actual stable/dev installation, update/restart, registration, and coexistence. See [installed-update acceptance](docs/installed-updates.md). Synthetic unsigned fixtures do not establish production signing.

For PowerShell, use PSScriptAnalyzer 1.25.0 with `PSScriptAnalyzerSettings.psd1`, then Pester 5.7.1 against `build/*.Tests.ps1`. Reproduce the exact sequential analyzer partitions and negative fixtures in [scripts.yml](.github/workflows/scripts.yml); engine exceptions and rule findings are failures.

Run the [seeded parser campaigns](docs/security/fuzzing.md) when changing parser or activation boundaries. Never use private documents as a corpus.

## Style and dependencies

Follow `.editorconfig`: three-space C# indentation, Allman braces, file-scoped namespaces, PascalCase public members, and `_camelCase` private fields. Keep type/method header comments and three blank lines between members. YAML uses two spaces.

Package versions live in `Directory.Packages.props`. After a deliberate version change, regenerate and commit affected locks:

```powershell
dotnet restore MarkdownViewr.Windows.slnx -p:Platform=x64 --force-evaluate
pre-commit install --hook-type pre-commit --hook-type commit-msg
```

Use `dotnet format` without `--verify-no-changes` for supported formatting fixes; inspect the diff before committing.

## Releases and verification

Follow [RELEASING.md](docs/RELEASING.md), [signing](docs/release-signing.md), and [repository setup](docs/repository-setup.md). Stable tags use `v1.2.3`; prereleases such as `v1.2.3-beta.1` publish to the isolated development channel.

The protected release builder signs owned binaries before packaging, verifies distributed signatures, and generates final checksums and attestations. Never bypass signing failures or publish fixture packages.

After publication, download the actual assets and verify them:

```powershell
Get-FileHash ./markdownviewr-windows-stable-Setup.exe -Algorithm SHA256
gh attestation verify ./markdownviewr-windows-stable-Setup.exe --repo KofTwentyTwo/MarkdownViewr.Windows --signer-repo KofTwentyTwo/standards
Get-AuthenticodeSignature ./markdownviewr-windows-stable-Setup.exe | Format-List Status, SignerCertificate
signtool verify /pa /all /v ./markdownviewr-windows-stable-Setup.exe
```

Compare hashes with the published `SHA256SUMS`; confirm publisher, trust, and RFC3161 timestamp. Verify both packages and their shipped payloads using the release runbook.

## Branding

The AppKit BrandTool generates the vector and multi-size ICO assets:

```powershell
brandtool --out src/MarkdownViewr.Windows/Assets/Brand --start '#1A2B4D' --end '#0C1836' --glyph 'M 144 328 V 184 L 208 260 L 272 184 V 328 M 344 184 V 328 M 312 296 L 344 328 L 376 296 M 144 380 H 376' --stroke 28
```

Preserve original-author and dependency attribution in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) and the About dialog.
