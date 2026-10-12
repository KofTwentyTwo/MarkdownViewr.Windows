# Contributing to MarkdownViewr

Questions, bug reports, documentation improvements, and code contributions are welcome. For everyday use, start with the [user guide](docs/user-guide.md). For build commands and tests, use [DEVELOPERS.md](DEVELOPERS.md).

## Before you start

For a substantial change, open an [issue](https://github.com/KofTwentyTwo/MarkdownViewr.Windows/issues/new/choose) first to agree on the approach. Ask usage questions in [Discussions](https://github.com/KofTwentyTwo/MarkdownViewr.Windows/discussions).

Report security vulnerabilities privately according to [SECURITY.md](SECURITY.md). Do not include private documents, credentials, or personal information in reports, screenshots, or test fixtures.

## Development workflow

This Product follows the [KofTwentyTwo standards](https://github.com/KofTwentyTwo/standards).

1. Branch from `main` using `<type>/<short-description>`, for example `fix/source-highlighting`.
2. Follow the [developer guide](DEVELOPERS.md), install both pre-commit and commit-msg hooks, and run the relevant quality gates.
3. Make signed, signed-off [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/): `git commit -S -s`.
4. Open a pull request targeting `main` with a Conventional Commit title. PRs are squash merged.
5. Resolve all review threads and required checks before merging.

## Pull request checklist

- Explain the problem, resulting behavior, and linked issues.
- Report the checks you actually ran and any limitations.
- Include screenshots for UI changes and update user documentation.
- Retain zero-warning builds, clean formatting, and 100% measured core line coverage.
- Add regression tests for fixes; exercise native and installed behavior where relevant.
- Justify dependency changes and commit regenerated locks.
- Update the [threat model](docs/security/threat-model.md) for attack-surface changes.
- Preserve original MIT notices and dependency credits.
- Disclose substantial AI assistance and include a `Co-Authored-By:` trailer naming the model.

Required CI covers build/coverage/style, reader and installed-update acceptance, parser fuzzing, tooling tests, dependency/secret/workflow scans, CodeQL, PR title, and DCO.

Under the single-maintainer policy, `review / automated` also requires a genuine current-commit acknowledgement from the configured human maintainer. An agent cannot provide that acknowledgement. See [repository setup](docs/repository-setup.md).

## Sign-off and license

Signing off certifies the [Developer Certificate of Origin 1.1](https://developercertificate.org/): you wrote the contribution or have the right to submit it under the project's license. `git commit -s` adds:

```text
Signed-off-by: Your Name <your.email@example.com>
```

The sign-off must match the commit author. Contributions are licensed under the project's [MIT License](LICENSE). AI assistance does not remove your responsibility to understand, test, and explain the change.
