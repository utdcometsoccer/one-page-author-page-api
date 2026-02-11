# Minimum Viable Launch Checklist - North America

**Created:** 2026-02-11  
**Target:** First Sale in North America (US, Canada, Mexico)  
**Status:** 🟡 In Progress - Domain Validation Phase

## Overview

This document identifies the **absolute minimum** requirements to launch OnePageAuthor API platform and achieve the first customer sale in North America. Everything not on this list is deferred to post-launch.

**Philosophy:** Ship fast, iterate based on real customer feedback.

---

## 🎯 Definition of "Launch Ready"

A user can:
1. ✅ Sign up and authenticate via Entra ID
2. ✅ Purchase a subscription via Stripe
3. ⚠️ Register a custom domain for their author page
4. ⚠️ Have DNS automatically configured and domain go live
5. ✅ Upload images and content to their author page
6. ✅ View their live author page at their custom domain
7. ✅ Manage their subscription (upgrade, cancel)

---

## 🔴 CRITICAL PATH TO FIRST SALE

### Stage 1: Pre-Launch Validation (1 Week) ⚠️ CURRENT STAGE

**Goal:** Validate domain registration workflow works end-to-end

#### 1.1 Domain Registration E2E Test
- [ ] Set up Google Domains API test environment
- [ ] Acquire test domain (e.g., cheap `.xyz` or `.test` domain)
- [ ] Execute full registration flow with real API calls
- [ ] Verify domain registration completes successfully
- [ ] Document any issues found and fix

**Estimated Time:** 4-6 hours  
**Owner:** DevOps + QA  
**Blocker:** YES - Cannot launch without this

#### 1.2 DNS Automation Test
- [ ] Configure Azure DNS resource group
- [ ] Deploy DNS zone creation trigger function
- [ ] Register test domain and verify DNS zone is auto-created
- [ ] Confirm nameservers are assigned correctly
- [ ] Verify NS records are stored in DomainRegistration entity

**Estimated Time:** 3-4 hours  
**Owner:** DevOps  
**Blocker:** YES - Domains won't work without DNS

#### 1.3 Front Door Integration Test
- [ ] Configure Azure Front Door profile in staging
- [ ] Test automatic domain addition after DNS creation
- [ ] Verify domain validation (CNAME/TXT records)
- [ ] Confirm HTTPS certificate provisioning works
- [ ] Test routing to author pages

**Estimated Time:** 4-5 hours  
**Owner:** DevOps  
**Blocker:** YES - Custom domains won't route without Front Door

**STAGE 1 COMPLETE WHEN:** All three blockers validated and documented

---

### Stage 2: Production Configuration (2-3 Days)

**Goal:** Configure production environment for real customer traffic

#### 2.1 Azure Resources
- [ ] Verify Cosmos DB is production-ready (scaling, backup, monitoring)
- [ ] Deploy all 4 Function Apps to production
- [ ] Configure Azure DNS resource group (production)
- [ ] Configure Azure Front Door profile (production)
- [ ] Verify Application Insights monitoring is active
- [ ] Confirm Azure Blob Storage is configured for images

#### 2.2 Environment Variables (All Function Apps)
- [ ] **ImageAPI** - Set `COSMOSDB_*`, `AAD_*`, storage config
- [ ] **InkStainedWretchFunctions** - Set `COSMOSDB_*`, `AAD_*`, `AZURE_DNS_*`, `AZURE_FRONTDOOR_*`
- [ ] **InkStainedWretchStripe** - Set `STRIPE_API_KEY` (production), `STRIPE_WEBHOOK_SECRET`, `COSMOSDB_*`
- [ ] **function-app** - Set `COSMOSDB_*`, `AAD_*`
- [ ] **InkStainedWretchesConfig** - Set `COSMOSDB_*`

#### 2.3 External Service Configuration
- [ ] Stripe: Production API keys configured, webhook endpoints registered
- [ ] Entra ID: Production tenant configured, app registration complete
- [ ] Google Domains: Production API credentials (if applicable)
- [ ] Azure Communication Services: Email configuration (if using ACS)

#### 2.4 Data Seeding (Production)
- [ ] Run `SeedInkStainedWretchesLocale` - Localized UI text (EN, ES, FR)
- [ ] Run `SeedCountries` - Country data for NA (US, CA, MX)
- [ ] Run `SeedLanguages` - Language data
- [ ] Run `OnePageAuthor.DataSeeder` - StateProvince data (US states, CA provinces, MX states)
- [ ] Run `SeedImageStorageTiers` - Storage tier configurations
- [ ] Verify Stripe subscription products are synced

