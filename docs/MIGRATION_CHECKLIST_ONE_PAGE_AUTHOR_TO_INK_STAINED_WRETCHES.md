# Migration Checklist: One Page Author → Ink Stained Wretches

**Quick Reference Checklist for Execution**

Use this checklist alongside the comprehensive [Migration Guide](MIGRATION_ONE_PAGE_AUTHOR_TO_INK_STAINED_WRETCHES.md).

---

## Pre-Migration Phase

### Planning

- [ ] Review complete migration guide
- [ ] Get team approval for migration
- [ ] Decide on database naming strategy
  - [ ] **Recommended:** Keep "OnePageAuthorDb" (no data migration)
  - [ ] **Alternative:** Migrate to "InkStainedWretchesDb" (requires data migration plan)
- [ ] Schedule migration window
- [ ] Create migration branch: `git checkout -b migrate-to-ink-stained-wretches`
- [ ] Notify team of upcoming changes
- [ ] Backup current codebase

---

## Phase 1: Solution and Projects

### Solution File

- [ ] Rename: `OnePageAuthorAPI.sln` → `InkStainedWretchesAPI.sln`

  ```bash
  git mv OnePageAuthorAPI.sln InkStainedWretchesAPI.sln
  ```

### Project Directories (Rename in this order)

- [ ] Rename library: `OnePageAuthorLib` → `InkStainedWretchesLib`

  ```bash
  git mv OnePageAuthorLib InkStainedWretchesLib
  ```

- [ ] Rename test project: `OnePageAuthor.Test` → `InkStainedWretches.Test`

  ```bash
  git mv OnePageAuthor.Test InkStainedWretches.Test
  ```

- [ ] Rename data seeder: `OnePageAuthor.DataSeeder` → `InkStainedWretches.DataSeeder`

  ```bash
  git mv OnePageAuthor.DataSeeder InkStainedWretches.DataSeeder
  ```

### Project Files

- [ ] Rename: `InkStainedWretchesLib/OnePageAuthorLib.csproj` → `InkStainedWretchesLib/InkStainedWretchesLib.csproj`
- [ ] Rename: `InkStainedWretches.Test/OnePageAuthor.Test.csproj` → `InkStainedWretches.Test/InkStainedWretches.Test.csproj`
- [ ] Rename: `InkStainedWretches.DataSeeder/OnePageAuthor.DataSeeder.csproj` → `InkStainedWretches.DataSeeder/InkStainedWretches.DataSeeder.csproj`

### Update Solution File Content

- [ ] Update all project paths in `InkStainedWretchesAPI.sln`
- [ ] Update project names in solution file
- [ ] Verify GUIDs are correct

---

## Phase 2: Project References

### Update .csproj Files (20+ projects)

Update `<ProjectReference>` in each file:

- [ ] `InkStainedWretchFunctions/InkStainedWretchFunctions.csproj`
- [ ] `AuthorInvitationTool/AuthorInvitationTool.csproj`
- [ ] `EntraIdRoleManager/EntraIdRoleManager.csproj`
- [ ] `SeedCountries/SeedCountries.csproj`
- [ ] `IntegrationTestAuthorDataService/IntegrationTestAuthorDataService.csproj`
- [ ] `InkStainedWretches.DataSeeder/InkStainedWretches.DataSeeder.csproj`
- [ ] `SeedTestimonials/SeedTestimonials.csproj`
- [ ] `SeedImageStorageTiers/SeedImageStorageTiers.csproj`
- [ ] `SeedInkStainedWretchesLocale/SeedInkStainedWretchesLocale.csproj`
- [ ] `SeedAPIData/SeedAPIData.csproj`
- [ ] `MigrateDomainRegistrationLastUpdatedAt/MigrateDomainRegistrationLastUpdatedAt.csproj`
- [ ] `SeedLanguages/SeedLanguages.csproj`
- [ ] `InkStainedWretchesConfig/InkStainedWretchesConfig.csproj`
- [ ] `AmazonProductTestConsole/AmazonProductTestConsole.csproj`
- [ ] `InkStainedWretchStripe/InkStainedWretchStripe.csproj`
- [ ] `function-app/function-app.csproj`
- [ ] `InkStainedWretches.Test/InkStainedWretches.Test.csproj`
- [ ] `DomainRegistrationTestHarness/DomainRegistrationTestHarness.csproj`
- [ ] `ImageAPI/ImageAPI.csproj`
- [ ] `SeedExperiments/SeedExperiments.csproj`

**Find/Replace Pattern:**

```xml
OLD: <ProjectReference Include="..\OnePageAuthorLib\OnePageAuthorLib.csproj" />
NEW: <ProjectReference Include="..\InkStainedWretchesLib\InkStainedWretchesLib.csproj" />
```

### Update Project Properties in .csproj Files

