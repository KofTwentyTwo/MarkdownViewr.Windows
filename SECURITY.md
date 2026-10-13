# Security Policy

## Reporting a vulnerability

**Do not open a public issue for a security problem.**

Report it privately through GitHub's
[private vulnerability reporting](https://github.com/KofTwentyTwo/MarkdownViewr.Windows/security/advisories/new)
(the repository's **Security** tab, then **Report a vulnerability**). That is the only
reporting channel.

Security contact: **James Maes**, maintainer (reached through the private reporting link
above).

Please include the affected version, how to reproduce it, and the impact you see. The
app's attack surface is described in its
[threat model](docs/security/threat-model.md).

## What to expect

KofTwentyTwo follows coordinated vulnerability disclosure. Timeframes are counted
from the day the report arrives and come from the
[KofTwentyTwo security program](https://github.com/KofTwentyTwo/standards/blob/main/policies/security-program.md)
(targets until the standards reach v1.0.0, commitments from then on):

| Step | Target |
| --- | --- |
| Acknowledge the report | 7 days |
| Assess it: confirm or reject, with a severity | 14 days |
| Release a fix for a CRITICAL vulnerability | 7 days |
| Release a fix for a HIGH vulnerability | 30 days |
| Release a fix for a MEDIUM vulnerability | 90 days |
| Release a fix for a LOW vulnerability | The next release |
| Publish the advisory (GitHub Security Advisory, CVE where applicable) | When the fix ships, or 90 days after the report, whichever comes first |

Reporters are credited in the advisory unless they ask not to be.

## Supported versions

| Version | Security fixes |
| --- | --- |
| Latest minor release | Yes |
| Older | No; update in place with **Help → Check for updates**, or install the latest release |

Only the latest minor release receives fixes; a line stops receiving security fixes when
the next minor version is released. After a new major version, the previous major's last
minor receives security fixes for 6 months.

## Code signing

Public Windows releases require Authenticode signing of the final shipped binaries
and installers. Publication fails closed until the approved Azure signing identity
and expected publisher, trust and timestamp are verified. Unsigned test fixtures are not releases.

Verify final signatures, `SHA256SUMS` and provenance attestations as described in the
[Developer Guide](DEVELOPERS.md#releases-and-verification) and
[release signing instructions](docs/release-signing.md). No historical exception
is claimed as permission to publish unsigned application releases.

## Published vulnerabilities

Fixed vulnerabilities are published as
[GitHub Security Advisories](https://github.com/KofTwentyTwo/MarkdownViewr.Windows/security/advisories).
