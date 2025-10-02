# ✅ Registration Saga Fix - Testing Checklist

## 🎯 Objective
Verify that SubscriptionService NO longer hosts RegistrationSaga and registration flow works end-to-end.

---

## 📋 Pre-Test Setup

### 1. Ensure Services are Running
```powershell
# Check all services are healthy
docker ps

# Expected: All services should show (healthy) status
# ✅ authservice-api (healthy)
# ✅ userservice-api (healthy)
# ✅ notificationservice-api (healthy)
# ✅ subscriptionservice-api (healthy)
# ✅ contentservice-api (healthy)
# ✅ paymentservice-api (healthy)
# ✅ gateway-api (healthy)
# ✅ postgres (healthy)
# ✅ rabbitmq (healthy)
# ✅ redis (healthy)
```

### 2. Clear Previous Test Data (Optional)
```powershell
# Clear users from previous tests
docker exec -it postgres psql -U healink -d authservicedb -c "DELETE FROM \"AspNetUsers\" WHERE \"Email\" LIKE 'test%@example.com';"
docker exec -it postgres psql -U healink -d userservicedb -c "DELETE FROM \"UserProfiles\" WHERE \"Email\" LIKE 'test%@example.com';"
```

---

## ✅ Test 1: Verify No Saga Logs in SubscriptionService

### Steps:
```powershell
# Clear logs
docker-compose restart subscriptionservice-api

# Wait 10 seconds for service to start
Start-Sleep -Seconds 10

# Check for RegistrationSaga logs
docker-compose logs subscriptionservice-api | Select-String "RegistrationSaga"
```

### Expected Result:
```
(No output - completely empty)
```

### ✅ PASS Criteria:
- [ ] NO logs containing "RegistrationSaga" in SubscriptionService
- [ ] NO logs containing "NEW RegistrationSaga instance created"
- [ ] NO logs containing "Saga .* initialized successfully"

### ❌ FAIL Indicators:
- Any log line with "RegistrationSaga" in SubscriptionService

---

## ✅ Test 2: Verify Saga Logs ONLY in AuthService

### Steps:
```powershell
# Clear logs
docker-compose restart authservice-api

# Wait 10 seconds
Start-Sleep -Seconds 10

# Register a new user to trigger Saga
$registerBody = @{
    email = "test-saga@example.com"
    password = "Test@123456"
    fullName = "Saga Test User"
    phoneNumber = "0987654321"
    otpSentChannel = "Email"
} | ConvertTo-Json

Invoke-WebRequest -Uri "http://localhost:5000/api/auth/register" `
    -Method POST `
    -ContentType "application/json" `
    -Body $registerBody

# Wait 5 seconds for Saga to process
Start-Sleep -Seconds 5

# Check AuthService logs for Saga
docker-compose logs authservice-api | Select-String "RegistrationSaga" | Select-Object -Last 10
```

### Expected Result:
```log
info: SharedLibrary.Contracts.User.Saga.RegistrationSaga[0]
      NEW RegistrationSaga instance created - Email: test-saga@example.com, 
      CorrelationId: <some-guid>

info: SharedLibrary.Contracts.User.Saga.RegistrationSaga[0]
      Saga <correlation-id> initialized successfully - Email: test-saga@example.com
```

### ✅ PASS Criteria:
- [ ] Saga logs appear in AuthService
- [ ] Saga instance created with correct email
- [ ] Saga initialized successfully

---

## ✅ Test 3: Complete Registration Flow End-to-End

### Step 1: Register User
```powershell
$correlationId = [guid]::NewGuid().ToString()
$testEmail = "test-e2e-$correlationId@example.com".Substring(0, 40)

$registerBody = @{
    email = $testEmail
    password = "Test@123456"
    fullName = "E2E Test User"
    phoneNumber = "0123456789"
    otpSentChannel = "Email"
} | ConvertTo-Json

$response = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/register" `
    -Method POST `
    -ContentType "application/json" `
    -Body $registerBody

Write-Host "Registration Response: $($response.StatusCode)"
Write-Host $response.Content
```

### Expected:
- [ ] HTTP 200 OK
- [ ] Response: "User registration started. Please check your email for OTP verification."

### Step 2: Get OTP from Logs
```powershell
# Wait for OTP notification
Start-Sleep -Seconds 3

# Get OTP code from NotificationService logs
$otpLogs = docker-compose logs notificationservice-api | Select-String $testEmail | Select-Object -Last 5
Write-Host "OTP Logs:"
$otpLogs

# Extract OTP manually from logs (look for 6-digit code)
$otpCode = Read-Host "Enter OTP code from logs above"
```

### Step 3: Verify OTP
```powershell
$verifyBody = @{
    contact = $testEmail
    otpCode = $otpCode
    otpType = "Registration"
    otpSentChannel = "Email"
} | ConvertTo-Json

$verifyResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/verify-otp" `
    -Method POST `
    -ContentType "application/json" `
    -Body $verifyBody

Write-Host "Verify Response: $($verifyResponse.StatusCode)"
Write-Host $verifyResponse.Content
```

### Expected:
- [ ] HTTP 200 OK
- [ ] Response: "OTP verified successfully. Creating your account..."

### Step 4: Verify User Created in AuthService
```powershell
# Wait for user creation
Start-Sleep -Seconds 5

# Check AuthService database
docker exec -it postgres psql -U healink -d authservicedb -c "SELECT \"Id\", \"Email\", \"EmailConfirmed\" FROM \"AspNetUsers\" WHERE \"Email\" = '$testEmail';"
```

### Expected:
```
                  Id                  |          Email           | EmailConfirmed
--------------------------------------+--------------------------+----------------
 <some-guid>                          | test-e2e-*@example.com   | t
