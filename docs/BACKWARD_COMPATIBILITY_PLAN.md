# Backward Compatibility Plan for Multi-Domain Invitations

## Overview

This document explains how existing pending invitations are handled with the new multi-domain feature and provides a migration plan for administrators.

## Executive Summary

✅ **Existing pending invitations WILL NOT break** with this update.
✅ **No immediate action required** - the system includes automatic migration.
✅ **Optional migration tool** available for explicit data consistency.

---

## How Existing Invitations Are Protected

### 1. Automatic Migration in Entity

The `AuthorInvitation` entity includes automatic migration logic:

```csharp
public List<string> DomainNames 
{ 
    get 
    {
        // If DomainNames is empty but DomainName has a value,
        // automatically populate DomainNames from DomainName
        if ((_domainNames == null || _domainNames.Count == 0) && 
            !string.IsNullOrWhiteSpace(DomainName))
        {
            _domainNames = new List<string> { DomainName };
        }
        return _domainNames;
    }
    set 
    {
        _domainNames = value ?? new List<string>();
    }
}
```

**What this means:**
- When an old invitation is read from the database, if `DomainNames` is empty, it automatically gets populated from `DomainName`
- This happens transparently at runtime
- No database changes required
- No breaking changes to existing data

### 2. Backward Compatibility in API

The API maintains backward compatibility:

**Old format still works:**
```json
{
  "emailAddress": "author@example.com",
  "domainName": "example.com"
}
```

**New format also works:**
```json
{
  "emailAddress": "author@example.com",
  "domainNames": ["example.com", "author-site.com"]
}
```

Both are accepted and handled correctly.

### 3. Response Format

API responses include both fields for compatibility:

```json
{
  "id": "abc-123",
  "emailAddress": "author@example.com",
  "domainName": "example.com",        // First domain (backward compatibility)
  "domainNames": ["example.com"],     // All domains (new feature)
  "status": "Pending",
  ...
}
```

---

## Scenarios and Outcomes

### Scenario 1: Existing Invitation (Old Format in DB)

**Database record:**
```json
{
  "id": "abc-123",
  "EmailAddress": "author@example.com",
  "DomainName": "example.com",
  "Status": "Pending",
  "CreatedAt": "2024-01-01T00:00:00Z",
  "ExpiresAt": "2024-01-31T00:00:00Z"
}
```

**What happens when read:**
- Entity deserializes with `DomainName = "example.com"`
- Entity deserializes with `DomainNames = []` (empty)
- Automatic migration kicks in
- `DomainNames` getter returns `["example.com"]`

**Result:**
✅ API returns both `domainName` and `domainNames` correctly
✅ Console tool sees the domain in `DomainNames`
✅ All operations work normally
✅ No data loss

### Scenario 2: List Pending Invitations

**GET /api/author-invitations**

Old invitations in the list will show:
```json
{
  "id": "abc-123",
  "emailAddress": "author@example.com",
  "domainName": "example.com",
  "domainNames": ["example.com"],  // Automatically migrated
  "status": "Pending"
}
```

✅ Works correctly

### Scenario 3: Update Existing Invitation

**PUT /api/author-invitations/abc-123**

When you update an old invitation:
1. Record is read from DB
2. Automatic migration populates `DomainNames`
3. Update is applied
4. Record is saved back with explicit `DomainNames`

After first update, the record in DB will have:
```json
{
  "id": "abc-123",
  "EmailAddress": "author@example.com",
  "DomainName": "example.com",
  "DomainNames": ["example.com", "newdomain.com"],  // Now explicit
  "LastUpdatedAt": "2024-01-15T00:00:00Z"
}
```

✅ Migration happens automatically on first update

### Scenario 4: Resend Email for Existing Invitation

**POST /api/author-invitations/abc-123/resend**

1. Record is read with automatic migration
2. Email is sent with all domains from `DomainNames`
3. Record is updated with `LastEmailSentAt`
4. `DomainNames` becomes explicit in DB

✅ Works correctly and migrates data

---

## Migration Options

### Option A: Do Nothing (Recommended)

