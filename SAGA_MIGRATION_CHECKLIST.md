# Migration Checklist: Saga Refactor

## ✅ Pre-Migration Checklist

### 1. Backup Current State
- [ ] Backup all databases
- [ ] Commit current code state
- [ ] Tag release: `git tag pre-saga-refactor`
- [ ] Document current saga table locations

### 2. Verify Current State
```sql
-- Check saga tables in each service
-- AuthService (should have)
SELECT COUNT(*) FROM "RegistrationSagaStates";

-- SubscriptionService (shouldn't have - but might)
SELECT COUNT(*) FROM "RegistrationSagaStates";

-- UserService (shouldn't have)
-- Check if table exists
```

## 🔄 Migration Steps

### Step 1: AuthService - Add New Saga Implementation ✅
- [x] Create `AuthService.Infrastructure.Saga.RegistrationSaga`
- [x] Create `AuthService.Infrastructure.Saga.RegistrationSagaState`
- [x] Create `AuthService.Infrastructure.Configurations.AuthSagaConfiguration`
- [x] Create `AuthService.Infrastructure.Extensions.AuthSagaDbContextExtensions`
- [x] Update `AuthService.Infrastructure.Context.AuthDbContext`
- [x] Update `AuthService.API.Configurations.ServiceConfiguration`

### Step 2: Create Migration for AuthService
```powershell
cd src/AuthService/AuthService.Infrastructure

# Create migration
dotnet ef migrations add MoveSagaToAuthService `
  --context AuthDbContext `
  --output-dir Migrations `
  --startup-project ../AuthService.API

# Review migration before applying!
# Check that it adds RegistrationSagaStates table with correct schema
```

### Step 3: Apply Migration to AuthService
```powershell
# Apply to development database
dotnet ef database update --context AuthDbContext --startup-project ../AuthService.API

# Verify table created
# Should have: RegistrationSagaStates, InboxState, OutboxMessage, OutboxState
```

### Step 4: Test AuthService
- [ ] Start AuthService
- [ ] Check no startup errors
- [ ] Test registration flow end-to-end
- [ ] Verify saga state in database
- [ ] Check logs for saga transitions

### Step 5: Update Other Services (Remove Old Saga References)

#### SubscriptionService
```powershell
cd src/SubscriptionService/SubscriptionService.Infrastructure

# Check if saga table exists
# If exists, create migration to drop it
dotnet ef migrations add RemoveRegistrationSaga `
  --context SubscriptionDbContext `
  --output-dir Migrations `
  --startup-project ../SubscriptionService.API
```

#### Verify Other Services
- [ ] UserService - Check for saga table (should not exist)
- [ ] NotificationService - Check for saga table (should not exist)
- [ ] PaymentService - Check for saga table (should not exist)

### Step 6: Clean Up SharedLibrary
```powershell
# Mark as deprecated (don't delete yet - wait for full verification)
# Add [Obsolete] attribute to old files
```

## 🧪 Testing Checklist

### Functional Tests
- [ ] **Registration Flow**: Complete user registration
  - [ ] Start registration → Saga created
  - [ ] OTP sent → Saga transitions to OtpSent
  - [ ] OTP verified → Saga transitions to OtpVerified  
  - [ ] Auth user created → Saga transitions to AuthUserCreated
  - [ ] User profile created → Saga completes
  - [ ] Welcome email sent

- [ ] **Error Scenarios**:
  - [ ] Invalid OTP → Saga remains in OtpSent
  - [ ] User creation fails → Saga transitions to Failed
  - [ ] Profile creation fails → Saga rolls back (AuthUserCreated → RollingBack)

### Database Verification
```sql
-- AuthService DB should have:
SELECT COUNT(*) FROM "RegistrationSagaStates";
SELECT COUNT(*) FROM "InboxState";
SELECT COUNT(*) FROM "OutboxMessage";
SELECT COUNT(*) FROM "OutboxState";

-- Check saga states
SELECT "CorrelationId", "Email", "CurrentState", "IsCompleted", "IsFailed"
FROM "RegistrationSagaStates"
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

### Service Health Checks
- [ ] AuthService starts without errors
- [ ] UserService starts without errors
- [ ] NotificationService starts without errors
- [ ] SubscriptionService starts without errors
- [ ] All services connect to RabbitMQ
- [ ] All services connect to their databases

### RabbitMQ Verification
- [ ] `registration-saga` queue exists
- [ ] Saga consumes messages correctly
- [ ] No dead letter queue messages
- [ ] Message flow traces correctly

## 🚨 Rollback Plan (If Issues Occur)

### Quick Rollback
```powershell
# Rollback AuthService migration
cd src/AuthService/AuthService.Infrastructure
dotnet ef database update <PreviousMigrationName> --context AuthDbContext

# Revert code changes
git revert HEAD
git push
```

### Full Rollback
```powershell
# Restore from backup
# Checkout previous tag
git checkout pre-saga-refactor

# Redeploy services
```

## 📊 Post-Migration Verification

### Database State
- [ ] AuthService has saga tables ✅
- [ ] SubscriptionService NO saga tables ✅
- [ ] UserService NO saga tables ✅
- [ ] NotificationService NO saga tables ✅

### Code State
- [ ] Saga lives in AuthService.Infrastructure.Saga ✅
- [ ] SharedLibrary has generic configuration only ✅
- [ ] All services reference correct saga location ✅
- [ ] No orphaned saga references ✅

### Performance Metrics
- [ ] Registration latency unchanged
- [ ] Database query performance unchanged
- [ ] RabbitMQ message throughput unchanged
- [ ] Memory usage acceptable

## 🎯 Success Criteria

- ✅ All functional tests pass
- ✅ No errors in service logs
- ✅ Saga tables only in AuthService DB
- ✅ Registration flow works end-to-end
- ✅ Error handling works correctly
- ✅ Rollback scenarios tested
- ✅ Documentation updated

## 📝 Next Steps (After Successful Migration)

1. **Monitor Production** (if applicable)
   - [ ] Check error rates
   - [ ] Monitor saga completion rates
   - [ ] Track database performance
   - [ ] Monitor RabbitMQ queues

2. **Clean Up Old Code**
   - [ ] Add [Obsolete] to old saga files
   - [ ] Schedule deletion after 1 sprint
   - [ ] Update all references

3. **Document Lessons Learned**
   - [ ] Update architecture docs
   - [ ] Add to team wiki
   - [ ] Share with team

## 🔗 Related Documents
- `SAGA_REFACTOR_SUMMARY.md` - Technical changes summary
- `docs/SAGA_ARCHITECTURE_GUIDE.md` - Best practices guide
- `REGISTRATION_SAGA_FIX_SUMMARY.md` - Previous saga fixes

## 👥 Review Checklist
- [ ] Code review completed
- [ ] Architecture review completed
- [ ] DBA review completed (if applicable)
- [ ] QA sign-off
- [ ] Product owner notified
