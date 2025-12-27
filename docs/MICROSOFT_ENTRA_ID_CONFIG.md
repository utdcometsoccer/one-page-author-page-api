# Azure Static Web Apps + Azure Functions + Microsoft Entra ID Authentication Setup

This document provides a complete, end‑to‑end configuration for:

- A Single Page Application (SPA) hosted on Azure Static Web Apps  
- A backend API hosted on Azure Functions  
- Authentication using Microsoft Entra ID  
- A separate SPA app registration and API app registration  
- MSAL configuration  
- Azure Functions JWT validation  
- A full ASCII diagram of the authentication flow  

---

# 1. Entra ID Configuration Steps

## 1.1 Create the SPA App Registration

1. Go to **Microsoft Entra ID → App registrations → New registration**
2. Name:  
   **InkstainedWretches SPA**
3. Supported account types:  
   **Accounts in this organizational directory only**
4. Redirect URI:  
   Platform: **Single-page application**  
   URI:  https://inkstainedwretches.com/.auth/login/aad/callback
   Local development (SWA CLI):  http://localhost:4280/.auth/login/aad/callback
5. Save.

### Add API Permissions

1. Go to **API permissions → Add a permission**
2. Select **My APIs**
3. Choose your API app registration (created below)
4. Add the scope:

---

## 1.2 Create the API App Registration

1. Go to **App registrations → New registration**
2. Name:  
**InkstainedWretches API**
3. No redirect URIs needed.

### Expose the API

1. Go to **Expose an API**
2. Click **Set** for Application ID URI  
It becomes: api://<api-client-id>
3. Add a scope:
- **Scope name:** `access_as_user`
- **Who can consent:** Admins and users
- **Description:**  
  “Allow the SPA to call the API on behalf of the user.”

### (Optional) Add App Roles

Only if you want role-based authorization.

---

# 2. MSAL Configuration for the SPA

```javascript
import { PublicClientApplication } from "@azure/msal-browser";

export const msalConfig = {
auth: {
 clientId: "<SPA-CLIENT-ID>",
 authority: "https://login.microsoftonline.com/<TENANT-ID>",
 redirectUri: "https://inkstainedwretches.com/.auth/login/aad/callback"
},
cache: {
 cacheLocation: "localStorage",
 storeAuthStateInCookie: false
}
};

export const loginRequest = {
scopes: ["api://<API-CLIENT-ID>/access_as_user"]
};

export const msalInstance = new PublicClientApplication(msalConfig);
```

# 3. Azure Functions Configuration
Azure Static Web Apps automatically injects the authenticated user into your Functions via headers, but for direct JWT validation, configure the following.

## 3.1 host.json

```
{
  "version": "2.0",
  "extensions": {
    "http": {
      "routePrefix": ""
    }
  }
}
```

## 3.2 local.settings.json
```
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "node",
    "WEBSITE_AUTH_ENABLED": false,
    "JWT_ISSUER": "https://login.microsoftonline.com/<TENANT-ID>/v2.0",
    "JWT_AUDIENCE": "api://<API-CLIENT-ID>"
  }
}
```

# 4. Full Authentication Flow Diagram

+---------------------------+
|     User's Browser        |
+-------------+-------------+
              |
              | 1. User clicks "Sign in"
              v
+---------------------------+
|   Azure Static Web Apps   |
|   /.auth/login/aad        |
+-------------+-------------+
              |
              | 2. Redirect to Entra ID
              v
+---------------------------+
|     Microsoft Entra ID    |
+-------------+-------------+
              |
              | 3. User authenticates
              | 4. Entra redirects back
              v
+---------------------------+
|   SWA /.auth/login/aad/   |
|          callback         |
+-------------+-------------+
              |
              | 5. SWA issues auth cookie
              |    and injects user identity
              v
+---------------------------+
|   Single Page App (SPA)   |
+-------------+-------------+
              |
              | 6. SPA calls API with token
              v
+---------------------------+
|   Azure Functions API     |
|  Validates JWT using      |
|  API App Registration     |
+---------------------------+
              |
              | 7. Returns data
              v
+---------------------------+
|   SPA renders response    |
+---------------------------+
