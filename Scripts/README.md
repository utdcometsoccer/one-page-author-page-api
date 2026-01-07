# Development Scripts

This directory contains PowerShell and shell scripts for development, deployment, and maintenance of the OnePageAuthor API platform.

## 📋 Script Categories

### 🚀 Development & Runtime Scripts

- **`UpdateAndRun.ps1`** - Main development script that updates packages, builds the solution, and runs Azure Functions
- **`StopFunctions.ps1`** - Stops all running Azure Functions background jobs

### 🔐 Secrets Management Scripts

- **`Initialize-GitHubSecrets.ps1`** - Configure GitHub repository secrets for CI/CD deployment
- **`Update-SecretsConfig.ps1`** - Update existing secrets configuration file with new variables
- **`Set-DotnetUserSecrets.ps1`** - Configure .NET user secrets for local development
- **`Copy-LocalizationSeederSecrets.ps1`** - Copy secrets to localization seeder projects
- **`QuickSecretCleanup.ps1`** - Quick cleanup of secret-related files
- **`Clean-QuotedConfigValues.ps1`** - Clean quoted values in configuration files

### 📚 Documentation Generation Scripts

- **`Generate-ApiDocumentation.ps1`** - Generate comprehensive API documentation from XML comments
- **`Generate-Complete-Documentation.ps1`** - Generate complete system documentation
- **`Generate-Complete-Documentation.bat`** - Windows batch wrapper for documentation generation
- **`Generate-Documentation.bat`** - Windows batch wrapper for quick documentation generation
- **`CleanupMarkdownFiles.ps1`** - Cleanup and format markdown documentation files
- **`TransformMarkdownToReadme.ps1`** - Transform markdown files to README format

### 🧪 Testing Scripts

- **`SwitchTestingScenario.ps1`** - Switch between different testing scenarios (frontend-safe, individual, production)

### 🔧 Build & Version Scripts

- **`get-assembly-version.sh`** - Extract assembly version information (used in CI/CD)

## 📖 Usage

All scripts should be run from the repository root directory. For example:

```powershell
# Start development environment
.\Scripts\UpdateAndRun.ps1

# Stop all functions
.\Scripts\StopFunctions.ps1

# Configure GitHub secrets interactively
.\Scripts\Initialize-GitHubSecrets.ps1 -Interactive

# Switch to testing scenario
.\Scripts\SwitchTestingScenario.ps1 -Scenario frontend
```

### NPM Script Wrappers

Some scripts have convenient NPM wrappers defined in `package.json`:

```bash
npm run init-secrets:interactive    # Initialize GitHub secrets
npm run init-secrets:help          # Show help for GitHub secrets script
npm run update-secrets             # Update secrets configuration
npm run set-user-secrets           # Set .NET user secrets
```

## 📚 Documentation

For detailed documentation on these scripts, see:

- **[DEVELOPMENT_SCRIPTS.md](../docs/DEVELOPMENT_SCRIPTS.md)** - Comprehensive development scripts guide
- **[TESTING_SCENARIOS_GUIDE.md](../docs/TESTING_SCENARIOS_GUIDE.md)** - Testing scenario configuration guide
- **[GITHUB_SECRETS_CONFIGURATION.md](../docs/GITHUB_SECRETS_CONFIGURATION.md)** - GitHub secrets setup guide

## 🛠️ Script Location

These scripts were moved to the `/Scripts` directory as part of repository organization. All documentation and references have been updated to reflect the new location.

### Project-Specific Scripts

Some scripts remain in their original project directories as they are tightly coupled with those projects:

- **`infra/*.ps1` / `infra/*.sh`** - Infrastructure deployment scripts (Azure, Key Vault, Service Principal)
- **`InkStainedWretchFunctions/Testing/*.ps1`** - Testing harness scripts for InkStainedWretchFunctions
- **`InkStainedWretchFunctions/*.ps1`** - Project-specific configuration scripts
- **`OnePageAuthor.DataSeeder/*.ps1` / `*.cmd`** - Cosmos DB emulator startup scripts

## 🔒 Security Notes

- Never commit secrets or sensitive configuration to source control
- Use `.gitignore` to exclude `secrets.json` and similar files
- Use .NET User Secrets for local development secrets
- Use GitHub Secrets for CI/CD secrets
- Always validate script parameters before execution

## 🤝 Contributing

When adding new scripts to this directory:

1. Follow existing naming conventions (PascalCase for PowerShell, kebab-case for shell)
2. Include comprehensive help documentation with `-Help` parameter
3. Update this README with the script's purpose and usage
4. Update relevant documentation files in `/docs`
5. Add NPM wrapper scripts to `package.json` if appropriate
6. Ensure scripts work when run from the repository root directory