**When to choose:**
- You have few pending invitations
- You don't mind lazy migration (happens on first access)
- You trust the automatic migration logic

**Pros:**
- ✅ Zero effort
- ✅ Zero downtime
- ✅ No risk of migration errors
- ✅ Works immediately

**Cons:**
- ⚠️ DB records remain in old format until accessed
- ⚠️ Relies on automatic migration logic

### Option B: Run Migration Tool

**When to choose:**
- You have many pending invitations
- You want explicit data consistency
- You want to validate migration before going live
- You prefer not to rely on automatic migration

**Pros:**
- ✅ Explicit data consistency
- ✅ One-time operation
- ✅ Clear audit trail
- ✅ Can validate results

**Cons:**
- ⚠️ Requires running a tool
- ⚠️ Need database write access

**How to run:**

```bash
cd MigrateAuthorInvitations
dotnet run
```

See [MigrateAuthorInvitations/README.md](../MigrateAuthorInvitations/README.md) for details.

---

## Testing Plan

### Test 1: Read Old Invitation

1. Create a test invitation in old format (using Cosmos DB Data Explorer):
```json
{
  "id": "test-old-123",
  "EmailAddress": "test@example.com",
  "DomainName": "example.com",
  "Status": "Pending",
  "CreatedAt": "2024-01-01T00:00:00Z",
  "ExpiresAt": "2024-01-31T00:00:00Z"
}
```

2. Call GET /api/author-invitations/test-old-123

3. Verify response includes both fields:
```json
{
  "domainName": "example.com",
  "domainNames": ["example.com"]
}
```

### Test 2: Update Old Invitation

1. Use the test invitation from Test 1

2. Call PUT /api/author-invitations/test-old-123:
```json
{
  "domainNames": ["example.com", "newdomain.com"],
  "notes": "Updated"
}
```

3. Verify update succeeds

4. Call GET to verify:
```json
{
  "domainNames": ["example.com", "newdomain.com"]
}
```

### Test 3: List with Mixed Invitations

1. Create one old-format and one new-format invitation

2. Call GET /api/author-invitations

3. Verify both show up correctly with `domainNames` populated

### Test 4: Console Tool

1. Run: `AuthorInvitationTool list`

2. Verify old invitations show domains correctly

---

## Rollback Plan

If issues are discovered:

### Immediate Rollback

1. Revert to previous version of API/console tool
2. Old invitations still work (they never changed)
3. New invitations with multiple domains will appear as first domain only

### Data Cleanup (if needed)

If you ran the migration tool and want to revert:

1. The old `DomainName` field is preserved
2. Just remove `DomainNames` from documents if desired
3. Or leave it - it doesn't hurt anything

---

## FAQ

### Q: Will existing pending invitations break?

**A:** No. The automatic migration ensures they work correctly.

### Q: Do I need to run the migration tool?

**A:** No, it's optional. Automatic migration handles everything at runtime.

### Q: What if I have thousands of pending invitations?

**A:** They will all work. Consider running the migration tool for explicit consistency, but it's not required.

### Q: Can I mix old and new format invitations?

**A:** Yes, absolutely. The system handles both seamlessly.

### Q: What happens to the old `DomainName` field?

**A:** It's preserved for backward compatibility. New invitations populate both `DomainName` (first domain) and `DomainNames` (all domains).

### Q: Is there any performance impact?

**A:** The automatic migration happens in the getter, so there's negligible performance impact (simple list check and assignment).

### Q: What if someone updates an old invitation?

**A:** On the first update, `DomainNames` becomes explicit in the database. From then on, no automatic migration is needed for that record.

---

## Summary

| Aspect | Status |
|--------|--------|
| Existing invitations work | ✅ Yes, automatically |
| Database migration required | ❌ No |
| Breaking changes | ❌ None |
| Automatic migration | ✅ Built-in |
| Optional migration tool | ✅ Available |
| Rollback capability | ✅ Possible |
| API backward compatibility | ✅ Maintained |
| Console tool compatibility | ✅ Maintained |

**Recommendation:** Deploy with confidence. The automatic migration ensures zero breaking changes.