**STAGE 2 COMPLETE WHEN:** All production resources configured and verified

---

### Stage 3: Pre-Launch Testing (1-2 Days)

**Goal:** Execute critical user journeys in production environment

#### 3.1 Smoke Tests (Production)
- [ ] Test user authentication (sign in via Entra ID)
- [ ] Test Stripe checkout (use Stripe test mode initially)
- [ ] Test domain registration (with test domain)
- [ ] Test DNS zone creation
- [ ] Test Front Door domain routing
- [ ] Test image upload
- [ ] Test author profile creation/update

#### 3.2 End-to-End User Journey
- [ ] Complete journey: Register → Subscribe → Domain → Content → Live Site
- [ ] Verify custom domain resolves correctly
- [ ] Verify HTTPS works on custom domain
- [ ] Verify author page displays correctly
- [ ] Test from multiple browsers/devices

#### 3.3 Monitoring Validation
- [ ] Verify Application Insights captures telemetry
- [ ] Test critical alerts fire correctly
- [ ] Verify Stripe webhook events are logged
- [ ] Confirm error tracking works

**STAGE 3 COMPLETE WHEN:** All smoke tests pass in production

---

### Stage 4: Soft Launch (3-5 Days)

**Goal:** Launch to limited audience, monitor closely, iterate

#### 4.1 Limited Availability
- [ ] Launch to beta testers (5-10 users)
- [ ] Provide direct support channel (email/Slack)
- [ ] Monitor all telemetry 24/7 during initial period
- [ ] Address issues within hours, not days

#### 4.2 Customer Feedback Loop
- [ ] Collect feedback after each registration
- [ ] Document pain points and bugs
- [ ] Prioritize fixes: Blocker → High → Medium → Low
- [ ] Deploy fixes rapidly

#### 4.3 Performance Validation
- [ ] Monitor API response times (< 500ms target)
- [ ] Monitor domain registration completion time (< 2 hours target)
- [ ] Monitor DNS propagation time (< 24 hours target)
- [ ] Monitor payment success rate (> 98% target)

**STAGE 4 COMPLETE WHEN:** 5+ successful customer registrations with zero critical issues

---

### Stage 5: General Availability

**Goal:** Open to all North America customers

#### 5.1 Marketing Launch
- [ ] Announce availability on website
- [ ] Social media announcement
- [ ] Email existing waitlist (if applicable)
- [ ] Press release (optional)

#### 5.2 Operations Readiness
- [ ] On-call rotation established
- [ ] Escalation procedures documented
- [ ] Customer support trained
- [ ] Incident response procedures ready

#### 5.3 Monitoring & Alerts
- [ ] Critical alerts configured (on-call engineer notified)
- [ ] Dashboard with key metrics visible
- [ ] Daily operations review scheduled

**STAGE 5 COMPLETE WHEN:** Public launch announced, first 25 paying customers onboarded

---

## ✅ Feature Completeness - What Ships at Launch

### Must Have (Shipping)

| Feature | Status | Confidence |
|---------|--------|-----------|
| User authentication (Entra ID JWT) | ✅ Complete | 🟢 High |
| Stripe subscription management | ✅ Complete | 🟢 High |
| Author profile CRUD | ✅ Complete | 🟢 High |
| Image upload/storage | ✅ Complete | 🟢 High |
| Domain registration | ⚠️ Needs Testing | 🟡 Medium |
| DNS automation | ⚠️ Needs Testing | 🟡 Medium |
| Front Door routing | ⚠️ Needs Testing | 🟡 Medium |
| Multi-language UI (EN, ES, FR) | ✅ Complete | 🟢 High |
| Stripe webhook handling | ✅ Complete | 🟢 High |

### Shipping But Not Critical

| Feature | Status | Notes |
|---------|--------|-------|
| Author invitations | ✅ Complete | Nice bonus feature |
| Testimonials API | ✅ Complete | Can add post-launch |
| Lead capture | ✅ Complete | Marketing tool |
| Referral tracking | ✅ Complete | Growth feature |
| A/B testing framework | ✅ Complete | For optimization |
| Platform stats | ✅ Complete | Internal analytics |

### Explicitly NOT Shipping (Deferred)

| Feature | Priority | Defer Until |
|---------|----------|-------------|
| Image CDN optimization | Medium | Month 2 |
| Advanced analytics dashboard | Low | Month 3 |
| Email marketing integration | Low | Month 2 |
| SEO optimization tools | Medium | Month 2 |
| Mobile app | Low | Month 6+ |
| API rate limiting | Medium | Month 1 (if needed) |
| User-facing analytics | Low | Month 3 |

