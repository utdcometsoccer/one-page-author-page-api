# Microsoft Entra ID Authentication & Authorization Documentation

This directory contains comprehensive documentation for Microsoft Entra ID (formerly Azure Active Directory) authentication and authorization in the OnePageAuthor API platform.

## Documentation Structure

### Core Configuration Guides
- **[ENTRA_CIAM_CONFIGURATION_GUIDE.md](ENTRA_CIAM_CONFIGURATION_GUIDE.md)** - Complete guide for configuring Customer Identity Access Management (CIAM) with Microsoft Entra ID
- **[AZURE_ENTRA_ID_CONFIGURATION_CHECKLIST.md](AZURE_ENTRA_ID_CONFIGURATION_CHECKLIST.md)** - Step-by-step checklist for setting up Entra ID

### Environment Variables & Configuration
- **[ENVIRONMENT_VARIABLES_GUIDE.md](ENVIRONMENT_VARIABLES_GUIDE.md)** - Detailed guide for all Entra ID-related environment variables and where to find the values

### Architecture & Design
- **[AUTHENTICATION_AUTHORIZATION_SEPARATION.md](AUTHENTICATION_AUTHORIZATION_SEPARATION.md)** - Understanding the separation between authentication and authorization
- **[AUTHENTICATION_LOGGING.md](AUTHENTICATION_LOGGING.md)** - Comprehensive guide to authentication logging and telemetry

### Troubleshooting & Debugging
- **[JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md)** - Complete troubleshooting guide for JWT token validation issues
- **[ENTRA_ID_EXCEPTIONS_REFERENCE.md](ENTRA_ID_EXCEPTIONS_REFERENCE.md)** - Reference guide for all Entra ID-related exceptions
- **[APPLICATION_INSIGHTS_QUERIES.md](APPLICATION_INSIGHTS_QUERIES.md)** - KQL queries for monitoring authentication in Application Insights

### Best Practices
- **[MSAL_CIAM_BEST_PRACTICES.md](MSAL_CIAM_BEST_PRACTICES.md)** - Best practices for using MSAL (Microsoft Authentication Library) with CIAM

## Quick Start

### For Developers Setting Up Authentication
1. Start with [AZURE_ENTRA_ID_CONFIGURATION_CHECKLIST.md](AZURE_ENTRA_ID_CONFIGURATION_CHECKLIST.md)
2. Configure environment variables using [ENVIRONMENT_VARIABLES_GUIDE.md](ENVIRONMENT_VARIABLES_GUIDE.md)
3. Review [ENTRA_CIAM_CONFIGURATION_GUIDE.md](ENTRA_CIAM_CONFIGURATION_GUIDE.md) for CIAM-specific setup

### For Developers Troubleshooting Authentication Issues
1. Start with [JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md)
2. Check [ENTRA_ID_EXCEPTIONS_REFERENCE.md](ENTRA_ID_EXCEPTIONS_REFERENCE.md) for your specific error
3. Use KQL queries from [APPLICATION_INSIGHTS_QUERIES.md](APPLICATION_INSIGHTS_QUERIES.md) to investigate

### For Architects & Technical Leads
1. Review [AUTHENTICATION_AUTHORIZATION_SEPARATION.md](AUTHENTICATION_AUTHORIZATION_SEPARATION.md)
2. Understand logging with [AUTHENTICATION_LOGGING.md](AUTHENTICATION_LOGGING.md)
3. Follow best practices in [MSAL_CIAM_BEST_PRACTICES.md](MSAL_CIAM_BEST_PRACTICES.md)

## Related Documentation

### In Main Docs Directory
- [../MICROSOFT_ENTRA_ID_CONFIG.md](../MICROSOFT_ENTRA_ID_CONFIG.md) - Original Entra ID configuration (legacy, see ENTRA_CIAM_CONFIGURATION_GUIDE.md)
- [../IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES.md](../IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES.md) - Entra ID roles implementation summary
- [../MIGRATION_GUIDE_ENTRA_ID_ROLES.md](../MIGRATION_GUIDE_ENTRA_ID_ROLES.md) - Migration guide for Entra ID roles
- [../401_UNAUTHORIZED_RESOLUTION.md](../401_UNAUTHORIZED_RESOLUTION.md) - 401 error resolution guide
- [../AUTHORIZATION_FIX_DOCUMENTATION.md](../AUTHORIZATION_FIX_DOCUMENTATION.md) - Authorization fix documentation
- [../JWT_KEY_ROTATION_FIX.md](../JWT_KEY_ROTATION_FIX.md) - JWT signing key rotation fix
- [../REFRESH_ON_ISSUER_KEY_NOT_FOUND.md](../REFRESH_ON_ISSUER_KEY_NOT_FOUND.md) - RefreshOnIssuerKeyNotFound configuration

### Implementation Files
- `OnePageAuthorLib/Authentication/JwtValidationService.cs` - JWT token validation service
- `OnePageAuthorLib/Authentication/JwtAuthenticationHelper.cs` - JWT authentication helper
- `OnePageAuthorLib/Authentication/TokenIntrospectionService.cs` - Token introspection service
- `OnePageAuthorLib/api/AuthenticatedFunctionTelemetryService.cs` - Authentication telemetry

### Program.cs Files (Authentication Configuration)
- `ImageAPI/Program.cs` - Image API authentication setup
- `InkStainedWretchFunctions/Program.cs` - Main functions authentication setup
- `InkStainedWretchStripe/Program.cs` - Stripe functions authentication setup

## Key Concepts

### Microsoft Entra ID CIAM
Customer Identity Access Management (CIAM) is a specialized identity solution for customer-facing applications. The OnePageAuthor platform uses Entra ID CIAM for:
- User authentication for the Ink Stained Wretches SPA
- API authentication via JWT Bearer tokens
- Role-based access control (RBAC)
- Token management and refresh

### JWT Authentication Flow
1. User authenticates with Entra ID CIAM (via MSAL in SPA)
2. Entra ID issues JWT access token
3. SPA includes token in `Authorization: Bearer <token>` header
4. Azure Function validates token signature, issuer, audience, expiration
5. Function extracts user claims (user ID, email, roles)
6. Function authorizes access based on claims

### Key Refresh & Rotation
Entra ID periodically rotates signing keys used to sign JWT tokens. The platform handles this automatically:
- `RefreshOnIssuerKeyNotFound = true` in Program.cs
- Singleton `ConfigurationManager` with automatic refresh
- Retry logic in `JwtValidationService`

See [JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md) for details.

## Support & Feedback

For questions or issues:
1. Check the appropriate troubleshooting guide in this directory
2. Review Application Insights logs using KQL queries
3. Refer to implementation code in `OnePageAuthorLib/Authentication/`
4. Consult Microsoft Entra ID documentation: https://learn.microsoft.com/en-us/entra/

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0 | 2026-01-13 | Initial consolidated authentication documentation |

---

**Note**: This documentation consolidates and updates previous authentication documentation. For historical reference, see the main docs directory.
