# Version Numbering in GitHub Actions

## Overview

This project uses automated version numbering in the GitHub Actions workflow to ensure all build artifacts have consistent, traceable version information.

## Version Format

The version follows semantic versioning with build metadata:

```
MAJOR.MINOR.BUILD+sha.COMMIT
```

- **MAJOR**: Increments yearly (years since 2025, starts at `0`)
- **MINOR**: Current month number (1-12), resets to 1 each January
- **BUILD**: GitHub Actions run number (automatically incremented)
- **COMMIT**: Short git commit SHA (7 characters)

### Examples

- `0.12.123` - Version number (Year 2025, December, build 123)
- `0.12.123+sha.abc1234` - Informational version with commit SHA
- `1.3.456` - Version number (Year 2026, March, build 456)

## Implementation

### Directory.Build.props

A centralized `Directory.Build.props` file at the repository root defines default version properties for all projects:

```xml
<PropertyGroup>
  <VersionPrefix>0.1.0</VersionPrefix>
  <AssemblyVersion>0.1.0.0</AssemblyVersion>
  <FileVersion>0.1.0.0</FileVersion>
  <InformationalVersion>0.1.0-local</InformationalVersion>
  <Deterministic>true</Deterministic>
  <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
</PropertyGroup>
```

### GitHub Actions Workflow

The workflow generates version numbers in the "Generate Version Number" step:

```yaml
- name: 'Generate Version Number'
  id: version
  shell: bash
  run: |
    # Calculate major version based on years since project start (2025)
    BASE_YEAR=2025
    CURRENT_YEAR=$(date +%Y)
    MAJOR_VERSION=$((CURRENT_YEAR - BASE_YEAR))
    
    # Calculate minor version based on current month (1-12)
    CURRENT_MONTH=$(date +%-m)
    MINOR_VERSION=$CURRENT_MONTH
    
    BUILD_NUMBER=${{ github.run_number }}
    VERSION="${MAJOR_VERSION}.${MINOR_VERSION}.${BUILD_NUMBER}"
    SHORT_SHA=$(git rev-parse --short HEAD)
    INFORMATIONAL_VERSION="${VERSION}+sha.${SHORT_SHA}"
    
    echo "VERSION=${VERSION}" >> $GITHUB_ENV
    echo "INFORMATIONAL_VERSION=${INFORMATIONAL_VERSION}" >> $GITHUB_ENV
```

### Build Commands

All `dotnet build` and `dotnet publish` commands include version parameters:

```bash
dotnet build --configuration Release \
  /p:Version=${{ env.VERSION }} \
  /p:InformationalVersion=${{ env.INFORMATIONAL_VERSION }}
```

## Local Development

When building locally without GitHub Actions, the default version from `Directory.Build.props` is used:

```bash
# Uses default version 0.1.0-local
dotnet build --configuration Release

# Override version for testing
dotnet build --configuration Release \
  /p:Version=0.1.999 \
  /p:InformationalVersion=0.1.999+sha.test123
```

## Version Properties

| Property | Description | Example |
|----------|-------------|---------|
| `Version` | Package/assembly version | `0.1.123` |
| `AssemblyVersion` | .NET assembly version (binary compatibility) | `0.1.0.0` |
| `FileVersion` | File version (Windows file properties) | `0.1.123.0` |
| `InformationalVersion` | Full version with metadata | `0.1.123+sha.abc1234` |

## Incrementing Version Numbers

### Automatic (Build Number)

The build number is automatically incremented with each GitHub Actions workflow run. No manual intervention is required.

### Automatic (Minor Version - Monthly)

The minor version is automatically calculated based on the current month (1-12):
- January = 1
- February = 2
- ...
- December = 12

The minor version automatically resets to 1 each January.

### Automatic (Major Version - Yearly)

The major version is automatically calculated based on years since the project start (2025):
- 2025 = 0
- 2026 = 1
- 2027 = 2
- and so on...

No manual intervention is required. The version automatically updates based on the current date when the workflow runs.

## Benefits

1. **Traceability**: Every build can be traced back to a specific commit and workflow run
2. **Consistency**: All function apps in the solution share the same version
3. **Automation**: No manual version management required
4. **Semantic Versioning**: Clear indication of development stage (0.x) and feature releases
5. **Build Metadata**: Git SHA provides additional traceability

## Troubleshooting

### Version not appearing in build output

Ensure the `Directory.Build.props` file is in the repository root and that build commands include the `/p:Version` and `/p:InformationalVersion` parameters.

### Local builds show different version

This is expected. Local builds use the default version from `Directory.Build.props` unless explicitly overridden with `/p:Version` parameters.

### Version conflicts

If you see version conflicts or binding redirect warnings, check that:
- `AssemblyVersion` remains stable (only change for breaking changes)
- `Version` and `FileVersion` are properly set in builds

## Related Files

- `.github/workflows/main_onepageauthorapi.yml` - Workflow with version generation
- `Directory.Build.props` - Centralized version properties
- Individual `.csproj` files - Project files that inherit version properties
