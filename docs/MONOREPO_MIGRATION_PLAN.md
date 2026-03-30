# Monorepo Migration Plan: Ink Stained Wretches

**Status:** Planning  
**Last Updated:** 2026-03-30  
**Target Monorepo:** https://github.com/utdcometsoccer/ink-stained-wretch

---

## Overview

Ink Stained Wretches is preparing to launch as an independent application under the control of an independent legal entity. As part of this transition, three separate repositories must be consolidated into a single monorepo under [https://github.com/utdcometsoccer/ink-stained-wretch](https://github.com/utdcometsoccer/ink-stained-wretch), owned by the new legal entity.

### Repositories to Consolidate

| Repository | Description | Role in Monorepo |
|------------|-------------|------------------|
| [`utdcometsoccer/one-page-author-page-api`](https://github.com/utdcometsoccer/one-page-author-page-api) | This repository — .NET 10 API backend (Azure Functions, Cosmos DB, Stripe, WHMCS, domain registration) | `api/` subdirectory |
| [`utdcometsoccer/ink-stained-wretch`](https://github.com/utdcometsoccer/ink-stained-wretch) | Ink Stained Wretches main application — becomes the **monorepo root** | root (`/`) |
| [`utdcometsoccer/one-page-author-page`](https://github.com/utdcometsoccer/one-page-author-page) | Front-end application | `frontend/` subdirectory |

---

## Team Roles Required

The following roles are needed to execute this migration successfully:

### Legal

- **Corporate Attorney** (Connecticut / favorable U.S. jurisdiction, or Mexico/Canada)
  - Specialization: LLC or C-Corp formation in business-favorable jurisdiction (Delaware, Wyoming, or Connecticut; Mexico S. de R.L. de C.V.; Canada federal corporation)
  - Responsibilities: Form the new legal entity, draft operating/shareholder agreements, advise on intellectual property transfer from the current owner to the new entity, and manage trademark registration for the "Ink Stained Wretches" brand
- **Intellectual Property / Technology Attorney**
  - Responsibilities: IP assignment agreement from current repository owner to new entity, open-source license review, contractor/contributor agreements

### Engineering — Front End

- **React / TypeScript Developer** (1–2 people)
  - Responsibilities: Migrate and adapt the front-end application (`one-page-author-page`) into the monorepo `frontend/` directory; update build tooling; verify the Vite/Next.js/CRA setup runs correctly from the new path
- **UI/UX Designer** (optional but recommended at launch)
  - Responsibilities: Review author-facing UI and branding consistency with the new Ink Stained Wretches identity

### Engineering — Cloud / Infrastructure

- **Azure Architect / Cloud Engineer** (1 person)
  - Responsibilities:
    - Plan the migration to a new Azure tenant owned by the new legal entity
    - Migrate Azure resources (Cosmos DB, Blob Storage, Azure Front Door, DNS zones, App Service/Functions)
    - Configure new Azure AD / Entra ID CIAM (see requirements below)
    - Migrate Application Insights workspaces and alert rules
    - Transfer domain `inkstainedwretches.com` to the new Azure DNS zone and Front Door profile
- **DevOps / GitHub Actions Engineer** (1 person)
  - Responsibilities:
    - Create GitHub Actions workflows in the new monorepo (build, test, lint, deploy for each workspace)
    - Migrate CI/CD secrets to the new GitHub repository owned by the new organization
    - Configure branch protection rules and CODEOWNERS in the new monorepo

### Engineering — Back End

- **.NET Developer** (1–2 people)
  - Responsibilities:
    - Migrate the API (`one-page-author-page-api`) into the `api/` subdirectory of the monorepo while preserving git history (using `git filter-repo` or `git subtree`)
    - Update solution/project paths and references after the move
    - Complete the rename from `OnePageAuthorLib` → `InkStainedWretchesLib` (see [`MIGRATION_CHECKLIST_ONE_PAGE_AUTHOR_TO_INK_STAINED_WRETCHES.md`](MIGRATION_CHECKLIST_ONE_PAGE_AUTHOR_TO_INK_STAINED_WRETCHES.md))
    - Update Stripe keys, WHMCS credentials, and all environment variables to point to the new tenant's resources
    - Register new Entra ID app registrations in the new CIAM tenant

### Version Control Specialists

- **Git Expert** (1 person)
  - Responsibilities:
    - Execute history-preserving repository merges using `git filter-repo` and `git subtree`
    - Handle merge conflicts arising from overlapping path names or root-level files (`.gitignore`, `README.md`, etc.)
    - Document the merge procedure and verify commit provenance
- **GitHub Expert** (1 person)
  - Responsibilities:
    - Transfer or create the new GitHub organization owned by the Ink Stained Wretches legal entity
    - Configure repository settings: branch protection, required status checks, Dependabot, code scanning, secret scanning
    - Migrate GitHub Issues, milestones, and labels from the three source repos to the new monorepo
    - Configure GitHub Environments (`production`, `staging`) and OIDC for Azure deployments
    - Set up GitHub Packages or Container Registry if needed

---

## New Legal Entity & Infrastructure Requirements

Before the monorepo migration is complete, the following must be in place:

### Legal Entity

- [ ] Form new legal entity (recommended: Connecticut LLC or Delaware LLC, or evaluate Mexico/Canada options with legal counsel)
- [ ] Register trademark for "Ink Stained Wretches"
- [ ] Execute IP assignment agreement from current owner to new entity

### Domain & Email

- [ ] Take ownership of `inkstainedwretches.com` (transfer to new entity's domain registrar)
- [ ] Register Microsoft 365 tenant under `inkstainedwretches.com`
- [ ] Create support email addresses (e.g., `support@inkstainedwretches.com`, `hello@inkstainedwretches.com`, `legal@inkstainedwretches.com`)

### New Azure Tenant

- [ ] Register new Azure tenant under the new legal entity
- [ ] Migrate Azure resources (Cosmos DB, Front Door, DNS, Blob Storage, Service Bus, App Insights, Key Vault, Functions)
- [ ] Configure Azure DNS zone for `inkstainedwretches.com`
- [ ] Migrate / recreate Stripe account under new entity and update webhook endpoints

### New Entra ID CIAM

Configure a new Entra External ID (CIAM) tenant that supports all of the following identity providers:

| Provider | Notes |
|----------|-------|
| Google Accounts | OAuth 2.0 / OIDC federation |
| Facebook Accounts | OAuth 2.0 federation |
| Microsoft 365 Accounts | Work/school accounts via Azure AD federation |
| Personal Microsoft Accounts | Live / Outlook / Hotmail |
| Xbox Accounts | Via Microsoft personal accounts |
| BlueSky | AT Protocol OIDC — see [`AT_PROTOCOL_IMPLEMENTATION.md`](AT_PROTOCOL_IMPLEMENTATION.md) |
| Fediverse | ActivityPub OIDC bridge — see [`FEDIVERSE_INTEGRATION.md`](FEDIVERSE_INTEGRATION.md) |

- [ ] Restrict admin portal access to `@inkstainedwretches.com` email addresses via conditional access policy

---

## Migration Strategy: Consolidating Three Repos into the Monorepo

### Approach: `git subtree` with history preservation

The safest approach to preserve commit history from all three repositories is to use `git subtree` (or `git filter-repo` + merge) to re-root each repository under a subdirectory of the target monorepo. All work should be done on a dedicated integration branch (`monorepo/integration`) to avoid disrupting the ongoing launch.

### Target Directory Layout

```
ink-stained-wretch/          ← monorepo root (current ink-stained-wretch repo)
├── api/                     ← contents of one-page-author-page-api
│   ├── OnePageAuthorLib/    (to be renamed InkStainedWretchesLib)
│   ├── InkStainedWretchFunctions/
│   ├── InkStainedWretchStripe/
│   ├── WhmcsWorkerService/
│   ├── infra/
│   ├── docs/
│   └── OnePageAuthorAPI.sln (to be renamed InkStainedWretchesAPI.sln)
├── frontend/                ← contents of one-page-author-page
│   ├── src/
│   ├── public/
│   └── package.json
├── docs/                    ← shared cross-repo documentation
├── .github/
│   └── workflows/           ← monorepo-aware CI/CD workflows
├── README.md
└── .gitignore

```

---

## GitHub Issues for Incremental Consolidation

The following issues should be created in the **target monorepo** at `utdcometsoccer/ink-stained-wretch`. Each issue is self-contained and can be worked independently on the `monorepo/integration` branch. Issues are sequenced to minimize blocking dependencies.

---

### Issue 1: Create `monorepo/integration` branch and scaffold monorepo structure

**Labels:** `monorepo`, `setup`, `git`  
**Assignee:** Git Expert  
**Branch:** `monorepo/integration`

**Description:**

Create the long-lived integration branch and establish the top-level directory structure for the monorepo. This branch will receive all history-merging pull requests and will only be merged to `main` after full validation.

**Acceptance Criteria:**

- [ ] Branch `monorepo/integration` is created from `main` of `ink-stained-wretch`
- [ ] Top-level directories `api/`, `frontend/`, `docs/` are created (with `.gitkeep` placeholders)
- [ ] Root `README.md` is updated to describe the monorepo structure
- [ ] Root `.gitignore` consolidates ignore rules from all three source repos
- [ ] Branch protection: `monorepo/integration` requires 1 approver and passing CI

---

### Issue 2: Import `one-page-author-page-api` history into `api/` subdirectory

**Labels:** `monorepo`, `git`, `backend`  
**Assignee:** Git Expert + .NET Developer  
**Depends on:** Issue 1  
**Branch:** `monorepo/import-api`

**Description:**

Use `git filter-repo` to re-root the entire commit history of `one-page-author-page-api` under `api/`, then merge the rewritten history into `monorepo/integration`.

**Steps:**

```bash
# 1. Clone the API repo
git clone https://github.com/utdcometsoccer/one-page-author-page-api.git api-source
cd api-source

# 2. Rewrite history to prefix all paths with api/
git filter-repo --to-subdirectory-filter api/

# 3. In the monorepo, add the rewritten repo as a remote and merge
cd ../ink-stained-wretch
git checkout monorepo/integration
git remote add api-source ../api-source
git fetch api-source
git merge --allow-unrelated-histories api-source/main -m "chore: import one-page-author-page-api history into api/"
git remote remove api-source

```

**Acceptance Criteria:**

- [ ] All commits from `one-page-author-page-api` are present under `api/` in the monorepo
- [ ] `git log --oneline -- api/` shows full history
- [ ] `api/OnePageAuthorAPI.sln` resolves and `dotnet build` succeeds from `api/`
- [ ] Existing CI tests pass for the API project (`dotnet test OnePageAuthorAPI.sln`)

---

### Issue 3: Import `one-page-author-page` history into `frontend/` subdirectory

**Labels:** `monorepo`, `git`, `frontend`  
**Assignee:** Git Expert + Front End Developer  
**Depends on:** Issue 1  
**Branch:** `monorepo/import-frontend`

**Description:**

Use `git filter-repo` to re-root the commit history of `one-page-author-page` under `frontend/`, then merge into `monorepo/integration`.

**Steps:**

```bash
# 1. Clone the frontend repo
git clone https://github.com/utdcometsoccer/one-page-author-page.git frontend-source
cd frontend-source

# 2. Rewrite history to prefix all paths with frontend/
git filter-repo --to-subdirectory-filter frontend/

# 3. In the monorepo, add the rewritten repo as a remote and merge
cd ../ink-stained-wretch
git checkout monorepo/integration
git remote add frontend-source ../frontend-source
git fetch frontend-source
git merge --allow-unrelated-histories frontend-source/main -m "chore: import one-page-author-page history into frontend/"
git remote remove frontend-source

```

**Acceptance Criteria:**

- [ ] All commits from `one-page-author-page` are present under `frontend/` in the monorepo
- [ ] `git log --oneline -- frontend/` shows full history
- [ ] `npm install && npm run build` succeeds from `frontend/`
- [ ] Existing front-end tests pass

---

### Issue 4: Resolve root-level file conflicts between repos

**Labels:** `monorepo`, `git`, `setup`  
**Assignee:** Git Expert  
**Depends on:** Issues 2 and 3  
**Branch:** `monorepo/resolve-root-conflicts`

**Description:**

After merging both source repositories into `monorepo/integration`, resolve any conflicts or duplications in root-level files that existed in multiple source repos (e.g., `.gitignore`, `README.md`, `LICENSE`, `CODE_OF_CONDUCT.md`, `.editorconfig`).

**Acceptance Criteria:**

- [ ] A single root `.gitignore` covers all three workspaces (Node.js, .NET, any ink-stained-wretch-specific rules)
- [ ] Root `README.md` describes the monorepo and links to `api/`, `frontend/`, and `docs/`
- [ ] `LICENSE` reflects the new legal entity
- [ ] `CODE_OF_CONDUCT.md` is unified (keep the most recent version)
- [ ] No duplicate or conflicting `.editorconfig` / `.vscode` / `.code-workspace` settings

---

### Issue 5: Create monorepo-aware GitHub Actions CI/CD workflows

**Labels:** `monorepo`, `ci-cd`, `github-actions`  
**Assignee:** DevOps / GitHub Actions Engineer  
**Depends on:** Issues 2 and 3  
**Branch:** `monorepo/ci-cd`

**Description:**

Replace or supplement existing per-repo GitHub Actions workflows with monorepo-aware workflows that use path filters to trigger only the relevant jobs when code changes in a specific workspace.

**Workflow files to create/update:**

1. **`.github/workflows/api.yml`** — Triggered on changes to `api/**`:
   - Restore, build, and test the .NET solution
   - Deploy to Azure Functions on merge to `main`
   - Uses OIDC for Azure authentication

2. **`.github/workflows/frontend.yml`** — Triggered on changes to `frontend/**`:
   - `npm install`, lint, test, build
   - Deploy front-end assets to Azure Static Web Apps / CDN on merge to `main`

3. **`.github/workflows/infra.yml`** — Triggered on changes to `api/infra/**`:
   - Validate and deploy Bicep templates to the new Azure tenant

4. **`.github/workflows/ci.yml`** — Full integration check on any PR to `main` or `monorepo/integration`

**Acceptance Criteria:**

- [ ] Path-filtered triggers work (pushing only to `api/` does not trigger `frontend.yml`)
- [ ] All secrets are migrated to the new GitHub organization/repository
- [ ] OIDC workload identity federation is configured for the new Azure tenant
- [ ] CI passes on `monorepo/integration`

---

### Issue 6: Complete API rename — `OnePageAuthorLib` → `InkStainedWretchesLib`

**Labels:** `monorepo`, `refactor`, `backend`  
**Assignee:** .NET Developer  
**Depends on:** Issue 2  
**Branch:** `monorepo/rename-api`

**Description:**

Complete the rename of the API layer from "One Page Author" branding to "Ink Stained Wretches". This follows the existing checklist in [`docs/MIGRATION_CHECKLIST_ONE_PAGE_AUTHOR_TO_INK_STAINED_WRETCHES.md`](MIGRATION_CHECKLIST_ONE_PAGE_AUTHOR_TO_INK_STAINED_WRETCHES.md) (will be at `api/docs/` in the monorepo).

**Key tasks:**

- [ ] Rename `api/OnePageAuthorLib/` → `api/InkStainedWretchesLib/`
- [ ] Rename `api/OnePageAuthor.Test/` → `api/InkStainedWretches.Test/`
- [ ] Rename `api/OnePageAuthor.DataSeeder/` → `api/InkStainedWretches.DataSeeder/`
- [ ] Rename solution file: `OnePageAuthorAPI.sln` → `InkStainedWretchesAPI.sln`
- [ ] Update all `<ProjectReference>` paths in `.csproj` files
- [ ] Replace all `namespace OnePageAuthorLib` → `namespace InkStainedWretchesLib` (~1,734 occurrences)
- [ ] Replace all `using OnePageAuthorLib` → `using InkStainedWretchesLib`
- [ ] Update `package.json` name and author fields
- [ ] Update `.github/copilot-instructions.md` project overview
- [ ] Verify `dotnet build InkStainedWretchesAPI.sln` succeeds
- [ ] Verify all tests pass: `dotnet test InkStainedWretchesAPI.sln`

---

### Issue 7: Migrate Azure resources to new tenant

**Labels:** `infrastructure`, `azure`, `migration`  
**Assignee:** Azure Architect  
**Depends on:** New Azure tenant and legal entity established  
**Branch:** `monorepo/azure-migration`

**Description:**

Migrate all Azure resources from the current tenant to the new Azure tenant owned by the Ink Stained Wretches legal entity. This is an infrastructure-only issue; no code changes are required beyond updating environment variable values.

**Resource inventory to migrate:**

| Resource Type | Current Name | Action |
|---------------|-------------|--------|
| Cosmos DB account | `onepageauthor*` | Export data, recreate in new tenant |
| Azure Blob Storage | `*images*` | Migrate blobs to new storage account |
| Azure Front Door | `*` | Recreate with new `inkstainedwretches.com` domain |
| Azure DNS Zone | `inkstainedwretches.com` | Transfer to new tenant |
| Azure Service Bus | `onepageauthor-bus` | Recreate in new tenant |
| Azure Key Vault | `*` | Export secrets, recreate in new tenant |
| App Insights | `*` | Recreate in new tenant |
| Azure Functions | All four function apps | Redeploy from new CI/CD |

**Acceptance Criteria:**

- [ ] All resources recreated in new tenant under the new subscription
- [ ] Cosmos DB data migrated (no data loss)
- [ ] Blob Storage data migrated
- [ ] DNS resolves correctly for `inkstainedwretches.com` and all subdomains
- [ ] Azure Front Door health probes green
- [ ] All environment variables updated in GitHub Secrets and Azure Key Vault
- [ ] WHMCS IP allowlist updated for new VM static IP

---

### Issue 8: Configure new Entra ID CIAM for Ink Stained Wretches

**Labels:** `infrastructure`, `authentication`, `azure`  
**Assignee:** Azure Architect + .NET Developer  
**Depends on:** Issue 7 (new Azure tenant)  
**Branch:** `monorepo/entra-ciam`

**Description:**

Configure a new Microsoft Entra External ID (CIAM) tenant for `inkstainedwretches.com` supporting all required identity providers.

**Identity Providers to Configure:**

- [ ] **Google** — OAuth 2.0 / OIDC federation (requires Google Cloud project and OAuth credentials)
- [ ] **Facebook** — OAuth 2.0 via Microsoft's Facebook federation in Entra External ID
- [ ] **Microsoft 365 / Work accounts** — Enable organizational accounts
- [ ] **Personal Microsoft Accounts** — Live, Outlook, Hotmail, Xbox
- [ ] **BlueSky (AT Protocol)** — Custom OIDC federation; see [`docs/AT_PROTOCOL_IMPLEMENTATION.md`](AT_PROTOCOL_IMPLEMENTATION.md) (will be at `api/docs/` in the monorepo)
- [ ] **Fediverse (ActivityPub)** — Custom OIDC bridge; see [`docs/FEDIVERSE_INTEGRATION.md`](FEDIVERSE_INTEGRATION.md) (will be at `api/docs/` in the monorepo)

**Admin Access Restriction:**

- [ ] Configure Conditional Access policy to restrict admin portal access to `@inkstainedwretches.com` accounts
- [ ] Assign admin roles only to `@inkstainedwretches.com` users

**App Registrations:**

- [ ] Register new app registrations for each Function App and the front-end SPA
- [ ] Update `AAD_TENANT_ID`, `AAD_AUDIENCE`, and related environment variables
- [ ] Update CORS and redirect URI configurations

**Acceptance Criteria:**

- [ ] Users can sign in with all six identity providers
- [ ] Admin access is restricted to `@inkstainedwretches.com` accounts
- [ ] JWT tokens are accepted by the API function apps
- [ ] Existing tests pass with the new CIAM configuration

---

### Issue 9: Configure Stripe under new legal entity

**Labels:** `infrastructure`, `payments`, `stripe`  
**Assignee:** .NET Developer + Legal  
**Depends on:** New legal entity established  
**Branch:** `monorepo/stripe-migration`

**Description:**

Transfer or create a new Stripe account owned by the Ink Stained Wretches legal entity and update all API keys and webhook endpoints.

**Tasks:**

- [ ] Create new Stripe account under new entity (or transfer existing account with Stripe support)
- [ ] Recreate Stripe Products and Prices in new account (or use `StripeProductManager` tool)
- [ ] Update webhook endpoint URLs to new Azure Functions deployment URL
- [ ] Verify webhook signature validation with new `STRIPE_WEBHOOK_SECRET`
- [ ] Update all Stripe-related GitHub Secrets: `STRIPE_API_KEY`, `STRIPE_WEBHOOK_SECRET`
- [ ] Run integration tests against Stripe test mode

---

### Issue 10: Microsoft 365 tenant setup and support email addresses

**Labels:** `infrastructure`, `email`  
**Assignee:** Azure Architect  
**Depends on:** Domain ownership of `inkstainedwretches.com` (Issue 7)  
**Branch:** n/a (infrastructure-only)

**Description:**

Register a Microsoft 365 tenant under `inkstainedwretches.com` and create the required support email addresses.

**Tasks:**

- [ ] Register Microsoft 365 Business Basic (or appropriate tier) tenant for `inkstainedwretches.com`
- [ ] Verify domain ownership via DNS TXT record
- [ ] Create mailboxes:
  - `support@inkstainedwretches.com`
  - `hello@inkstainedwretches.com`
  - `legal@inkstainedwretches.com`
  - `privacy@inkstainedwretches.com` (recommended for GDPR/privacy policy)
  - `noreply@inkstainedwretches.com` (for transactional email from the platform)
- [ ] Configure SPF, DKIM, DMARC DNS records for email deliverability
- [ ] Update Azure Communication Services sender email (if used) to `noreply@inkstainedwretches.com`

---

### Issue 11: Update `WHMCS_CLIENT_ID` and domain registration configuration for new tenant

**Labels:** `backend`, `whmcs`, `configuration`  
**Assignee:** .NET Developer  
**Depends on:** Issues 7 and 8  
**Branch:** `monorepo/whmcs-update`

**Description:**

Update WHMCS configuration and the `WhmcsWorkerService` VM deployment to point to the new Azure tenant and update IP allowlists after VM migration.

**Tasks:**

- [ ] Update `WHMCS_CLIENT_ID` in the new Key Vault / GitHub Secrets
- [ ] Update `vm.bicep` with any new resource group or subscription references for the new tenant
- [ ] Redeploy `WhmcsWorkerService` VM with new static IP
- [ ] Update WHMCS API IP allowlist with new VM static IP
- [ ] Run `DomainRegistrationTestHarness` against new deployment to verify end-to-end domain registration works
- [ ] Verify Azure Service Bus queue connectivity from new VM

---

### Issue 12: GitHub organization setup and repository transfer

**Labels:** `github`, `setup`  
**Assignee:** GitHub Expert + Legal  
**Depends on:** Legal entity established  
**Branch:** n/a (GitHub administration)

**Description:**

Create or transfer the GitHub organization for the Ink Stained Wretches legal entity and establish the new monorepo under that organization.

**Tasks:**

- [ ] Create new GitHub Organization: `ink-stained-wretches` (or similar)
- [ ] Transfer or fork `utdcometsoccer/ink-stained-wretch` to the new organization
- [ ] Configure organization-level settings:
  - Branch protection rules
  - Required status checks
  - Dependabot alerts and security scanning
  - Secret scanning
- [ ] Migrate GitHub Issues, milestones, and labels from the three source repos
- [ ] Configure GitHub Environments: `production`, `staging`
- [ ] Set up OIDC workload identity for Azure deployments (no long-lived secrets)
- [ ] Restrict admin/owner roles to `@inkstainedwretches.com` email addresses
- [ ] Archive (do not delete) the three source repositories after validation

---

### Issue 13: Documentation consolidation and update

**Labels:** `documentation`, `monorepo`  
**Assignee:** Any developer  
**Depends on:** Issues 2–6  
**Branch:** `monorepo/docs-update`

**Description:**

Consolidate and update documentation across all workspaces after the monorepo integration is complete.

**Tasks:**

- [ ] Create `docs/` at monorepo root for cross-workspace documentation (architecture, onboarding, contributing)
- [ ] Move `api/docs/` to remain in `api/docs/` for API-specific docs
- [ ] Move `frontend/` README and any front-end-specific docs to `frontend/docs/`
- [ ] Update all inter-document links to use new monorepo-relative paths
- [ ] Update root `README.md` with monorepo overview, workspace descriptions, and quick-start instructions
- [ ] Update `CONTRIBUTING.md` with monorepo-specific contribution workflow
- [ ] Update `docs/INDEX.md` to reference the new document structure

---

### Issue 14: End-to-end validation and cut-over

**Labels:** `validation`, `launch`  
**Assignee:** All team leads  
**Depends on:** All previous issues  
**Branch:** merge `monorepo/integration` → `main`

**Description:**

Perform full end-to-end validation of the monorepo in the new tenant before cutting over.

**Validation Checklist:**

- [ ] `dotnet build InkStainedWretchesAPI.sln` succeeds from `api/`
- [ ] `dotnet test InkStainedWretchesAPI.sln` — all tests pass
- [ ] `npm install && npm run build` succeeds from `frontend/`
- [ ] All GitHub Actions workflows pass on `monorepo/integration`
- [ ] Azure Functions deploy and respond to health checks
- [ ] Domain `inkstainedwretches.com` resolves and Front Door serves traffic
- [ ] Author registration flow works end-to-end (Entra ID CIAM → API → Cosmos DB)
- [ ] Stripe checkout and webhook flows work
- [ ] Domain registration (WHMCS) flow works
- [ ] All identity providers (Google, Facebook, Microsoft, BlueSky, Fediverse) authenticate correctly
- [ ] Admin portal access restricted to `@inkstainedwretches.com`
- [ ] No regressions in Application Insights for 48 hours post-cutover

**Cut-over Steps:**

1. Merge `monorepo/integration` → `main` in new organization repo
2. Trigger production deployment
3. Monitor Application Insights for 48 hours
4. Archive source repos (`one-page-author-page-api`, `one-page-author-page`, and the original `ink-stained-wretch`)
5. Update all external links and documentation references

---

## Timeline Estimate

| Phase | Issues | Duration |
|-------|--------|----------|
| Legal entity formation | — | 4–8 weeks (parallel) |
| Branch setup and history imports | 1–4 | 1 week |
| CI/CD and infrastructure | 5, 7 | 2 weeks |
| API rename and Entra CIAM | 6, 8 | 1–2 weeks |
| Stripe, WHMCS, M365 | 9–11 | 1 week |
| GitHub org and docs | 12, 13 | 1 week |
| Validation and cut-over | 14 | 1 week |

**Estimated Total Engineering Time:** 6–8 weeks (some tasks run in parallel)  
**Critical Path:** Legal entity → Azure tenant → Entra CIAM → API rename → End-to-end validation

---

## References

- [`MIGRATION_CHECKLIST_ONE_PAGE_AUTHOR_TO_INK_STAINED_WRETCHES.md`](MIGRATION_CHECKLIST_ONE_PAGE_AUTHOR_TO_INK_STAINED_WRETCHES.md) — Detailed API rename checklist
- [`MIGRATION_ONE_PAGE_AUTHOR_TO_INK_STAINED_WRETCHES.md`](MIGRATION_ONE_PAGE_AUTHOR_TO_INK_STAINED_WRETCHES.md) — Comprehensive migration guide
- [`AT_PROTOCOL_IMPLEMENTATION.md`](AT_PROTOCOL_IMPLEMENTATION.md) — BlueSky integration
- [`FEDIVERSE_INTEGRATION.md`](FEDIVERSE_INTEGRATION.md) — Fediverse integration
- [`WHMCS_INTEGRATION_SUMMARY.md`](WHMCS_INTEGRATION_SUMMARY.md) — WHMCS domain registration
- [`DEPLOYMENT_ARCHITECTURE.md`](DEPLOYMENT_ARCHITECTURE.md) — Current Azure architecture
- [`authentication/ENTRA_CIAM_CONFIGURATION_GUIDE.md`](authentication/ENTRA_CIAM_CONFIGURATION_GUIDE.md) — Entra ID CIAM setup guide
- Source repositories:
  - https://github.com/utdcometsoccer/one-page-author-page-api (this repo)
  - https://github.com/utdcometsoccer/ink-stained-wretch (target monorepo)
  - https://github.com/utdcometsoccer/one-page-author-page (front-end)
