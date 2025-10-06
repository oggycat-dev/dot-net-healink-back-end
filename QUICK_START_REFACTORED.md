# 🚀 Quick Start: Run Refactored System

## 1️⃣ Apply Migration to AuthService

```powershell
# Navigate to AuthService Infrastructure
cd src/AuthService/AuthService.Infrastructure

# Create migration (if not already created)
dotnet ef migrations add MoveSagaToAuthService `
  --context AuthDbContext `
  --startup-project ../AuthService.API `
  --output-dir Migrations

# Review the migration file before applying!
# It should ADD:
# - RegistrationSagaStates table
# - InboxState table (if not exists)
# - OutboxMessage table (if not exists)  
# - OutboxState table (if not exists)

# Apply migration
dotnet ef database update `
  --context AuthDbContext `
  --startup-project ../AuthService.API

# Verify tables created
# Connect to your AuthService database and check:
# SELECT * FROM "RegistrationSagaStates" LIMIT 1;
```

## 2️⃣ Clean Up Other Services (Optional)

### Check SubscriptionService
```powershell
cd src/SubscriptionService/SubscriptionService.Infrastructure

# Check if RegistrationSagaStates table exists in SubscriptionService DB
# If exists, create migration to drop it
dotnet ef migrations add RemoveRegistrationSagaTable `
  --context SubscriptionDbContext `
  --startup-project ../SubscriptionService.API

# Review and apply
dotnet ef database update --context SubscriptionDbContext
```

## 3️⃣ Start Services

### Using Docker Compose (Recommended)
```powershell
# From root directory
docker-compose up --build -d

# Check logs
docker-compose logs -f authservice-api
```

### Using dotnet run (Development)
```powershell
# Terminal 1 - RabbitMQ & Redis (if not running)
docker-compose up -d rabbitmq redis postgres

# Terminal 2 - AuthService
cd src/AuthService/AuthService.API
dotnet run

# Terminal 3 - UserService
cd src/UserService/UserService.API
dotnet run

# Terminal 4 - NotificationService
cd src/NotificationService/NotificationService.API
dotnet run
```

## 4️⃣ Test Registration Flow

### Using curl
```powershell
# 1. Start Registration (generates OTP)
curl -X POST http://localhost:5001/api/v1/auth/register `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "Test@123456",
    "fullName": "Test User",
    "phoneNumber": "+1234567890",
    "channel": "Email"
  }'

# Response will include CorrelationId
# Check your email for OTP (or check logs in NotificationService)

# 2. Verify OTP
curl -X POST http://localhost:5001/api/v1/auth/verify-otp `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "otpCode": "123456"
  }'
```

## 5️⃣ Verify Saga in Database

```sql
-- Connect to AuthService database
-- Check saga created
SELECT 
    "CorrelationId",
    "Email",
    "CurrentState",
    "IsCompleted",
    "IsFailed",
    "CreatedAt",
    "CompletedAt"
FROM "RegistrationSagaStates"
ORDER BY "CreatedAt" DESC
LIMIT 10;

-- Check saga completed successfully
SELECT COUNT(*) as total_sagas,
       SUM(CASE WHEN "IsCompleted" = true THEN 1 ELSE 0 END) as completed,
       SUM(CASE WHEN "IsFailed" = true THEN 1 ELSE 0 END) as failed
FROM "RegistrationSagaStates";
```

## 6️⃣ Check RabbitMQ

1. Open RabbitMQ Management UI: http://localhost:15672
2. Login: guest/guest
3. Check queues:
   - `registration-saga` - Should exist and process messages
   - Check message rates
   - No messages in dead-letter queues

## 7️⃣ Common Issues & Solutions

### Issue 1: Migration Error - Table Already Exists
```
Solution: Drop the old saga table manually or use migration to handle it
```

### Issue 2: Saga Not Processing Messages
```powershell
# Check RabbitMQ connection
docker-compose logs rabbitmq

# Check AuthService logs
docker-compose logs authservice-api

# Verify queue exists
# Go to RabbitMQ UI → Queues → registration-saga should be there
```

### Issue 3: Database Connection Error
```powershell
# Check PostgreSQL is running
docker-compose ps postgres

# Check connection string in appsettings.Development.json
# Verify database exists
```

## 8️⃣ Rollback Plan

If something goes wrong:

```powershell
# Rollback database migration
cd src/AuthService/AuthService.Infrastructure
dotnet ef database update <PreviousMigrationName> --context AuthDbContext

# Rollback code
git stash
# or
git checkout main
```

## 📊 Health Check Endpoints

```powershell
# AuthService
curl http://localhost:5001/health

# UserService  
curl http://localhost:5002/health

# NotificationService
curl http://localhost:5003/health
```

## 🐛 Debugging Tips

### Check Saga State Transitions
```csharp
// Add logging in RegistrationSaga.cs (already there)
_logger.LogInformation("Saga state: {State}", context.Saga.CurrentState);
```

### Enable Detailed Logging
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MassTransit": "Debug",
      "AuthService.Infrastructure.Saga": "Debug"
    }
  }
}
```

### Check Message Flow
```powershell
# Watch logs in real-time
docker-compose logs -f --tail=100 authservice-api
```

## 📚 What Changed?

- ✅ Saga moved from SharedLibrary to AuthService
- ✅ Only AuthService has saga table now
- ✅ Other services are cleaner
- ✅ Better separation of concerns
- ✅ Same functionality, better architecture

## 🎯 Success Indicators

- ✅ Services start without errors
- ✅ Registration completes end-to-end
- ✅ Saga state saved in AuthService DB only
- ✅ Welcome email sent after registration
- ✅ No errors in service logs
- ✅ RabbitMQ queues processing correctly

## 📞 Need Help?

- Check: `SAGA_REFACTOR_COMPLETE_SUMMARY.md`
- Read: `docs/SAGA_ARCHITECTURE_GUIDE.md`
- Review: `SAGA_MIGRATION_CHECKLIST.md`

---

**Last Updated**: 2025-10-03  
**Tested On**: .NET 8.0, PostgreSQL 15, RabbitMQ 3.12
