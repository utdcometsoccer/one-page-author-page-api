# Creates an Azure AD app + service principal with subscription-scope access
# and prints the JSON payload you store as the GitHub Secret `AZURE_CREDENTIALS`.
#
# Prereqs:
# 1) az login
# 2) Permission to create service principals and assign roles at subscription scope
#
# Notes:
# - The generated JSON contains a client secret. Treat it as highly sensitive.
# - Prefer OIDC (federated credentials) for GitHub Actions if you want to avoid storing a secret.

$subscriptionId = (az account show --query id -o tsv)

$spName = "gh-onepageauthorapi-deploy-sub"
$scope  = "/subscriptions/$subscriptionId"

$azureCredentialsJson = az ad sp create-for-rbac `
  --name $spName `
  --role "Contributor" `
  --scopes $scope `
  --sdk-auth

# Optional: save locally (treat as sensitive). This file should remain untracked.
$azureCredentialsJson | Out-File .\azure-credentials-subscription.json -Encoding utf8

# Copy/paste this JSON into GitHub Secret named AZURE_CREDENTIALS
$azureCredentialsJson
