# Migration Guide: One Page Author → Ink Stained Wretches

## Overview

This document outlines the comprehensive changes needed to migrate all naming from "One Page Author" to "Ink Stained Wretches" throughout the codebase. This is a significant refactoring that affects solutions, projects, namespaces, variables, documentation, and infrastructure.

**⚠️ IMPORTANT:** This migration should be performed carefully with comprehensive testing, as it affects critical infrastructure, database references, and deployed services.

## Scope Summary

Based on code analysis:

- **~1,734 occurrences** of "OnePageAuthor" variants in code
- **~104 occurrences** of "one-page-author" in various files
- **~342 occurrences** in documentation files alone
- **3 project directories** with "OnePageAuthor" in their names
- **3 .csproj files** with "OnePageAuthor" in their names
- **20 projects** reference `OnePageAuthorLib.csproj`
- **Multiple namespaces** in C# code to update

## Migration Categories

### 1. Solution and Project Files

#### 1.1 Solution File

- **File to Rename:**
  - `OnePageAuthorAPI.sln` → `InkStainedWretches.sln` or `InkStainedWretchesAPI.sln`

- **Content Changes:**
  - Update all project references from `OnePageAuthor*` to `InkStainedWretches*`
  - Update project GUIDs if project names change
  - Update solution configurations

#### 1.2 Project Directories to Rename

