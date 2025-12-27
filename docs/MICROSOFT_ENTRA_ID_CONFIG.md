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
   URI:  
