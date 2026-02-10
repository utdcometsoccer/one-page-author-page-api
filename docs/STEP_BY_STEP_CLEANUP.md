# Step-by-Step Secret Removal Process

## ⚠️ CRITICAL: Complete this process ASAP to remove exposed secrets

### Step 1: Prepare for History Cleanup

1. **Create a backup** (in case anything goes wrong):

   ```powershell

   git clone --mirror . ../backup-$(Get-Date -Format 'yyyy-MM-dd-HHmmss')

   ```

2. **Commit any pending changes**:

   ```powershell

   git add .
   git commit -m "Prepare for secret cleanup"

   ```

### Step 2: Use BFG Repo-Cleaner (RECOMMENDED)

**Option A: Download BFG**

- Download `bfg.jar` from: <https://rtyley.github.io/bfg-repo-cleaner/>
- Requires Java (install if needed)

**Option B: Use Package Manager**

```powershell
# Windows (Scoop)
scoop install bfg

# macOS (Homebrew)  
brew install bfg

```

### Step 3: Create Secret Replacements File

Create `replacements.txt` in your repository root:

```
your-cosmos-primary-key==>***REMOVED***
your-cosmos-secondary-key==>***REMOVED***
your-amazon-access-key==>***REMOVED***
your-amazon-secret-key==>***REMOVED***
your-azure-tenant-id==>***REMOVED***
your-app-client-id==>***REMOVED***
your-google-project-id==>***REMOVED***

```

### Step 4: Clean Repository History

```powershell
# Create a fresh mirror clone for cleaning
git clone --mirror https://github.com/utdcometsoccer/one-page-author-page-api.git temp-clean.git

# Run BFG to clean secrets
java -jar bfg.jar --replace-text replacements.txt temp-clean.git

# Clean up Git objects  
cd temp-clean.git
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# Force push cleaned history
git push --force

```

### Step 5: Update Your Local Repository

```powershell
# Delete your current local copy
cd ..
Remove-Item -Recurse -Force one-page-author-page-api

# Re-clone the cleaned repository
git clone https://github.com/utdcometsoccer/one-page-author-page-api.git
cd one-page-author-page-api

```

### Step 6: Set Up User Secrets for Development

```powershell
cd InkStainedWretchFunctions
dotnet user-secrets init

# Add your actual secrets (get from team lead or Azure portal):
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-actual-cosmos-key"
dotnet user-secrets set "COSMOSDB_CONNECTION_STRING" "your-actual-cosmos-connection"  
dotnet user-secrets set "CosmosDBConnection" "your-actual-cosmos-connection"
dotnet user-secrets set "PENGUIN_RANDOM_HOUSE_API_KEY" "your-actual-prh-key"
dotnet user-secrets set "AMAZON_PRODUCT_ACCESS_KEY" "your-actual-amazon-key"
dotnet user-secrets set "AMAZON_PRODUCT_SECRET_KEY" "your-actual-amazon-secret"
dotnet user-secrets set "AAD_TENANT_ID" "your-actual-tenant-id"
dotnet user-secrets set "AAD_CLIENT_ID" "your-actual-client-id" 

```

### Step 7: Test Your Setup

```powershell
# Build and test the application
dotnet build
dotnet run  # or func start

```

### Step 8: Notify Team Members

Send this message to all team members:

> **🚨 URGENT: Repository History Cleaned**
>
> The Git history has been cleaned to remove exposed secrets.
>
> **YOU MUST:**
>
> 1. Delete your local clone
> 2. Re-clone from GitHub
> 3. Set up user secrets (see instructions in repository)
> 4. DO NOT push old branches or commits
>
> **DO NOT commit secrets to the repository again!**

### Step 9: Clean Up Temporary Files

```powershell
# Remove temporary files
Remove-Item replacements.txt -Force
Remove-Item -Recurse -Force temp-clean.git

```

## ✅ Verification

After completing these steps:

1. **Check history is clean**: `git log --oneline --all | grep -i secret` (should return nothing)
2. **Verify app works**: Test all functions locally
3. **Confirm secrets are in user-secrets**: `dotnet user-secrets list`

## 🚨 If Something Goes Wrong

1. Restore from backup: Copy from the backup directory you created
2. Contact team lead for help
3. Review Azure portal for production secret values

## Production Deployment

- Use Azure App Settings for production secrets
- Configure in Azure Portal > Function App > Configuration
- Never store production secrets in code or user-secrets

---

**EXECUTE THIS PROCESS IMMEDIATELY to secure your repository!**