1. **OnePageAuthorLib/** → **InkStainedWretchesLib/**
   - Core shared library with entities, repositories, services, orchestrators
   - Referenced by 20+ projects

2. **OnePageAuthor.DataSeeder/** → **InkStainedWretches.DataSeeder/**
   - StateProvince and geographical data seeding utility

3. **OnePageAuthor.Test/** → **InkStainedWretches.Test/**
   - Comprehensive unit and integration test project

#### 1.3 Project Files to Rename

1. `OnePageAuthorLib/OnePageAuthorLib.csproj` → `InkStainedWretchesLib/InkStainedWretchesLib.csproj`
2. `OnePageAuthor.DataSeeder/OnePageAuthor.DataSeeder.csproj` → `InkStainedWretches.DataSeeder/InkStainedWretches.DataSeeder.csproj`
3. `OnePageAuthor.Test/OnePageAuthor.Test.csproj` → `InkStainedWretches.Test/InkStainedWretches.Test.csproj`

#### 1.4 Project Reference Updates

All projects that reference `OnePageAuthorLib` need their `.csproj` updated:

- InkStainedWretchFunctions
- AuthorInvitationTool
- EntraIdRoleManager
- SeedCountries
- IntegrationTestAuthorDataService
- OnePageAuthor.DataSeeder (now InkStainedWretches.DataSeeder)
- SeedTestimonials
- SeedImageStorageTiers
- SeedInkStainedWretchesLocale
- SeedAPIData
- MigrateDomainRegistrationLastUpdatedAt
- SeedLanguages
- InkStainedWretchesConfig
- AmazonProductTestConsole
- InkStainedWretchStripe
- function-app
- OnePageAuthor.Test (now InkStainedWretches.Test)
- DomainRegistrationTestHarness
- ImageAPI
- SeedExperiments

**Update pattern:**

```xml
<!-- OLD -->
<ProjectReference Include="..\OnePageAuthorLib\OnePageAuthorLib.csproj" />

<!-- NEW -->
<ProjectReference Include="..\InkStainedWretchesLib\InkStainedWretchesLib.csproj" />
```

### 2. Namespace Changes

#### 2.1 C# Namespace Migrations

**OnePageAuthorLib project namespaces:**

- `OnePageAuthorLib.Api.Stripe` → `InkStainedWretchesLib.Api.Stripe`
- `OnePageAuthorLib.Interfaces.Stripe` → `InkStainedWretchesLib.Interfaces.Stripe`
- `OnePageAuthorLib.*` (all namespaces) → `InkStainedWretchesLib.*`

**OnePageAuthor.DataSeeder project:**

- `OnePageAuthorAPI.DataSeeder` → `InkStainedWretchesAPI.DataSeeder`

**OnePageAuthor.Test project:**

- `OnePageAuthor.Test` → `InkStainedWretches.Test`
- `OnePageAuthor.Test.*` → `InkStainedWretches.Test.*`

#### 2.2 Using Statement Updates

All `using` statements referencing old namespaces need updates:

```csharp
// OLD
using OnePageAuthorLib.Api.Stripe;
using OnePageAuthorLib.Interfaces.Stripe;
using OnePageAuthorLib.Services;

// NEW
using InkStainedWretchesLib.Api.Stripe;
using InkStainedWretchesLib.Interfaces.Stripe;
using InkStainedWretchesLib.Services;
```

#### 2.3 Assembly References

Update in `OnePageAuthorLib/Properties/AssemblyInfo.cs`:

```csharp
// OLD
[assembly: InternalsVisibleTo("OnePageAuthor.Test")]

// NEW
[assembly: InternalsVisibleTo("InkStainedWretches.Test")]
```

### 3. Configuration and Environment Variables

#### 3.1 Cosmos DB Database Name

**⚠️ CRITICAL - Database Migration Decision Required**

Current references to `"OnePageAuthor"` or `"OnePageAuthorDb"` in:

- Environment variable: `COSMOSDB_DATABASE_ID`
- Local settings files
- Documentation examples
- Infrastructure templates

**Options:**

1. **Keep existing database name** (recommended for production):
   - Update documentation to reflect that database retains "OnePageAuthor" name
   - Only update variable names and code references
   - Avoids data migration and downtime

2. **Migrate database name** (complex, requires data migration):
   - Requires creating new database with "InkStainedWretches" name
   - Must migrate all data from old to new database
   - Requires coordinated deployment and connection string updates
   - Significant downtime or dual-write period needed

**Recommended Approach:** Keep database name as "OnePageAuthorDb" but update all code references and documentation to clarify this is historical naming.

#### 3.2 Local Settings Files

Update these files (if they reference project names):

- `local.settings.json`
- `InkStainedWretchFunctions/Testing/scenario*.local.settings.json`
- Any project-specific settings files

### 4. GitHub and Repository Configuration

#### 4.1 GitHub Workflow Files

**File:** `.github/workflows/main_onepageauthorapi.yml`

**Changes needed:**

- Workflow file name: `main_onepageauthorapi.yml` → `main_inkstainedwretches.yml` or `main_inkstainedwretchesapi.yml`
- Update test command: `dotnet test OnePageAuthorAPI.sln` → `dotnet test InkStainedWretchesAPI.sln`
- Update any references to old project names

#### 4.2 Repository Name

**Current:** `utdcometsoccer/one-page-author-page-api`

**Proposed:** `utdcometsoccer/ink-stained-wretches-api` or similar

**⚠️ Note:** Changing the repository name on GitHub will:

- Update all issue and PR URLs
- Require updating all local clones
- Require updating CI/CD configurations
- Require updating documentation URLs
- GitHub provides automatic redirects, but explicit updates are better

**Files containing repository URLs:**

- README.md
- Multiple documentation files in `docs/`
- Project-specific README files
- .github/workflows/*.yml files

**Update pattern:**

```markdown
<!-- OLD -->
https://github.com/utdcometsoccer/one-page-author-page-api

<!-- NEW -->
https://github.com/utdcometsoccer/ink-stained-wretches-api
```

### 5. NPM and Package Configuration

#### 5.1 package.json

**File:** `package.json`

**Changes:**

```json
{
  "name": "ink-stained-wretches-api",  // OLD: "one-page-author-api"
  "description": "NPM wrappers for PowerShell development scripts",
  "author": "Ink Stained Wretches Team",  // OLD: "OnePageAuthor Team"
  // ... rest of configuration
}
```

### 6. Visual Studio Code Configuration

#### 6.1 Workspace File

**File:** `.code-workspace`

**Changes:**

```json
{
  "folders": [
    { "name": "Repository", "path": "." },
    { "name": "Library", "path": "InkStainedWretchesLib" },  // OLD: "OnePageAuthorLib"
    { "name": "Function App", "path": "function-app" },
    { "name": "Stripe Functions", "path": "InkStainedWretchStripe" },
    { "name": "Unit Tests", "path": "InkStainedWretches.Test" },  // OLD: "OnePageAuthor.Test"
    // ... other folders
  ],
  "settings": {
    "dotnet.defaultSolution": "InkStainedWretchesAPI.sln",  // OLD: "OnePageAuthorAPI.sln"
    // ... other settings
  }
}
```

### 7. Documentation Updates

#### 7.1 Main README.md

**Changes needed:**

- Title: "OnePageAuthor API Platform" → "Ink Stained Wretches API Platform"
- All references to OnePageAuthor projects and libraries
- Build/test commands referencing solution file
- Examples showing `COSMOSDB_DATABASE_ID`
- Project structure descriptions
- Repository URLs and badges

#### 7.2 Copilot Instructions

**File:** `.github/copilot-instructions.md`

**Changes:**

- Update project overview title
- Update all project structure references
- Update coding guideline examples
- Update command examples

#### 7.3 Project-Specific READMEs

Files to update:

- `OnePageAuthorLib/README.md` → `InkStainedWretchesLib/README.md`
- `InkStainedWretchFunctions/README.md`
- `IntegrationTestAuthorDataService/README.md`
- `SeedImageStorageTiers/README.md`
- `AuthorInvitationTool/README.md`
- Any other project README files

#### 7.4 Documentation Directory (docs/)

**~342 references** to update across documentation files including:

- API-Documentation.md
- Complete-System-Documentation.md
- IMPLEMENTATION_SUMMARY*.md files
- Configuration documentation
- Deployment guides
- Testing guides
- Quick reference guides
- All other markdown files in `docs/`

**Search and replace patterns:**

- "OnePageAuthor API" → "Ink Stained Wretches API"
- "OnePageAuthorLib" → "InkStainedWretchesLib"
- "OnePageAuthor.Test" → "InkStainedWretches.Test"
- "OnePageAuthor.DataSeeder" → "InkStainedWretches.DataSeeder"
- "OnePageAuthorAPI.sln" → "InkStainedWretchesAPI.sln"
- "one-page-author-page-api" → "ink-stained-wretches-api"

### 8. PowerShell Scripts

#### 8.1 Scripts Directory

**Files to update:**

- `Scripts/Generate-ApiDocumentation.ps1`
  - Update project names and paths
  - Update XML documentation paths
- Any other scripts referencing project names

**Example updates in Generate-ApiDocumentation.ps1:**

```powershell
# OLD
Name = "OnePageAuthorLib"
Path = "./OnePageAuthorLib"
XmlPath = "./OnePageAuthorLib/bin/Debug/net9.0/OnePageAuthorLib.xml"

# NEW
Name = "InkStainedWretchesLib"
Path = "./InkStainedWretchesLib"
XmlPath = "./InkStainedWretchesLib/bin/Debug/net9.0/InkStainedWretchesLib.xml"
```

### 9. Infrastructure and Deployment

#### 9.1 Azure Function App Names

Current naming pattern already uses "InkStainedWretch*" for deployed resources:

- `${ISW_BASE_NAME}-imageapi`
- `${ISW_BASE_NAME}-functions`
- `${ISW_BASE_NAME}-stripe`
- `${ISW_BASE_NAME}-config`
- `function-app` (legacy, may need review)

**Review needed:**

- Decide if `function-app` should be renamed to fit new naming pattern
- Ensure all infrastructure references are consistent

#### 9.2 Bicep Templates

**Directory:** `infra/`

**Files to review:**

- Check if any templates have hardcoded "OnePageAuthor" references
- Review parameter names and descriptions
- Update any comments or documentation

### 10. Build and Test Configuration

#### 10.1 Build Commands

All build/test commands need solution name update:

```bash
# OLD
dotnet build OnePageAuthorAPI.sln -c Debug
dotnet test OnePageAuthorAPI.sln -c Debug
dotnet test OnePageAuthorAPI.sln --collect:"XPlat Code Coverage"

# NEW
dotnet build InkStainedWretchesAPI.sln -c Debug
dotnet test InkStainedWretchesAPI.sln -c Debug
dotnet test InkStainedWretchesAPI.sln --collect:"XPlat Code Coverage"
```

#### 10.2 Directory.Build.props and Directory.Build.targets

Check these files for any project-specific references that need updating.

## Migration Execution Plan

### Phase 1: Planning and Preparation (Non-Breaking)

1. ✅ Create this migration documentation
2. Review and approve migration approach
3. Decide on database naming strategy
4. Plan deployment coordination
5. Create migration branch
6. Notify team of upcoming changes

### Phase 2: Code Changes (Breaking Changes Begin)

1. **Rename solution file**
   - Rename `OnePageAuthorAPI.sln` → `InkStainedWretchesAPI.sln`

2. **Rename project directories** (in order to minimize broken references):

   ```bash
   # Rename main library first
   git mv OnePageAuthorLib InkStainedWretchesLib
   
   # Rename test project
   git mv OnePageAuthor.Test InkStainedWretches.Test
   
   # Rename data seeder
   git mv OnePageAuthor.DataSeeder InkStainedWretches.DataSeeder
   ```

3. **Rename project files within directories**

   ```bash
   # In each renamed directory
   git mv InkStainedWretchesLib/OnePageAuthorLib.csproj InkStainedWretchesLib/InkStainedWretchesLib.csproj
   git mv InkStainedWretches.Test/OnePageAuthor.Test.csproj InkStainedWretches.Test/InkStainedWretches.Test.csproj
   git mv InkStainedWretches.DataSeeder/OnePageAuthor.DataSeeder.csproj InkStainedWretches.DataSeeder/InkStainedWretches.DataSeeder.csproj
   ```

4. **Update solution file** to reference new project paths and names

5. **Update all .csproj files** with new ProjectReference paths

6. **Update all C# namespaces**
   - Use find/replace in IDE with case-sensitive matching
   - Update namespace declarations
   - Update using statements
   - Update AssemblyInfo.cs

7. **Update project file properties** (RootNamespace, AssemblyName, DocumentationFile paths)

### Phase 3: Configuration and Scripts

1. Update package.json
2. Update .code-workspace
3. Update PowerShell scripts
4. Update GitHub workflow files (but don't rename yet)
5. Update local.settings.json examples
6. Review and update environment variable documentation

### Phase 4: Documentation

1. Update README.md
2. Update copilot-instructions.md
3. Update all project-specific README files
4. Update all documentation in docs/ directory
5. Update any inline code comments referencing old names

### Phase 5: Build and Test

1. Clean solution: `dotnet clean InkStainedWretchesAPI.sln`
2. Restore packages: `dotnet restore InkStainedWretchesAPI.sln`
3. Build solution: `dotnet build InkStainedWretchesAPI.sln -c Debug`
4. Run all tests: `dotnet test InkStainedWretchesAPI.sln --verbosity normal`
5. Verify all projects compile successfully
6. Run integration tests if available

### Phase 6: Repository and Deployment

1. Commit all changes with clear commit message
2. Push to migration branch
3. Create pull request with comprehensive description
4. Code review and approval
5. **COORDINATE**: Rename GitHub repository (if approved)
6. **COORDINATE**: Update CI/CD secrets and configurations
7. Merge to main branch
8. Update all local clones and documentation links
9. Monitor deployments and verify functionality

## Risk Assessment and Mitigation

### High-Risk Areas

1. **Database Name Changes**
   - **Risk:** Data loss, service outage
   - **Mitigation:** Keep existing database name, only update code references

2. **Azure Function Deployments**
   - **Risk:** Broken deployments, connection string issues
   - **Mitigation:** Test in development environment first, have rollback plan

3. **Repository Rename**
   - **Risk:** Broken links, CI/CD failures
   - **Mitigation:** GitHub provides redirects, update all known references explicitly

### Medium-Risk Areas

1. **Namespace Changes**
   - **Risk:** Compilation failures, missing references
   - **Mitigation:** Comprehensive testing, ensure all using statements updated

2. **Project References**
   - **Risk:** Build failures
   - **Mitigation:** Update solution file and all .csproj files systematically

### Testing Requirements

1. Unit tests must pass after all changes
2. Integration tests must pass
3. Local development environment must build and run
4. Documentation examples must be accurate
5. CI/CD pipelines must execute successfully

## Rollback Plan

If critical issues are discovered:

1. Keep migration branch separate from main
2. Do not merge until fully tested
3. If merged and issues found:
   - Revert merge commit
   - Fix issues in migration branch
   - Re-test before second merge attempt

## Post-Migration Tasks

### Immediate (Day 1)

- [ ] Verify all CI/CD pipelines run successfully
- [ ] Verify deployed Azure Functions work correctly
- [ ] Check all documentation links work
- [ ] Verify local development setup for team members

### Short-term (Week 1)

- [ ] Monitor application logs for any unexpected errors
- [ ] Verify external integrations (Stripe, APIs) still work
- [ ] Update any external documentation or wikis
- [ ] Notify users of any URL changes

### Medium-term (Month 1)

- [ ] Review and close migration-related issues
- [ ] Archive old documentation if needed
- [ ] Update any training materials
- [ ] Complete comprehensive regression testing

## Checklist Summary

### Pre-Migration

- [ ] Review and approve migration plan
- [ ] Decide on database naming strategy
- [ ] Create migration branch
- [ ] Notify team

### Code Changes

- [ ] Rename solution file
- [ ] Rename 3 project directories
- [ ] Rename 3 project files
- [ ] Update solution file content
- [ ] Update 20+ project references
- [ ] Update all C# namespaces (~1,734 occurrences)
- [ ] Update AssemblyInfo.cs

### Configuration

- [ ] Update package.json
- [ ] Update .code-workspace
- [ ] Update PowerShell scripts
- [ ] Update GitHub workflows
- [ ] Update local.settings.json examples

### Documentation

- [ ] Update README.md
- [ ] Update copilot-instructions.md
- [ ] Update project READMEs
- [ ] Update docs/ directory (~342 references)
- [ ] Update repository URLs

### Testing

- [ ] Clean and rebuild solution
- [ ] Run all unit tests
- [ ] Run integration tests
- [ ] Verify local development
- [ ] Test CI/CD pipeline

### Deployment

- [ ] Commit changes
- [ ] Create pull request
- [ ] Code review
- [ ] Rename repository (if approved)
- [ ] Merge to main
- [ ] Monitor deployments

### Post-Migration

- [ ] Verify production functionality
- [ ] Update team development environments
- [ ] Monitor for issues
- [ ] Update external documentation

## Estimated Effort

- **Planning:** 2-4 hours
- **Code Changes:** 8-16 hours
- **Testing:** 4-8 hours
- **Documentation:** 4-8 hours
- **Deployment Coordination:** 2-4 hours
- **Post-Migration Verification:** 2-4 hours

**Total Estimated Effort:** 22-44 hours (3-6 business days)

**Recommended Team Size:** 2-3 developers for parallel work and code review

## Conclusion

This migration is substantial but straightforward if executed systematically. The key to success is:

1. Thorough planning and team coordination
2. Systematic execution following the phases outlined
3. Comprehensive testing at each stage
4. Clear communication with all stakeholders
5. Having a rollback plan ready

The migration aligns the codebase naming with the already-established "Ink Stained Wretches" branding used in deployed infrastructure and documentation.

---

**Document Version:** 1.0  
**Last Updated:** January 15, 2026  
**Status:** Planning Phase
