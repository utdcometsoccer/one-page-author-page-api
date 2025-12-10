# Version Numbering in GitHub Actions

## Overview

This project uses automated version numbering in the GitHub Actions workflow to ensure all build artifacts have consistent, traceable version information.

## Version Format

The version follows semantic versioning with build metadata:

```
MAJOR.MINOR.BUILD+sha.COMMIT
```

- **MAJOR**: Currently `0` (initial development phase)
- **MINOR**: Incremented manually for feature releases (currently `1`)
- **BUILD**: GitHub Actions run number (automatically incremented)
- **COMMIT**: Short git commit SHA (7 characters)

### Examples

- `0.1.123` - Version number
- `0.1.123+sha.abc1234` - Informational version with commit SHA

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
    MAJOR_VERSION=0
    MINOR_VERSION=1
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

### Manual (Minor Version)

To increment the minor version (e.g., from `0.1.x` to `0.2.x`):

1. Edit `.github/workflows/main_onepageauthorapi.yml`
2. Locate the "Generate Version Number" step
3. Update `MINOR_VERSION` value:
   ```bash
   MINOR_VERSION=2  # Change from 1 to 2
   ```
4. Optionally update `Directory.Build.props` for consistency:
   ```xml
   <VersionPrefix>0.2.0</VersionPrefix>
   ```

### Manual (Major Version)

To increment the major version (e.g., from `0.x.y` to `1.x.y`):

1. Edit `.github/workflows/main_onepageauthorapi.yml`
2. Update `MAJOR_VERSION` in the "Generate Version Number" step
3. Reset `MINOR_VERSION` to 0 if desired
4. Update `Directory.Build.props` accordingly

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
