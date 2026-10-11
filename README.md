# MarkdownViewr

> In development: a Windows counterpart to [Darin Kelkhoff's macOS markdownViewr](https://github.com/darinkelkhoff/markdownViewr) ([his website](https://kelkhoff.com/)), built with AppKit. The reader, source/find/navigation/editor features and full printing pass hosted acceptance on `bf4fcdc`, including both installed update channels. Document-window acceptance and the new renderer, typography, shared appearance and theme-management controls remain in progress. Full reference parity and signed production releases are not complete.

A Windows Markdown viewer with live reload, themes and document navigation.

[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/KofTwentyTwo/MarkdownViewr.Windows/badge)](https://scorecard.dev/viewer/?uri=github.com/KofTwentyTwo/MarkdownViewr.Windows)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)

| | |
| --- | --- |
| **Tier** | Product ([scope](https://github.com/KofTwentyTwo/standards/blob/main/policies/README.md#scope)) |
| **Standards** | Adopting [KofTwentyTwo standards](https://github.com/KofTwentyTwo/standards) (pre-v1.0); built from the KofTwentyTwo Windows app template |
| **Latest release** | [Releases](https://github.com/KofTwentyTwo/MarkdownViewr.Windows/releases/latest) |

Settings → Advanced contains the startup splash toggle, splash duration (500–30000 milliseconds) and startup update checks. Changes save immediately and apply on the next launch. The native toolbar keeps Print visible, aligns controls in a compact row and groups reading commands.

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
- **Settings → Advanced → Choose default file types:** an installed copy opens Windows Default Apps so you can choose MarkdownViewr for `.md`, `.markdown`, `.mdown` or `.mkd`. `.txt` is optional. Windows 11 23H2+ opens this installation's app page; older versions open general Default Apps. Portable previews show installation guidance. Registration/routing tests pass locally; actual installed selection and Explorer acceptance remain pending.
- **Source highlighting:** the native read-only source view uses document-theme accents adjusted for shell light/dark contrast. Windows high contrast takes precedence. Copy preserves visible original text/newline spelling without rich-text clipboard data; find/fold/reload share the committed snapshot. New native acceptance is prepared, not yet run.
- **Live reload:** saves automatically refresh the document, including replacement saves. Deletion keeps the last version visible until the file is restored. The relative reading position is preserved.
- **File → Reload:** reread the current file manually. Local images stay within the document folder; remote images and authored scripts/styles are blocked.
- **File → Print / Ctrl+P:** choose a separate print theme, images/backgrounds, standard or matching font zoom, content width, borders and page padding. **Print...** opens Windows print UI; **Save PDF...** asks for a PDF destination. Cancel leaves the reading view unchanged. The earlier five hosted print cases passed in native, published and both installed builds. The owner-reported missing system dialog and PDF behavior still need current-build reproduction; wider options, readable-file verification and opt-in PDF/folder actions are implemented locally. Final production signatures remain unverified.
- **Document toolbar:** TOC and H1–H6 depth appear on the left; source, minus/percentage/plus zoom, theme and external editor follow in the Mac reference order. **Print** is a primary toolbar command; **Theme** uses the Color icon. Native overflow keeps commands accessible in narrow windows. Buttons share View/File handlers; depth is disabled when TOC is hidden, and editors launch directly or offer named choices. New toolbar/shared-editor acceptance is pending.
- **Reading zoom:** use the minus/percentage/plus toolbar or **View → Zoom in / Zoom out / Actual size**. `Ctrl++` (also `Ctrl+=`), `Ctrl+-` and `Ctrl+0` change only the reading window's font scale, bounded to 30–500%. Reload and theme changes retain it; the percentage button resets to 100%. Glyph/key/reload acceptance passes in native, published and installed stable/dev builds on `2639e2d`; broader theme/find/collapse combinations remain pending.
- **View → Markdown extensions:** independently enable highlight (`==text==`), superscript (`^text^`), subscript (`~text~`) and underline (`++text++`). All start enabled; code stays literal. Choices are saved and applied to reloads.
- **File → Settings:** open the resizable Windows settings window with Appearance, Document themes, Editors and Advanced navigation. Content-width changes save immediately across readers; **Apply custom CSS** saves the advanced CSS draft. Closing discards unapplied CSS. Theme/editor dialogs use the settings window as their owner and keep their own Save/Cancel. Current desktop acceptance is pending.
- **View → Document theme:** choose one of Darin's 20 built-in palettes. The saved document palette is independent of the Windows shell theme; changing it retains the reading position.
- **File → Settings → Themes → Edit current theme:** edit all palette colors, body/heading/code fonts, body/code sizes, heading levels 1–6, line height and custom CSS. Keep its name to override a built-in, or enter a new name to create a custom theme. Invalid input keeps the editor open. **Restore built-in themes** restores and enables the original palettes, keeping edited built-ins as uniquely named custom copies.
- **Settings → Themes → Manage themes:** reorder, enable/disable, delete, create, copy or edit choices in a copied draft, including disabled themes. **Use selected** chooses and enables a default. Save updates all open readers; Cancel discards changes across editor/picker dialogs. Built-in deletion is restorable. **Next/Previous document theme** and `Ctrl+Shift+Down/Up` cycle enabled choices in saved order when reading has focus. New management acceptance is pending.
- **Settings → Themes → Import / Export:** import multiple JSON files, choosing **Overwrite**, **Keep both** or **Skip** for each conflict. Compatible newer schemas require **Import anyway** confirmation. Invalid/oversized files are rejected independently. Export any visible theme, including disabled choices, to the selected JSON destination; version and metadata are retained. The manager can also import/export its copied draft. New transfer acceptance is pending.
- **View → Table of contents:** show the heading sidebar, choose levels 1–6, and drag its divider to resize. Select a heading to jump there; collapse a selected section or all sections, then expand when ready. Matching section state survives reloads and document theme changes.
- **Settings → Editors / File → Open in editor:** save named installed `.exe` editors, add/remove or move them in the list, and choose whether each opens the document or its folder. Save commits and refreshes all open readers; Cancel discards changes. One editor has a direct named action; several offer a submenu in saved order. A missing editor or document reports a status message; the viewer remains open. Named list/folder/cancel/removal acceptance passes in native, published and both installed channels on `2639e2d`.
- **Edit → Find / Ctrl+F:** search the rendered document and literal source, with one occurrence count and wrapping **Next/Previous** controls. Enter moves forward, Shift+Enter moves back, and Escape closes find. Search temporarily reveals collapsed sections; closing it restores prior choices and retains explicit section commands made during search.
- **Reading keys:** `j/k` move a line; `Ctrl+D/U` move half a viewport; `gg/G` go to the beginning/end; `]]/[[` visit visible headings; `/` opens native find and `n/N` advance/reverse its matches. Escape closes find. Prefixes expire after 500 ms; typing in find/theme/editor controls retains normal behavior. Windows `Ctrl+F` opens find; `Ctrl+B` moves back a viewport. Physical-key acceptance has passed in hosted native, published and installed builds.
- **View → Markdown source:** show the literal, read-only snapshot beside the reader, including frontmatter and fenced code. Visibility persists; every window and reopening starts at 50/50. Drag the divider left/right, or focus it and use Left/Right arrows; Home restores equal panes. The outline is excluded from the split, and saves refresh both surfaces. New proportional/keyboard layout acceptance is pending. Scrolling follows the corresponding heading positions in either pane. Section collapse retains its literal heading and replaces the hidden source body with an ellipsis; nested choices remain independent. Find temporarily expands the source, then restores prior folds on close. Actual bidirectional heading/wheel/focus/fold/find/reload acceptance passes in native, published and both installed channels on `2639e2d`; raw-heading parity audit remains open.
- **View → Frontmatter:** hide leading YAML by default, show an escaped metadata table, or render the block as Markdown. The mode is saved and applied to reloads.
- **View:** choose the light, dark, or system shell theme; turn the startup splash and the
  startup update check on or off. Choices are saved immediately.
- **Help → Activity log:** a live view of the app's log, with an errors-only filter and a
  shortcut to the log folder.
- **Help → View on GitHub / Submit an issue… / Community support…:** source, bug/feature templates and Discussions for the Windows application.
- **Help → Check for updates** and **Help → About**; About → Contributions credits Darin's original Mac app with repository, website and license links; Components links every restored application dependency, with development/test tools acknowledged separately.

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

This Windows application is based on [Darin Kelkhoff's macOS markdownViewr](https://github.com/darinkelkhoff/markdownViewr). Visit [Darin's website](https://kelkhoff.com/). The original MIT copyright/license and adapted resource notices are included in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md), distributed with the app.


The current review build uses native Settings pages for long theme/editor forms,
with Back, Save and Cancel. Contents and Source start off in new windows unless
changed under Settings → Advanced; toolbar toggles affect the current reader.
Contents and Print have icon/text labels, with depth selection retained in the
Contents pane. The footer shows the loaded file's modification time, marks failed
reloads/stale files and offers Open folder. Disabled editor commands explain their
configuration state. These owner-review changes have passing local build/core/
adapter/style gates; current native/distributed acceptance remains pending.


Stable releases use `markdownviewr-windows`; prereleases use
`markdownviewr-windows-dev` and show **MarkdownViewr (dev)**. Each installed edition
has its own install directory, settings/recent files, activity logs and update
channel. Local loose builds retain the stable identity. Edition selection reads
the actual Velopack channel before activation or window services initialize.
Current local identity tests pass; actual side-by-side install/update acceptance
remains required before publication.

The icon uses a distinct white Markdown/reading mark on the navy rounded tile
shared with gclo. AppKit BrandTool generates its vector and all nine ICO sizes:

```powershell
brandtool --out src/MarkdownViewr.Windows/Assets/Brand --start '#1A2B4D' --end '#0C1836' --glyph 'M 144 328 V 184 L 208 260 L 272 184 V 328 M 344 184 V 328 M 312 296 L 344 328 L 376 296 M 144 380 H 376' --stroke 28
```
