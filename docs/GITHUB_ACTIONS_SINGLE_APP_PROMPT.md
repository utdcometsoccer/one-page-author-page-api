# GitHub Actions Rebuild Prompt — Single Function App Architecture

*Generated: 2026-03-04*

This document contains a GitHub Copilot prompt to rebuild the GitHub Actions CI/CD workflow for the **consolidated single Function App architecture**. The new workflow replaces the current `main_onepageauthorapi.yml` which builds and deploys four separate Function App projects.

---

## Current Architecture vs. Target Architecture

### Current (multi-app)
| Step | Projects deployed |
|------|-----------------|
| Build | `function-app`, `ImageAPI`, `InkStainedWretchFunctions`, `InkStainedWretchStripe`, `InkStainedWretchesConfig`, `WhmcsWorkerService` |
| Deploy | 5 separate Azure Function Apps + 1 VM service |
| Secrets | 60+ GitHub secrets |

### Target (single Function App)
| Step | Projects deployed |
|------|-----------------|
| Build | `InkStainedWretchFunctions` (consolidated), `WhmcsWorkerService` |
| Deploy | 1 Azure Function App + 1 VM service |
| Secrets | Streamlined set |

---

## Rebuild Prompt

```
Rebuild the GitHub Actions workflow at
`.github/workflows/main_onepageauthorapi.yml`
for the new consolidated single Function App architecture.

The new architecture has ONE Azure Function App (`InkStainedWretchFunctions`)
that contains all HTTP endpoints, Service Bus queue processors, and Cosmos DB
change-feed triggers previously spread across four projects.

The `WhmcsWorkerService` VM deployment is unchanged.

TRIGGER
=======
on:
  push:
    branches: [main]
  workflow_dispatch:

ENVIRONMENT VARIABLES (top-level env block)
===========================================
DOTNET_VERSION: '10.0.x'
FUNCTION_APP_PATH: 'InkStainedWretchFunctions'
WHMCS_WORKER_SERVICE_PATH: 'WhmcsWorkerService'

JOBS
====
Use a single job: build-and-deploy
runs-on: ubuntu-latest

STEPS (in order)
================

── STEP 1: Checkout ──────────────────────────────────────────
- name: 'Checkout GitHub Action'
  uses: actions/checkout@v4

── STEP 2: Setup .NET ────────────────────────────────────────
- name: Setup DotNet ${{ env.DOTNET_VERSION }} Environment
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: ${{ env.DOTNET_VERSION }}

── STEP 3: Generate Version Number ───────────────────────────
Same version scheme as the current workflow:
  MAJOR = (current year - 2025)
  MINOR = current month (1-12)
  BUILD = ${{ github.run_number }}
  VERSION = MAJOR.MINOR.BUILD
  INFORMATIONAL_VERSION = VERSION+sha.SHORT_SHA

Export VERSION, INFORMATIONAL_VERSION, BUILD_NUMBER to GITHUB_ENV.

── STEP 4: Run Unit Tests ────────────────────────────────────
- name: 'Run Unit Tests'
  run: dotnet test OnePageAuthorAPI.sln --configuration Release --verbosity normal --logger "console;verbosity=detailed"

── STEP 5: Build & Publish InkStainedWretchFunctions ─────────
- name: 'Build and Publish InkStainedWretchFunctions'
  run: |
    pushd '${{ env.FUNCTION_APP_PATH }}'
    dotnet build --configuration Release /p:Version=${{ env.VERSION }} /p:InformationalVersion=${{ env.INFORMATIONAL_VERSION }}
    dotnet publish --configuration Release --output ./output /p:Version=${{ env.VERSION }} /p:InformationalVersion=${{ env.INFORMATIONAL_VERSION }}
    popd

- name: 'Zip InkStainedWretchFunctions Output'
  run: |
    cd '${{ env.FUNCTION_APP_PATH }}/output'
    zip -r ../inkstainedwretchfunctions.zip .
    cd ../..

── STEP 6: Build & Publish WhmcsWorkerService ────────────────
- name: 'Build and Publish WhmcsWorkerService'
  run: |
    pushd '${{ env.WHMCS_WORKER_SERVICE_PATH }}'
    dotnet publish --configuration Release \
      -r linux-x64 \
      --self-contained false \
      --output ./output \
      /p:Version=${{ env.VERSION }} \
      /p:InformationalVersion=${{ env.INFORMATIONAL_VERSION }}
    popd

- name: 'Zip WhmcsWorkerService Output'
  run: |
    cp '${{ env.WHMCS_WORKER_SERVICE_PATH }}/whmcs-worker.service' \
       '${{ env.WHMCS_WORKER_SERVICE_PATH }}/output/'
    cd '${{ env.WHMCS_WORKER_SERVICE_PATH }}/output'
    zip -r ../whmcsworker.zip .
    cd ../..

── STEP 7: Azure Login ───────────────────────────────────────
- name: 'Login to Azure'
  continue-on-error: true
  uses: azure/login@v2
  with:
    creds: ${{ secrets.AZURE_CREDENTIALS }}

── STEP 8: Deploy Infrastructure (Conditional) ───────────────
Provide separate conditional steps for each infrastructure component.
Each step checks whether required secrets are present before deploying.
If secrets are absent, print a clear ⚠️ message and exit 0.

8a. Deploy Cosmos DB Account (conditional)
    Required secrets: COSMOSDB_RESOURCE_GROUP, COSMOSDB_ACCOUNT_NAME, COSMOSDB_LOCATION
    Template: infra/cosmosdb.bicep
    Skip if account already exists.
    continue-on-error: true

8b. Deploy Application Insights (conditional)
    Required secrets: COSMOSDB_RESOURCE_GROUP, APPINSIGHTS_NAME, COSMOSDB_LOCATION
    Template: infra/applicationinsights.bicep
    Skip if resource already exists.
    continue-on-error: true

8c. Deploy Azure Service Bus (conditional)
    Required secrets: ISW_RESOURCE_GROUP, WHMCS_SB_NAMESPACE_NAME (or ISW_BASE_NAME)
    Template: infra/servicebus.bicep
    IMPORTANT: Namespace name must NOT end in "-sb" (Azure reserved suffix).
    Use ISW_BASE_NAME + "-bus" as the namespace name if WHMCS_SB_NAMESPACE_NAME is not set.
    Skip if namespace already exists.
    Capture deployment outputs: listenerConnectionString, senderConnectionString
    continue-on-error: true

8d. Deploy Azure Notification Hub (conditional) [NEW]
    Required secrets: ISW_RESOURCE_GROUP, ISW_BASE_NAME, ISW_LOCATION
    Template: infra/notificationhub.bicep
    Create/update an Azure Notification Hub namespace named ISW_BASE_NAME + "-nh"
    and a hub named "onepageauthor-hub".
    Skip if namespace already exists.
    continue-on-error: true

8e. Deploy consolidated Function App infrastructure (conditional)
    Required secrets: ISW_RESOURCE_GROUP, ISW_BASE_NAME, ISW_LOCATION
    Template: infra/functionapp.bicep (single app)
    Function App name: ISW_BASE_NAME (or ISW_BASE_NAME + "-api")
    Skip if Function App already exists.
    continue-on-error: true

8f. Deploy WHMCS Worker VM infrastructure (conditional, unchanged from current)
    Required secrets: WHMCS_WORKER_RESOURCE_GROUP, WHMCS_WORKER_SSH_PUBLIC_KEY,
                     DEPLOY_WHMCS_WORKER (must be "true")
    Template: infra/vm.bicep
    Skip if VM already exists.
    continue-on-error: true

── STEP 9: Deploy InkStainedWretchFunctions ──────────────────
- name: 'Deploy InkStainedWretchFunctions'
  continue-on-error: true
  env:
    ISW_RESOURCE_GROUP: ${{ secrets.ISW_RESOURCE_GROUP }}
    ISW_BASE_NAME: ${{ secrets.ISW_BASE_NAME }}
    DEPLOY_ISW_FUNCTIONS: ${{ secrets.DEPLOY_ISW_FUNCTIONS }}
  run: |
    if [ "$DEPLOY_ISW_FUNCTIONS" != "true" ] || [ -z "$ISW_RESOURCE_GROUP" ] || [ -z "$ISW_BASE_NAME" ]; then
      echo "⚠️ Skipping InkStainedWretchFunctions deployment"
      exit 0
    fi

    FUNCTION_APP_NAME="${ISW_BASE_NAME}"
    echo "🚀 Deploying InkStainedWretchFunctions to: $FUNCTION_APP_NAME"
    az functionapp deployment source config-zip \
      --name "$FUNCTION_APP_NAME" \
      --resource-group "$ISW_RESOURCE_GROUP" \
      --src '${{ env.FUNCTION_APP_PATH }}/inkstainedwretchfunctions.zip'
    echo "✓ Deployed successfully"

── STEP 10: Configure InkStainedWretchFunctions App Settings ─
After deployment, apply ALL app settings in one az functionapp config appsettings set call.
This avoids multiple restarts and is more efficient.

- name: 'Configure InkStainedWretchFunctions App Settings'
  continue-on-error: true
  env:
    ISW_RESOURCE_GROUP: ${{ secrets.ISW_RESOURCE_GROUP }}
    ISW_BASE_NAME: ${{ secrets.ISW_BASE_NAME }}
    DEPLOY_ISW_FUNCTIONS: ${{ secrets.DEPLOY_ISW_FUNCTIONS }}
    # Identity
    AAD_TENANT_ID: ${{ secrets.AAD_TENANT_ID }}
    AAD_AUDIENCE: ${{ secrets.AAD_AUDIENCE }}
    AAD_CLIENT_ID: ${{ secrets.AAD_CLIENT_ID }}
    AAD_AUTHORITY: ${{ secrets.AAD_AUTHORITY }}
    AAD_VALID_ISSUERS: ${{ secrets.AAD_VALID_ISSUERS }}
    OPEN_ID_CONNECT_METADATA_URL: ${{ secrets.OPEN_ID_CONNECT_METADATA_URL }}
    # Cosmos DB
    COSMOSDB_ENDPOINT_URI: ${{ secrets.COSMOSDB_ENDPOINT_URI }}
    COSMOSDB_PRIMARY_KEY: ${{ secrets.COSMOSDB_PRIMARY_KEY }}
    COSMOSDB_DATABASE_ID: ${{ secrets.COSMOSDB_DATABASE_ID }}
    COSMOSDB_CONNECTION_STRING: ${{ secrets.COSMOSDB_CONNECTION_STRING }}
    # Stripe
    STRIPE_API_KEY: ${{ secrets.STRIPE_API_KEY }}
    STRIPE_WEBHOOK_SECRET: ${{ secrets.STRIPE_WEBHOOK_SECRET }}
    # Blob Storage (for image uploads)
    AZURE_STORAGE_CONNECTION_STRING: ${{ secrets.AZURE_STORAGE_CONNECTION_STRING }}
    # WHMCS
    WHMCS_API_URL: ${{ secrets.WHMCS_API_URL }}
    WHMCS_API_IDENTIFIER: ${{ secrets.WHMCS_API_IDENTIFIER }}
    WHMCS_API_SECRET: ${{ secrets.WHMCS_API_SECRET }}
    WHMCS_CLIENT_ID: ${{ secrets.WHMCS_CLIENT_ID }}
    # External APIs
    PENGUIN_RANDOM_HOUSE_API_DOMAIN: ${{ secrets.PENGUIN_RANDOM_HOUSE_API_DOMAIN }}
    PENGUIN_RANDOM_HOUSE_API_KEY: ${{ secrets.PENGUIN_RANDOM_HOUSE_API_KEY }}
    AMAZON_PRODUCT_ACCESS_KEY: ${{ secrets.AMAZON_PRODUCT_ACCESS_KEY }}
    AMAZON_PRODUCT_SECRET_KEY: ${{ secrets.AMAZON_PRODUCT_SECRET_KEY }}
    AMAZON_PRODUCT_PARTNER_TAG: ${{ secrets.AMAZON_PRODUCT_PARTNER_TAG }}
    AMAZON_PRODUCT_MARKETPLACE: ${{ secrets.AMAZON_PRODUCT_MARKETPLACE }}
    AMAZON_PRODUCT_REGION: ${{ secrets.AMAZON_PRODUCT_REGION }}
    # Azure DNS / Front Door
    AZURE_DNS_RESOURCE_GROUP: ${{ secrets.AZURE_DNS_RESOURCE_GROUP }}
    AZURE_FRONTDOOR_PROFILE_NAME: ${{ secrets.AZURE_FRONTDOOR_PROFILE_NAME }}
    AZURE_SUBSCRIPTION_ID: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
    AZURE_RESOURCE_GROUP_NAME: ${{ secrets.AZURE_RESOURCE_GROUP_NAME }}
    # Communication Services
    ACS_CONNECTION_STRING: ${{ secrets.ACS_CONNECTION_STRING }}
    ACS_SENDER_ADDRESS: ${{ secrets.ACS_SENDER_ADDRESS }}
    # Service Bus (for queue processors)
    WHMCS_SB_NAMESPACE_NAME: ${{ secrets.WHMCS_SB_NAMESPACE_NAME }}
    WHMCS_SB_QUEUE_NAME: ${{ secrets.WHMCS_SB_QUEUE_NAME }}
    # Application Insights
    APPLICATIONINSIGHTS_CONNECTION_STRING: ${{ secrets.APPLICATIONINSIGHTS_CONNECTION_STRING_ISW }}
    # Notification Hub (new)
    NOTIFICATION_HUB_CONNECTION_STRING: ${{ secrets.NOTIFICATION_HUB_CONNECTION_STRING }}
    NOTIFICATION_HUB_NAME: ${{ secrets.NOTIFICATION_HUB_NAME }}
    ISW_DNS_ZONE_NAME: ${{ secrets.ISW_DNS_ZONE_NAME }}
    ISW_LOCATION: ${{ secrets.ISW_LOCATION }}
  run: |
    if [ "$DEPLOY_ISW_FUNCTIONS" != "true" ] || [ -z "$ISW_RESOURCE_GROUP" ] || [ -z "$ISW_BASE_NAME" ]; then
      echo "⚠️ Skipping app settings configuration"
      exit 0
    fi

    FUNCTION_APP_NAME="${ISW_BASE_NAME}"
    SETTINGS=()

    # Helper function: add setting only if value is non-empty
    add_setting() {
      local key="$1" value="$2"
      if [ -n "$value" ]; then
        SETTINGS+=( "${key}=${value}" )
      else
        echo "⚠️ Skipping empty setting: $key"
      fi
    }

    add_setting "AAD_TENANT_ID"               "$AAD_TENANT_ID"
    add_setting "AAD_AUDIENCE"                "$AAD_AUDIENCE"
    add_setting "AAD_CLIENT_ID"               "$AAD_CLIENT_ID"
    add_setting "AAD_AUTHORITY"               "$AAD_AUTHORITY"
    add_setting "AAD_VALID_ISSUERS"           "$AAD_VALID_ISSUERS"
    add_setting "OPEN_ID_CONNECT_METADATA_URL" "$OPEN_ID_CONNECT_METADATA_URL"
    add_setting "COSMOSDB_ENDPOINT_URI"       "$COSMOSDB_ENDPOINT_URI"
    add_setting "COSMOSDB_PRIMARY_KEY"        "$COSMOSDB_PRIMARY_KEY"
    add_setting "COSMOSDB_DATABASE_ID"        "$COSMOSDB_DATABASE_ID"
    add_setting "COSMOSDB_CONNECTION_STRING"  "$COSMOSDB_CONNECTION_STRING"
    add_setting "STRIPE_API_KEY"              "$STRIPE_API_KEY"
    add_setting "STRIPE_WEBHOOK_SECRET"       "$STRIPE_WEBHOOK_SECRET"
    add_setting "AZURE_STORAGE_CONNECTION_STRING" "$AZURE_STORAGE_CONNECTION_STRING"
    add_setting "WHMCS_API_URL"               "$WHMCS_API_URL"
    add_setting "WHMCS_API_IDENTIFIER"        "$WHMCS_API_IDENTIFIER"
    add_setting "WHMCS_API_SECRET"            "$WHMCS_API_SECRET"
    add_setting "WHMCS_CLIENT_ID"             "$WHMCS_CLIENT_ID"
    add_setting "PENGUIN_RANDOM_HOUSE_API_DOMAIN" "$PENGUIN_RANDOM_HOUSE_API_DOMAIN"
    add_setting "PENGUIN_RANDOM_HOUSE_API_KEY"    "$PENGUIN_RANDOM_HOUSE_API_KEY"
    add_setting "AMAZON_PRODUCT_ACCESS_KEY"   "$AMAZON_PRODUCT_ACCESS_KEY"
    add_setting "AMAZON_PRODUCT_SECRET_KEY"   "$AMAZON_PRODUCT_SECRET_KEY"
    add_setting "AMAZON_PRODUCT_PARTNER_TAG"  "$AMAZON_PRODUCT_PARTNER_TAG"
    add_setting "AMAZON_PRODUCT_MARKETPLACE"  "$AMAZON_PRODUCT_MARKETPLACE"
    add_setting "AMAZON_PRODUCT_REGION"       "$AMAZON_PRODUCT_REGION"
    add_setting "AZURE_DNS_RESOURCE_GROUP"    "$AZURE_DNS_RESOURCE_GROUP"
    add_setting "AZURE_FRONTDOOR_PROFILE_NAME" "$AZURE_FRONTDOOR_PROFILE_NAME"
    add_setting "AZURE_SUBSCRIPTION_ID"       "$AZURE_SUBSCRIPTION_ID"
    add_setting "AZURE_RESOURCE_GROUP_NAME"   "$AZURE_RESOURCE_GROUP_NAME"
    add_setting "ACS_CONNECTION_STRING"       "$ACS_CONNECTION_STRING"
    add_setting "ACS_SENDER_ADDRESS"          "$ACS_SENDER_ADDRESS"
    add_setting "WHMCS_SB_NAMESPACE_NAME"     "$WHMCS_SB_NAMESPACE_NAME"
    add_setting "WHMCS_SB_QUEUE_NAME"         "$WHMCS_SB_QUEUE_NAME"
    add_setting "APPLICATIONINSIGHTS_CONNECTION_STRING" "$APPLICATIONINSIGHTS_CONNECTION_STRING"
    add_setting "NOTIFICATION_HUB_CONNECTION_STRING" "$NOTIFICATION_HUB_CONNECTION_STRING"
    add_setting "NOTIFICATION_HUB_NAME"       "$NOTIFICATION_HUB_NAME"
    add_setting "ISW_DNS_ZONE_NAME"           "$ISW_DNS_ZONE_NAME"
    add_setting "ISW_LOCATION"                "$ISW_LOCATION"

    if [ "${#SETTINGS[@]}" -eq 0 ]; then
      echo "⚠️ No app settings to configure"
      exit 0
    fi

    echo "🔧 Applying ${#SETTINGS[@]} app settings to $FUNCTION_APP_NAME..."
    az functionapp config appsettings set \
      --name "$FUNCTION_APP_NAME" \
      --resource-group "$ISW_RESOURCE_GROUP" \
      --settings "${SETTINGS[@]}" \
      --only-show-errors \
      --output none
    echo "✓ App settings applied"

── STEP 11: Deploy WHMCS Worker VM Infrastructure ────────────
Identical to the current workflow (unchanged).
Required secrets: WHMCS_WORKER_RESOURCE_GROUP, WHMCS_WORKER_SSH_PUBLIC_KEY,
                 DEPLOY_WHMCS_WORKER (must equal "true")
Template: infra/vm.bicep
continue-on-error: true

── STEP 12: Deploy WhmcsWorkerService Binary ─────────────────
Identical to the current workflow (unchanged).
Steps:
a. Upload whmcsworker.zip to Azure Blob Storage container "whmcs-worker-deploy"
b. Generate a 30-minute SAS URL
c. az vm run-command invoke to download, unzip, install, and start the service
continue-on-error: true

── STEP 13: Configure WhmcsWorkerService Environment ─────────
Identical to the current workflow (unchanged).
Writes /etc/whmcs-worker/environment on the VM with secrets passed as
protected parameters to az vm run-command invoke.
continue-on-error: true

REQUIRED GITHUB SECRETS (consolidated list for new architecture)
================================================================
Add these to the repository's GitHub Secrets. Remove secrets that are
no longer needed after consolidation.

SECRETS TO KEEP (required):
  AZURE_CREDENTIALS                — Service principal JSON for az login
  AZURE_SUBSCRIPTION_ID            — Azure subscription ID
  AAD_TENANT_ID                    — Microsoft Entra ID tenant GUID
  AAD_AUDIENCE                     — API client ID / audience
  AAD_CLIENT_ID                    — Application client ID
  AAD_AUTHORITY                    — Entra ID authority URL
  AAD_VALID_ISSUERS                — Comma-separated valid JWT issuers
  OPEN_ID_CONNECT_METADATA_URL     — OIDC metadata document URL
  COSMOSDB_RESOURCE_GROUP          — Resource group for Cosmos DB
  COSMOSDB_ACCOUNT_NAME            — Cosmos DB account name
  COSMOSDB_LOCATION                — Azure region for Cosmos DB
  COSMOSDB_ENDPOINT_URI            — Cosmos DB endpoint URI
  COSMOSDB_PRIMARY_KEY             — Cosmos DB primary key
  COSMOSDB_DATABASE_ID             — Cosmos DB database ID
  COSMOSDB_CONNECTION_STRING       — Cosmos DB connection string
  COSMOSDB_ENABLE_FREE_TIER        — "true" or "false"
  COSMOSDB_ENABLE_ZONE_REDUNDANCY  — "true" or "false"
  APPINSIGHTS_NAME                 — Application Insights resource name
  APPLICATIONINSIGHTS_CONNECTION_STRING_ISW — App Insights connection string
  ISW_RESOURCE_GROUP               — Resource group for the consolidated Function App
  ISW_BASE_NAME                    — Base name for the consolidated Function App
  ISW_LOCATION                     — Azure region for the Function App
  ISW_DNS_ZONE_NAME                — Azure DNS zone name
  DEPLOY_ISW_FUNCTIONS             — "true" to deploy the Function App
  AZURE_DNS_RESOURCE_GROUP         — Resource group containing Azure DNS zones
  AZURE_FRONTDOOR_PROFILE_NAME     — Azure Front Door profile name
  AZURE_RESOURCE_GROUP_NAME        — Resource group for Front Door
  AZURE_STORAGE_CONNECTION_STRING  — Blob Storage for image uploads
  STRIPE_API_KEY                   — Stripe secret key
  STRIPE_WEBHOOK_SECRET            — Stripe webhook signing secret
  WHMCS_API_URL                    — WHMCS REST API base URL
  WHMCS_API_IDENTIFIER             — WHMCS API credential identifier
  WHMCS_API_SECRET                 — WHMCS API credential secret
  WHMCS_CLIENT_ID                  — WHMCS client ID
  WHMCS_SB_NAMESPACE_NAME          — Service Bus namespace name (must not end in -sb)
  WHMCS_SB_QUEUE_NAME              — WHMCS domain registration queue name
  ACS_CONNECTION_STRING            — Azure Communication Services connection string
  ACS_SENDER_ADDRESS               — Email sender address for ACS
  PENGUIN_RANDOM_HOUSE_API_DOMAIN  — PRH API domain
  PENGUIN_RANDOM_HOUSE_API_KEY     — PRH API key
  AMAZON_PRODUCT_ACCESS_KEY        — Amazon PAAPI access key
  AMAZON_PRODUCT_SECRET_KEY        — Amazon PAAPI secret key
  AMAZON_PRODUCT_PARTNER_TAG       — Amazon associate tag
  AMAZON_PRODUCT_MARKETPLACE       — Amazon marketplace (e.g., www.amazon.com)
  AMAZON_PRODUCT_REGION            — Amazon region (e.g., us-east-1)
  NOTIFICATION_HUB_CONNECTION_STRING — Azure Notification Hub connection string [NEW]
  NOTIFICATION_HUB_NAME            — Azure Notification Hub name [NEW]
  WHMCS_WORKER_RESOURCE_GROUP      — Resource group for the WHMCS worker VM
  WHMCS_WORKER_VM_NAME             — VM name (default: whmcs-worker-vm)
  WHMCS_WORKER_LOCATION            — Azure region for the VM
  WHMCS_WORKER_ADMIN_USERNAME      — VM admin username
  WHMCS_WORKER_SSH_PUBLIC_KEY      — SSH public key for VM access
  DEPLOY_WHMCS_WORKER              — "true" to deploy the WHMCS worker

SECRETS TO REMOVE (no longer needed after consolidation):
  AZURE_FUNCTIONAPP_NAME           — Replaced by ISW_BASE_NAME
  AZURE_RESOURCE_GROUP             — Replaced by ISW_RESOURCE_GROUP
  AZURE_LOCATION                   — Replaced by ISW_LOCATION
  DEPLOY_IMAGE_API                 — Image API is now in the consolidated app
  DEPLOY_ISW_STRIPE                — Stripe is now in the consolidated app
  DEPLOY_ISW_CONFIG                — InkStainedWretchesConfig is removed
  DEPLOY_COMMUNICATION_SERVICES    — Merged into DEPLOY_ISW_FUNCTIONS
  APPLICATIONINSIGHTS_CONNECTION_STRING_FUNCTION_APP — Consolidated to _ISW
  APPLICATIONINSIGHTS_CONNECTION_STRING (bare) — Use _ISW variant only

INFRASTRUCTURE BICEP TEMPLATES TO CREATE/UPDATE
================================================
If these Bicep templates don't already exist, create them:

1. infra/notificationhub.bicep
   Parameters: namespaceName (string), hubName (string), location (string)
   Creates an Azure Notification Hub namespace (Free tier) and a hub.
   Outputs: connectionString (listens), hubName

2. infra/servicebus.bicep (update if exists)
   Ensure the namespace name does NOT end in "-sb" (reserved suffix).
   Use the ISW_BASE_NAME + "-bus" naming pattern.
   Create queues:
   - whmcs-domain-registrations
   - domain-registration-commands
   - author-invitation-commands
   - testimonial-commands
   - lead-commands
   - referral-commands
   - image-commands
   - stripe-commands
   Authorization rules: WhmcsSender (Send), WhmcsListener (Listen),
   FunctionAppSender (Send on all command queues),
   FunctionAppListener (Listen on all command queues)

NOTES AND CONSTRAINTS
=====================
1. The consolidated Function App's name defaults to ISW_BASE_NAME.
   If ISW_BASE_NAME already refers to a different resource, add "-api" suffix.

2. All continue-on-error: true steps should print clear ⚠️ / ✓ / ❌
   messages to make the run log easy to read.

3. Infrastructure deployment steps should check for existing resources
   before deploying (idempotent pattern from current workflow).

4. App settings must be applied in a SINGLE az functionapp config appsettings set
   call (not multiple calls) to minimize Function App restarts.

5. Secrets passed to az vm run-command invoke must use --parameters (positional
   $1, $2, etc.) so they do not appear in Azure Activity Logs.

6. The WhmcsWorkerService deployment and environment configuration steps are
   IDENTICAL to the current workflow — do not modify them.

7. Add a summary step at the end that prints:
   - Deployed version (VERSION)
   - Function App name
   - Deployment timestamp
```
