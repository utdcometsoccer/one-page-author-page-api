# Security Policy

## Reporting a Vulnerability

If you believe you have found a security vulnerability, please do not open a public issue. Instead:

1. Use GitHub Security Advisories (preferred) to report privately to maintainers.
2. Alternatively, contact the maintainers privately (if a security contact is listed).

Please include:

- A description of the issue and potential impact
- Steps to reproduce or proof of concept
- Any suggested mitigations

We will acknowledge receipt and work with you on validation and remediation. Thank you for helping keep the project and users safe.

## Handling Secrets

- Do not commit secrets to the repository.
- Use environment variables, user secrets, or CI/CD secret stores.
- For local development, prefer `local.settings.json` (Functions) and exclude it from version control.
