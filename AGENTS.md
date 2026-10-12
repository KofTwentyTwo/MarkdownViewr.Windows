# Repository Guidelines

## Purpose and Structure

MarkdownViewr.Windows is a Windows Markdown viewer built with AppKit and WinUI 3. Read `docs/PRODUCT.md`, `docs/TODO.md` and `docs/SESSION-STATE.md` before continuing. The first reader is implemented; full parity and desktop acceptance remain pending. Do not claim requirements passed without evidence.

`src/MarkdownViewr.Windows.Core/` contains testable logic and view models. `src/MarkdownViewr.Windows/` contains WinUI views and Windows adapters; brand assets are in `Assets/Brand/`. Unit, desktop UI and installed-update probes live under `tests/`. `build/` contains coverage, packaging, setup and release validation tools.

## Build and Test

Use Windows and the .NET 10 SDK selected by `global.json`:

```powershell
dotnet restore MarkdownViewr.Windows.slnx -p:Platform=x64 --locked-mode
dotnet build MarkdownViewr.Windows.slnx -p:Platform=x64 -warnaserror
dotnet test tests/MarkdownViewr.Windows.Core.Tests --settings coverage.runsettings --collect:'XPlat Code Coverage' --results-directory TestResults
./build/Assert-Coverage.ps1 -ResultsDirectory TestResults
dotnet format MarkdownViewr.Windows.slnx --verify-no-changes --severity warn --no-restore
```

Desktop tests require an explicitly available interactive desktop. Run `dotnet test tests/MarkdownViewr.Windows.UiTests` only when input will not interfere with other work. Preserve 100% measured core coverage; use xUnit and deterministic injected time/filesystem boundaries. Follow `Method_Scenario_Expectation` test names.

## Style and Contributions

Follow `.editorconfig`: three-space C# indentation, Allman braces, file-scoped namespaces, PascalCase public members and `_camelCase` private fields. Keep type/method comments and three blank lines between members. YAML uses two spaces. Centralize versions in `Directory.Packages.props` and commit regenerated locks.

Use topic branches, Conventional Commits, signed/DCO commits (`git commit -S -s`), and PRs targeting `main`. Report actual validation, link issues, include UI screenshots and disclose substantial AI assistance. Install both pre-commit and commit-msg hooks. Never fabricate the required human review acknowledgement.

## Security and Continuity

Treat document content and renderer messages as untrusted; keep native access narrow. Preserve the original MIT notices when adapting Darin's code. Update the threat model for renderer/file-handling changes and maintain the session/task documents. Never commit secrets or publish unsigned production releases. Follow the repository release runbooks and existing authorization boundaries.