(1 row)
```

- [ ] User exists in AspNetUsers table
- [ ] EmailConfirmed = true

### Step 5: Verify UserProfile Created in UserService
```powershell
docker exec -it postgres psql -U healink -d userservicedb -c "SELECT \"Id\", \"Email\", \"FullName\", \"Status\" FROM \"UserProfiles\" WHERE \"Email\" = '$testEmail';"
```

### Expected:
```
                  Id                  |          Email           |    FullName     | Status
--------------------------------------+--------------------------+-----------------+--------
 <some-guid>                          | test-e2e-*@example.com   | E2E Test User   | Active
(1 row)
```

- [ ] UserProfile exists in UserProfiles table
- [ ] FullName matches registration data
- [ ] Status = Active

---

## ✅ Test 4: Verify Saga Workflow with Logs

### Check Complete Saga Flow:
```powershell
# Get all logs for the test email
$allLogs = docker-compose logs | Select-String $testEmail
$allLogs | Out-File -FilePath "saga-flow-logs.txt"

# Display key events
Write-Host "`n=== SAGA FLOW ANALYSIS ==="
$allLogs | Select-String "RegistrationStarted|OtpSent|OtpVerified|AuthUserCreated|UserProfileCreated"
```

### Expected Event Sequence:
```log
1. [AuthService] RegistrationStarted - Email: test-e2e-*@example.com
2. [NotificationService] OTP notification sent successfully
3. [AuthService] OTP verified successfully
4. [AuthService] Publishing OtpVerified event
5. [AuthService] ✅ SAGA RECEIVED OtpVerified event!
6. [AuthService] Publishing CreateAuthUser
7. [AuthService] Auth user created successfully
8. [AuthService] Publishing AuthUserCreated event
9. [UserService] Processing CreateUserProfile
10. [UserService] User profile created successfully
11. [UserService] Publishing UserProfileCreated event
12. [NotificationService] Welcome notification sent
```

### ✅ PASS Criteria:
- [ ] All 12 events appear in correct order
- [ ] No error messages
- [ ] Saga transitions through all states: Started → OtpSent → OtpVerified → AuthUserCreated → UserProfileCreated → Completed

---

## ✅ Test 5: Database Validation

### Check RegistrationSagaStates Tables:
```powershell
# AuthService - SHOULD have RegistrationSagaStates table
docker exec -it postgres psql -U healink -d authservicedb -c "\dt" | Select-String "RegistrationSagaStates"

# SubscriptionService - should NOT have RegistrationSagaStates table
docker exec -it postgres psql -U healink -d subscriptiondb -c "\dt" | Select-String "RegistrationSagaStates"
```

### Expected:
```
AuthService: RegistrationSagaStates | table | healink  ✅ EXISTS
SubscriptionService: (No output) ✅ NOT EXISTS
```

### ✅ PASS Criteria:
- [ ] AuthService has RegistrationSagaStates table
- [ ] SubscriptionService does NOT have RegistrationSagaStates table

---

## 📊 Summary Checklist

### Configuration Verification
- [ ] SubscriptionService uses `AddMassTransitWithConsumers` (not `AddMassTransitWithSaga`)
- [ ] AuthService uses `AddMassTransitWithSaga<AuthDbContext>`
- [ ] UserService uses `AddMassTransitWithConsumers`
- [ ] NotificationService uses `AddMassTransitWithConsumers`

### Runtime Verification
- [ ] No RegistrationSaga logs in SubscriptionService
- [ ] RegistrationSaga logs appear ONLY in AuthService
- [ ] Complete registration flow works end-to-end
- [ ] User created in both AuthService and UserService
- [ ] Saga state transitions correctly

### Database Verification
- [ ] RegistrationSagaStates table exists in authservicedb
- [ ] RegistrationSagaStates table does NOT exist in subscriptiondb
- [ ] User data persisted correctly in both databases

---

## 🚨 Troubleshooting

### If Test Fails:

#### No Saga Logs in AuthService
```powershell
# Check if RabbitMQ is healthy
docker ps | Select-String rabbitmq

# Check RabbitMQ queues
docker exec -it rabbitmq rabbitmqctl list_queues

# Check AuthService is running
docker logs authservice-api --tail 50
```

#### User Not Created
```powershell
# Check for errors in logs
docker-compose logs authservice-api | Select-String "ERROR"
docker-compose logs userservice-api | Select-String "ERROR"

# Check Saga state
docker exec -it postgres psql -U healink -d authservicedb -c "SELECT * FROM \"RegistrationSagaStates\" ORDER BY \"CreatedAt\" DESC LIMIT 1;"
```

#### Saga Still Appears in SubscriptionService
```powershell
# Verify configuration file
cat src/SubscriptionService/SubscriptionService.API/Configurations/ServiceConfiguration.cs | Select-String "AddMassTransit"

# Should see: AddMassTransitWithConsumers
# Should NOT see: AddMassTransitWithSaga
```

---

## 📝 Test Results Template

```markdown
## Test Execution Results

**Date:** 2025-10-02  
**Tester:** [Your Name]  
**Environment:** Local Docker Compose

### Test Results:
- [ ] Test 1: No Saga Logs in SubscriptionService - PASS/FAIL
- [ ] Test 2: Saga Logs Only in AuthService - PASS/FAIL
- [ ] Test 3: Complete Registration Flow - PASS/FAIL
- [ ] Test 4: Saga Workflow Validation - PASS/FAIL
- [ ] Test 5: Database Validation - PASS/FAIL

### Issues Found:
(List any issues discovered during testing)

### Notes:
(Any additional observations or comments)
```

---

**Last Updated:** 2025-10-02  
**Version:** 1.0  
**Status:** Ready for Testing 🚀
