# Contributing

Thanks for your interest in contributing! This document describes how to get set up, coding standards, and how to submit changes.

## Getting started

1. Ensure you have the prerequisites from the README installed.
2. Fork the repo (if external) and create a feature branch:

   - Suggested: `feature/<short-description>` or `fix/<short-description>`

3. Build and run tests locally:

   - `dotnet build OnePageAuthorAPI.sln -c Debug`
   - `dotnet test OnePageAuthorAPI.sln -c Debug`

## Development workflow

- Keep functions thin; put orchestration and core logic in `OnePageAuthorLib`.
- Prefer DI extensions defined in `ServiceFactory` to keep hosts simple and consistent.
- Add unit tests for new functionality (happy path + at least one edge case).
- For public behavior changes, update or add tests accordingly.

## Coding guidelines

- C# 12 / .NET 9 features are acceptable when they improve clarity.
- Favor async where I/O is involved.
- Keep exception messages actionable; log contextual details.
- Repository partition keys and container setup are handled in DI; avoid re-creating clients/containers in app code.

## Commits and PRs

- Prefer Conventional Commits style (e.g., `feat: add ensure customer orchestrator`).
- Keep PRs focused and small when possible; include a brief description and testing notes.
- Link related issues.

## Testing

- Run the entire test suite before opening a PR: `dotnet test OnePageAuthorAPI.sln`.
- Add tests alongside new code; for fixes, add a regression test.
- **CI/CD Quality Gate**: All unit tests are automatically run in the GitHub Actions workflow. If any tests fail, deployment to Azure is blocked. Ensure all tests pass locally before pushing.

## Security and secrets

- Never commit secrets; use environment variables or user secrets.
- See `SECURITY.md` for vulnerability reporting.

## Documentation

- If you add or change public-facing behavior, update `README.md` and any relevant docs.
