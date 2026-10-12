# Parser fuzzing assessment

## Scope and properties

K22-TEST-25 applies to this public document reader. Owner: @KofTwentyTwo.
The native .NET/xUnit mutation harness uses the actual parsers and independent
DOM/path/source properties, with no new dependencies. It is deterministic
property fuzzing, not coverage-guided instrumentation or an exhaustive proof.
The existing core coverage gate remains unchanged.

| Target | Inputs and independent assertions |
| --- | --- |
| Markdown/HTML/frontmatter/extensions | Mutated synthetic markup; parsed output has no executable elements, event handlers or authored host attributes; checkboxes remain disabled. URLs use browser HTTPS-base semantics. |
| Source ranges/folds | Actual parser headings have bounded indices/offsets; literal source/copy preserves original newlines after fold/unfold; generated selection ranges fit the committed display. |
| Resource/heading URLs | Accepted once-decoded paths resolve within a synthetic root; foreign hosts and queried document commands are denied; heading targets remain bounded. Native open-handle containment has separate filesystem tests. |
| Theme JSON | Mutated real built-in schemas; invalid imports reject explicitly; accepted title/CSS cannot escape their HTML containers or introduce executable DOM. |
| Activation JSON | Real generated schema and validator; accepted envelopes contain only known properties and bounded absolute literal paths; unknown command fields, malformed syntax and excessive/invalid paths are rejected. No pipe/window/process is opened. |

The two target files and executable CI command are declared in
[assurance.json](assurance.json). Generated document/theme inputs are at most 4096
characters; activation envelopes have at most 144 small synthetic paths. No
attacker script, real document, credential or profile is used. No input causes
browser execution, network requests or native launches.

## Campaigns and replay

Run from the repository root with the pinned .NET 10 SDK on Windows:

```powershell
dotnet restore MarkdownViewr.Windows.slnx -p:Platform=x64 --locked-mode
$env:K22_FUZZ_ITERATIONS = '500'
dotnet test tests/MarkdownViewr.Windows.Core.Tests -c Release -p:Platform=x64 --filter Category=Fuzz
dotnet test tests/MarkdownViewr.Windows.UiTests -c Release -p:Platform=x64 --filter Category=Fuzz
# Weekly and before every MINOR/MAJOR release, repeat both commands with 10000.
$env:K22_FUZZ_ITERATIONS = '10000'
```

Each target uses seeds 22023 and 99173. PR/main campaigns run 500 iterations per
seed/target (5000 generated property evaluations total); weekly and every release
reuse run 10000 (100000 total). `fuzz.yml` runs on PRs, `main`, weekly and manual
requests; release.yml calls it and requires success before publication. The
`fuzzing` check is declared in the repository protection plan; apply and verify
that live protection after its first hosted run. The job has a 15-minute bound;
iteration configuration outside 1–100000 fails explicitly.

Failures retain seed, target, zero-based iteration and JSON-escaped synthetic
input in TRX/CI logs. To replay the sequence, use the same checkout/runtime/seed
and at least iteration+1 operations (a shorter budget does not reach it). Minimize
the input into a named regression before changing an assertion or production
code; retain original failure evidence. Genuine vulnerabilities use
[SECURITY.md](../../SECURITY.md), not a public issue containing private data.

## Evidence and review

Prepared 2026-10-11 by Codex; maintainer assessment pending; reassess by 2027-01-09,
on parser-boundary changes and before MINOR/MAJOR publication. The long local
campaign exposed an oracle error: System.Uri treats backslash input as a Windows
file URI even with an HTTPS base. AngleSharp's browser URL model preserves the
actual HTTPS semantics; a minimized regression still requires resource denial.
No production vulnerability is established from that oracle failure. Original
failed TRXs are retained; no existing test/assertion/deadline was removed.

Corrected core campaign passes 9/9 at 10000, zero skips (80000 generated inputs plus
the minimized regression). Final activation passes 2/2 at 10000, zero skips (20000
envelopes). Full core 677 tests/100% measured lines, zero-warning Release build,
formatting and 23 repository setup tests and all 64 script tests pass. Hosted execution and required-check
readback remain pending. This record does not certify
Best Practices enrollment, production signatures or general parser completeness.
