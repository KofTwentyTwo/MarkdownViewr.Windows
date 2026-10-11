# MarkdownViewr

> In development: a Windows counterpart to [Darin's markdownViewr](https://github.com/darinkelkhoff/markdownViewr), built with AppKit. The first reader implementation opens Markdown, renders GFM and supports manual and automatic reload. The 20 original document themes are implemented; user-theme editing is implemented with acceptance pending; navigation and printing remain in development. Hosted reader/reload and both installed update channels pass. See [the feature plan](docs/PRODUCT.md) and [development tasks](docs/TODO.md). No Windows release is available yet.

A Windows Markdown viewer with live reload, themes and document navigation.

[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/KofTwentyTwo/MarkdownViewr.Windows/badge)](https://scorecard.dev/viewer/?uri=github.com/KofTwentyTwo/MarkdownViewr.Windows)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)

| | |
| --- | --- |
| **Tier** | Product ([scope](https://github.com/KofTwentyTwo/standards/blob/main/policies/README.md#scope)) |
| **Standards** | Adopting [KofTwentyTwo standards](https://github.com/KofTwentyTwo/standards) (pre-v1.0); built from the KofTwentyTwo Windows app template |
| **Latest release** | [Releases](https://github.com/KofTwentyTwo/MarkdownViewr.Windows/releases/latest) |

## Install

No Windows release has been published. The following describes the planned release artifacts.

Download `markdownviewr-windows-stable-Setup.exe` from the
[latest release](https://github.com/KofTwentyTwo/MarkdownViewr.Windows/releases/latest) and run it. It
installs per user (no administrator rights needed) and updates itself in place from
**Help → Check for updates**. `markdownviewr-windows-stable-Portable.zip` is a portable build that
does not self-update.

Prerelease tags (`v1.2.3-beta.1`) publish to the `dev` channel
(`markdownviewr-windows-dev-Setup.exe`); an installation only ever updates within its own channel.

## Usage

- **File → Open / Ctrl+O:** choose a Markdown document. A literal file path can also be supplied when launching the executable.
- **Live reload:** saves automatically refresh the document, including replacement saves. Deletion keeps the last version visible until the file is restored. The relative reading position is preserved.
- **File → Reload:** reread the current file manually. Local images stay within the document folder; remote images and authored scripts/styles are blocked.
- **View → Document theme:** choose one of Darin's 20 built-in palettes. The saved document palette is independent of the Windows shell theme; changing it retains the reading position.
- **View → Edit document theme:** edit colors, code font/size and custom CSS. Keep its name to override a built-in, or enter a new name to create a custom theme. Invalid input keeps the editor open. **Restore built-in themes** removes built-in edits after confirmation and keeps separately named custom themes.
- **View → Frontmatter:** hide leading YAML by default, show an escaped metadata table, or render the block as Markdown. The mode is saved and applied to reloads.
- **View:** choose the light, dark, or system shell theme; turn the startup splash and the
  startup update check on or off. Choices are saved immediately.
- **Help → Activity log:** a live view of the app's log, with an errors-only filter and a
  shortcut to the log folder.
- **Help → Check for updates** and **Help → About**.

Settings and logs live in `%LOCALAPPDATA%\markdownviewr-windows`. Set the data-folder environment
variable the app reports in its log (the app id in capitals, plus `_DATA_DIR`) to use a
different folder, for example a portable one.

## Building from source

Prerequisites:

- Windows 10 version 1809 or later, x64 or ARM64
- Microsoft Edge WebView2 evergreen Runtime
- .NET 10 SDK (the feature band is pinned by `global.json`)
- Optionally Visual Studio 2026 (Windows App SDK workload) or Rider

```powershell
# Once after generation, create and commit the NuGet locks locally:
# dotnet restore MarkdownViewr.Windows.slnx -p:Platform=x64
dotnet restore MarkdownViewr.Windows.slnx -p:Platform=x64 --locked-mode
dotnet build MarkdownViewr.Windows.slnx -p:Platform=x64 -warnaserror
dotnet test MarkdownViewr.Windows.slnx -p:Platform=x64 --no-build --filter "FullyQualifiedName!~UiTests"
dotnet test tests/MarkdownViewr.Windows.UiTests --no-build   # launches the app on the desktop
```

Package versions are declared in `Directory.Packages.props` and locked in each project's
`packages.lock.json`. [CONTRIBUTING.md](CONTRIBUTING.md) has the full local workflow,
including the coverage gate and the format check that CI runs.

## Verifying a release

Every release asset is listed in `SHA256SUMS` and has a build-provenance attestation
signed by the KofTwentyTwo release workflow:

```powershell
# Integrity: the file matches the published checksum
(Get-FileHash .\markdownviewr-windows-stable-Setup.exe -Algorithm SHA256).Hash   # compare to SHA256SUMS

# Origin: built by the KofTwentyTwo release workflow from this repository
gh attestation verify .\markdownviewr-windows-stable-Setup.exe --repo KofTwentyTwo/MarkdownViewr.Windows --signer-repo KofTwentyTwo/standards

# Publisher: Authenticode signature (once code signing is enabled; see SECURITY.md)
Get-AuthenticodeSignature .\markdownviewr-windows-stable-Setup.exe | Format-List Status, SignerCertificate
```

## Support

The latest minor release receives fixes; see [SECURITY.md](SECURITY.md#supported-versions)
for the full support policy.

## Security

Report vulnerabilities privately; see [SECURITY.md](SECURITY.md). The app's attack
surface is described in its [threat model](docs/security/threat-model.md).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE). Copyright (c) 2026 James Maes.
