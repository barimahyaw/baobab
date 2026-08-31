# Security Policy

## Supported Versions

This project is under active development. Security fixes are applied to the
latest released version on the `master` branch; there is no formal long-term
support policy yet.

## Reporting a Vulnerability

Please do not report security vulnerabilities through public GitHub issues.

Instead, use GitHub's private vulnerability reporting for this repository:
open the **Security** tab → **Report a vulnerability**. This opens a private
advisory visible only to the maintainer until a fix is ready.

When reporting, please include:
- A description of the vulnerability and its potential impact
- Steps to reproduce it
- A suggested fix, if you have one

You should expect an initial response within a few days. This is a
solo-maintained project, so response times can vary — thank you for your
patience.

## Scope

Baobab.SharedKernel is a Clean Architecture / DDD foundation library, not a
hosted service. Most security properties of applications built on it (input
validation, authorization rules, secrets management, transport security)
depend on how the consuming application configures and uses it. Findings
about the library's own code — e.g. the JWT validation configuration, the
API key/secret generation scheme, or the audit trail — are in scope. Findings
that only apply to a hypothetical misconfiguration by a consumer are still
welcome, but will generally be addressed via documentation rather than a
code change.

## Responsible Disclosure

Please give a reasonable amount of time to address a reported issue before
any public disclosure. Credit is given to reporters who wish to be named,
once a fix is released.
