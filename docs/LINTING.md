# Documentation Linting

This project uses [markdownlint](https://github.com/DavidAnson/markdownlint) to ensure consistent formatting and quality of Markdown documentation files.

## Quick Start

### Install Dependencies

```bash
npm install
```

### Run Linting

Check all markdown files for lint errors:

```bash
npm run lint:docs
```

Auto-fix most lint errors:

```bash
npm run lint:docs:fix
```

## Configuration

The markdownlint configuration is defined in `.markdownlint.json` at the root of the repository. The following rules are disabled for this project:

- **MD013** (line-length): Documentation often contains long URLs, tables, and code examples
- **MD024** (no-duplicate-heading): Auto-generated documentation has legitimate duplicate headings
- **MD033** (no-inline-html): Documentation uses HTML for collapsible sections and enhanced formatting
- **MD034** (no-bare-urls): Legitimate use of bare URLs in reference documentation
- **MD036** (no-emphasis-as-heading): Stylistic preference for using emphasis in certain contexts
- **MD040** (fenced-code-language): Not all code blocks have an appropriate language identifier
- **MD060** (table-column-style): Cosmetic rule that would require extensive manual table reformatting

## Continuous Integration

Documentation linting can be added to CI/CD pipelines:

```yaml
- name: Lint documentation
  run: npm run lint:docs
```

## Best Practices

1. **Run linting before committing**: Use `npm run lint:docs:fix` to auto-fix common issues
2. **Review auto-fixes**: The auto-fix can sometimes make unwanted changes, always review before committing
3. **Manual fixes**: Some issues require manual intervention, especially table formatting and code block languages
4. **Keep it green**: Try to keep the lint check passing to maintain documentation quality

## Resources

- [markdownlint rules](https://github.com/DavidAnson/markdownlint/blob/main/doc/Rules.md)
- [markdownlint-cli documentation](https://github.com/igorshubovych/markdownlint-cli)
