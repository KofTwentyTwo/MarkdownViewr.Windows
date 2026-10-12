# Scorecard process finding assessment

Prepared 2026-10-12 for the first Windows release. These findings describe
repository processes; they do not identify an exploitable shipped component.
This assessment does not fabricate a vulnerability waiver, independent reviewer,
90-day maintenance history or a passing Best Practices badge.

## Single-maintainer review and branch controls

The owner selected the single-maintainer policy. Approved
[EX-0001](https://github.com/KofTwentyTwo/standards/blob/main/exceptions/register.md#ex-0001)
applies to every KofTwentyTwo repository and records the missing non-author
human approval as an accepted risk, with compensating controls.

[Code-Review #8](https://github.com/KofTwentyTwo/MarkdownViewr.Windows/security/code-scanning/8)
correctly counts no independently approved changesets. The main ruleset has
no bypass actors, requires PRs, signed commits, linear history, resolved threads
and seventeen Actions-bound checks, including the maintainer-owned automated
review record. The current application PR remains blocked until that actual
record exists; this assessment cannot supply it.

[Branch-Protection #5](https://github.com/KofTwentyTwo/MarkdownViewr.Windows/security/code-scanning/5)
identifies the same unavailable independent approver, code-owner approval and
last-push approval. Keep the chosen policy and existing controls intact.
Reassess when a second maintainer joins; require independent approval then.
This is an accepted review gap under the existing exception, not maximal
Scorecard protection or proof that every historical change was reviewed.

## Repository-age finding

[Maintained #9](https://github.com/KofTwentyTwo/MarkdownViewr.Windows/security/code-scanning/9)
explicitly reports creation within the last90 days. GitHub reports creation on
2026-10-11; recent issues, signed feature commits and actual CI evidence show
current activity. The age-based score does not establish abandonment, nor does
current activity prove long-term maintenance. Reassess on2027-01-09, after the
90-day boundary, and investigate any remaining maintenance finding honestly.

## Remaining findings and actions

Retain the current alert state until the assessment is reviewed and any authorized
disposition is recorded. Do not disable Scorecard or invent changesets to raise
its score. Fuzzing and SAST scores on the scaffold main revision must be
reassessed after the tested application is merged; native fuzz/property targets
may not be recognized by Scorecard. The actual Windows-specific Best Practices
enrollment/assessment remains open in issue#31. No earned badge is claimed.

No OpenVEX statement is fabricated for process findings without a vulnerable
component. Actual exploitable code/dependency findings still need remediation
or a reviewed, precisely scoped exception before shipping.