- [ ] `InkStainedWretchesLib.csproj`: Update `<RootNamespace>`, `<AssemblyName>`, `<DocumentationFile>`
- [ ] `InkStainedWretches.Test.csproj`: Update `<RootNamespace>`, `<AssemblyName>`
- [ ] `InkStainedWretches.DataSeeder.csproj`: Update `<RootNamespace>`, `<AssemblyName>`

---

## Phase 3: C# Code Changes

### Namespace Declarations (~1,734 occurrences)

Use IDE find/replace with case-sensitive matching:

- [ ] `namespace OnePageAuthorLib` → `namespace InkStainedWretchesLib`
- [ ] `namespace OnePageAuthorLib.Api` → `namespace InkStainedWretchesLib.Api`
- [ ] `namespace OnePageAuthorLib.Api.Stripe` → `namespace InkStainedWretchesLib.Api.Stripe`
- [ ] `namespace OnePageAuthorLib.Interfaces` → `namespace InkStainedWretchesLib.Interfaces`
- [ ] `namespace OnePageAuthorLib.Interfaces.Stripe` → `namespace InkStainedWretchesLib.Interfaces.Stripe`
- [ ] `namespace OnePageAuthorAPI.DataSeeder` → `namespace InkStainedWretchesAPI.DataSeeder`
- [ ] `namespace OnePageAuthor.Test` → `namespace InkStainedWretches.Test`
- [ ] `namespace OnePageAuthor.Test.*` → `namespace InkStainedWretches.Test.*`

### Using Statements

- [ ] `using OnePageAuthorLib` → `using InkStainedWretchesLib`
- [ ] `using OnePageAuthorLib.Api` → `using InkStainedWretchesLib.Api`
- [ ] `using OnePageAuthorLib.Interfaces` → `using InkStainedWretchesLib.Interfaces`
- [ ] (And all sub-namespaces)

### Assembly References

- [ ] Update `InkStainedWretchesLib/Properties/AssemblyInfo.cs`:

  ```csharp
  [assembly: InternalsVisibleTo("InkStainedWretches.Test")]
  ```

---

## Phase 4: Configuration Files

### NPM Configuration

- [ ] Update `package.json`:
  - [ ] `"name": "ink-stained-wretches-api"`
  - [ ] `"author": "Ink Stained Wretches Team"`

### VS Code Workspace

- [ ] Update `.code-workspace`:
  - [ ] Library path: `"path": "InkStainedWretchesLib"`
  - [ ] Test path: `"path": "InkStainedWretches.Test"`
  - [ ] Solution name: `"dotnet.defaultSolution": "InkStainedWretchesAPI.sln"`

### GitHub Workflows

- [ ] Update `.github/workflows/main_onepageauthorapi.yml`:
  - [ ] Test command: `dotnet test InkStainedWretchesAPI.sln`
  - [ ] Build references
  - [ ] (Consider renaming file itself)

### Local Settings Examples

- [ ] Review `local.settings.json` for any project name references
- [ ] Review `InkStainedWretchFunctions/Testing/scenario*.local.settings.json`
- [ ] Update `COSMOSDB_DATABASE_ID` documentation (keep value as "OnePageAuthorDb" if decided)

---

## Phase 5: PowerShell Scripts

### Scripts Directory

- [ ] Update `Scripts/Generate-ApiDocumentation.ps1`:
  - [ ] Project name: "InkStainedWretchesLib"
  - [ ] Project path: "./InkStainedWretchesLib"
  - [ ] XML path: "./InkStainedWretchesLib/bin/Debug/net10.0/InkStainedWretchesLib.xml"
  - [ ] Test project references
- [ ] Review other scripts for project name references

---

## Phase 6: Documentation

### Main Documentation

- [ ] Update `README.md`:
  - [ ] Title: "Ink Stained Wretches API Platform"
  - [ ] Project structure section
  - [ ] Build commands: `dotnet build InkStainedWretchesAPI.sln`
  - [ ] Test commands: `dotnet test InkStainedWretchesAPI.sln`
  - [ ] Configuration examples
  - [ ] Repository URLs
  - [ ] Build badges

- [ ] Update `.github/copilot-instructions.md`:
  - [ ] Project overview title
  - [ ] Project structure descriptions
  - [ ] Command examples
  - [ ] Code examples

### Project READMEs

- [ ] `InkStainedWretchesLib/README.md` (rename from OnePageAuthorLib)
- [ ] `InkStainedWretchFunctions/README.md`
- [ ] `IntegrationTestAuthorDataService/README.md`
- [ ] `SeedImageStorageTiers/README.md`
- [ ] `AuthorInvitationTool/README.md`

### Documentation Directory (docs/)

Bulk find/replace in all files (~342 references):

- [ ] "OnePageAuthor API" → "Ink Stained Wretches API"
- [ ] "OnePageAuthorLib" → "InkStainedWretchesLib"
- [ ] "OnePageAuthor.Test" → "InkStainedWretches.Test"
- [ ] "OnePageAuthor.DataSeeder" → "InkStainedWretches.DataSeeder"
- [ ] "OnePageAuthorAPI.sln" → "InkStainedWretchesAPI.sln"
- [ ] "one-page-author-page-api" → "ink-stained-wretches-api"