---

## 🚨 Show-Stopper Issues (Launch Cancelled If Found)

1. **Payment processing completely broken** - Cannot charge customers
2. **Domain registration fundamentally broken** - Core value prop fails
3. **Data loss or corruption** - Customer data not safe
4. **Critical security vulnerability** - Customer data exposed
5. **Authentication bypass** - Unauthorized access possible

**Mitigation:** Thorough testing in Stages 1-3 should prevent these

---

## 📊 Launch Success Criteria (First 30 Days)

### Technical Success
- ✅ Zero critical incidents (P0/P1)
- ✅ API uptime > 99.5%
- ✅ Domain registration success rate > 95%
- ✅ Payment processing success rate > 98%
- ✅ Average domain activation time < 24 hours

### Business Success
- ✅ 25+ paid subscriptions
- ✅ 15+ custom domains registered
- ✅ $500+ Monthly Recurring Revenue (MRR)
- ✅ Customer satisfaction > 4.0/5 (if survey implemented)
- ✅ < 10% churn rate in first month

### Operational Success
- ✅ All incidents resolved within SLA
- ✅ Customer support response time < 4 hours
- ✅ Zero customer refunds due to technical issues
- ✅ Documentation used successfully by customers (if public)

---

## 🛑 Launch Blockers - Current Status

| Blocker | Status | ETA | Owner |
|---------|--------|-----|-------|
| Domain registration E2E test | 🔴 Not Started | 1 week | DevOps |
| DNS automation validation | 🔴 Not Started | 1 week | DevOps |
| Front Door integration test | 🔴 Not Started | 1 week | DevOps |

**GO/NO-GO DECISION POINT:** Cannot proceed to Stage 2 until all blockers are green (✅)

---

## 🎯 Minimum Team for Launch

- **1 DevOps Engineer** - Infrastructure, deployment, on-call
- **1 Backend Developer** - Bug fixes, feature tweaks (on-call backup)
- **1 Product Owner** - Customer communication, prioritization
- **0.5 QA Engineer** - Testing support (part-time OK)
- **Optional: Customer Support** - Can be handled by Product Owner initially

---

## 📋 Daily Stand-Up During Launch Week

**Every day until Stage 5 complete:**

1. **Blockers?** - What's preventing progress?
2. **Yesterday** - What was completed?
3. **Today** - What's the plan?
4. **Risks** - Any concerns emerging?
5. **Metrics** - Are we hitting targets?

**Duration:** 15 minutes max  
**Time:** Morning (10 AM local time)  
**Format:** Video call + Slack updates

---

## 📞 Emergency Contacts

**Critical Issue (Payment/Auth Down):**  
Contact: [TBD] - [phone] - Available 24/7

**High Priority (Domain Issues):**  
Contact: [TBD] - [phone] - Available business hours + on-call rotation

**General Support:**  
Contact: [TBD] - [email] - Response within 4 hours

---

## 📝 Post-Launch Retrospective (Schedule After 30 Days)

**Questions to Answer:**
1. What went well?
2. What went wrong?
3. What surprised us?
4. What would we do differently?
5. What should we prioritize next?

**Attendees:** Full team + stakeholders

---

## 🎉 Launch Celebration Plan

**When Stage 5 complete:**
- Team celebration (virtual or in-person)
- Thank you notes to beta testers
- Social media announcement
- Internal company announcement

---

**Document Owner:** Product Team  
**Last Updated:** 2026-02-11  
**Next Review:** After Stage 1 blockers resolved

---

## Appendix: Quick Commands

### Data Seeding (Production)
```bash
cd SeedInkStainedWretchesLocale && dotnet run
cd ../SeedCountries && dotnet run
cd ../SeedLanguages && dotnet run
cd ../OnePageAuthor.DataSeeder && dotnet run
cd ../SeedImageStorageTiers && dotnet run
```

### Smoke Test Script (To Be Created)
```bash
# Run smoke tests against production
cd OnePageAuthor.Test
dotnet test --filter "Category=Smoke" --logger "console;verbosity=detailed"
```

### Monitoring Dashboard
- Application Insights: `https://portal.azure.com` → Search "Application Insights"
- Stripe Dashboard: `https://dashboard.stripe.com`
- Azure Front Door: `https://portal.azure.com` → Search "Front Door"
- Cosmos DB: `https://portal.azure.com` → Search "Cosmos DB"