Key documentation files:

- [ ] `docs/API-Documentation.md`
- [ ] `docs/Complete-System-Documentation.md`
- [ ] `docs/IMPLEMENTATION_SUMMARY.md`
- [ ] All other markdown files in docs/

---

## Phase 7: Build and Test

### Clean Build

- [ ] Clean solution:

  ```bash
  dotnet clean InkStainedWretchesAPI.sln
  ```

- [ ] Remove bin/obj directories if needed:

  ```bash
  find . -type d -name "bin" -o -name "obj" | xargs rm -rf
  ```

### Restore and Build

- [ ] Restore packages:

  ```bash
  dotnet restore InkStainedWretchesAPI.sln
  ```

- [ ] Build solution:

  ```bash
  dotnet build InkStainedWretchesAPI.sln -c Debug
  ```

- [ ] Verify no build errors
- [ ] Check for any warnings that need attention

### Run Tests

- [ ] Run all tests:

  ```bash
  dotnet test InkStainedWretchesAPI.sln --verbosity normal
  ```

- [ ] Verify all tests pass
- [ ] Check test output for any issues

### Integration Tests

- [ ] Run integration tests if available
- [ ] Test local development environment
- [ ] Verify Azure Functions run locally

---

## Phase 8: Repository Updates

### Commit Changes

- [ ] Review all changed files:

  ```bash
  git status
  git diff
  ```

- [ ] Stage all changes:

  ```bash
  git add -A
  ```

- [ ] Commit with descriptive message:

  ```bash
  git commit -m "refactor: migrate from One Page Author to Ink Stained Wretches naming"
  ```

### GitHub Repository

- [ ] Push migration branch:

  ```bash
  git push -u origin migrate-to-ink-stained-wretches
  ```

- [ ] Create pull request
- [ ] Add comprehensive PR description
- [ ] Request code reviews

### Repository Rename (If Approved)

- [ ] **COORDINATE WITH TEAM**
- [ ] Rename GitHub repository: `one-page-author-page-api` → `ink-stained-wretches-api`
- [ ] Update CI/CD secrets if needed
- [ ] Update deployment configurations
- [ ] Notify team of URL change

---

## Phase 9: Deployment

### Pre-Deployment

- [ ] Get PR approval
- [ ] Ensure all tests pass in CI/CD
- [ ] Review deployment plan

### Merge and Deploy

- [ ] Merge PR to main branch
- [ ] Monitor CI/CD pipeline
- [ ] Verify automated deployments succeed
- [ ] Check Azure Function Apps are running

### Verification

- [ ] Test deployed endpoints
- [ ] Verify Stripe integration works
- [ ] Check external API integrations
- [ ] Review application logs
- [ ] Verify Cosmos DB connections

---

## Phase 10: Post-Migration

### Immediate Checks (Day 1)

- [ ] All CI/CD pipelines running successfully
- [ ] Deployed Azure Functions responding
- [ ] No critical errors in Application Insights
- [ ] Documentation links working
- [ ] Team members can build locally

### Team Communication

- [ ] Update team on successful migration
- [ ] Document any issues encountered
- [ ] Update development environment setup instructions
- [ ] Provide support for team members updating local clones

### Short-term Monitoring (Week 1)

- [ ] Monitor application logs daily
- [ ] Check for any unexpected errors
- [ ] Verify external integrations stable
- [ ] Address any reported issues

### Clean-up Tasks

- [ ] Archive old documentation if needed
- [ ] Close migration-related issues
- [ ] Update any external wikis or documentation
- [ ] Document lessons learned

---

## Rollback Procedure (If Needed)

If critical issues are discovered:

1. **Immediate Rollback:**
   - [ ] Revert merge commit on main branch
   - [ ] Redeploy previous version
   - [ ] Notify team

2. **Fix and Retry:**
   - [ ] Document issues found
   - [ ] Fix on migration branch
   - [ ] Re-test thoroughly
   - [ ] Create new PR

3. **Communication:**
   - [ ] Notify stakeholders of rollback
   - [ ] Provide timeline for retry
   - [ ] Document root cause

---

## Success Criteria

Migration is complete when:

- [ ] All projects build without errors
- [ ] All tests pass
- [ ] CI/CD pipelines work
- [ ] Deployed applications function correctly
- [ ] Documentation is accurate and complete
- [ ] Team can develop locally without issues
- [ ] No critical issues in production for 1 week

---

## Notes Section

Use this space to track specific issues, decisions, or observations during migration:

```
Date: ___________
Notes:
```

---

**Checklist Version:** 1.0  
**Last Updated:** January 15, 2026  
**Estimated Duration:** 3-6 business days with 2-3 developers
