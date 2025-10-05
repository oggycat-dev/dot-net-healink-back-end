# Refactor: Move Saga from SharedLibrary to AuthService

## ❌ Problem
- `RegistrationSaga` and `RegistrationSagaState` were in SharedLibrary
- This caused EVERY microservice to have saga tables in their database
- SubscriptionService, UserService had unnecessary saga tables
- Violated single responsibility - saga should only live in owning service

## ✅ Solution
1. **Moved Saga to AuthService**:
   - `RegistrationSaga` → `AuthService.Infrastructure.Saga.RegistrationSaga`
   - `RegistrationSagaState` → `AuthService.Infrastructure.Saga.RegistrationSagaState`

2. **Made MassTransit Configuration Generic**:
   - `AddMassTransitWithSaga<TDbContext>()` now accepts:
     - `configureSagas` - Configure saga state machines
     - `configureConsumers` - Configure consumers
     - `configureEndpoints` - Configure custom endpoints
   - Removed hard-coded `RegistrationSaga` from SharedLibrary

3. **Created AuthService-specific Saga Configuration**:
   - `AuthSagaConfiguration.ConfigureRegistrationSaga()` - Saga setup
   - `AuthSagaConfiguration.ConfigureSagaEndpoints()` - Endpoint configuration
   - `AuthSagaDbContextExtensions.AddAuthSagaEntities()` - DbContext setup

4. **Updated AuthService**:
   - `ServiceConfiguration` now uses new configuration methods
   - `AuthDbContext` uses `AddAuthSagaEntities()` instead of `AddSagaEntities()`

## 📁 Files Changed
- ✅ Created: `AuthService.Infrastructure.Saga.RegistrationSaga`
- ✅ Created: `AuthService.Infrastructure.Saga.RegistrationSagaState`
- ✅ Created: `AuthService.Infrastructure.Configurations.AuthSagaConfiguration`
- ✅ Created: `AuthService.Infrastructure.Extensions.AuthSagaDbContextExtensions`
- ✅ Updated: `SharedLibrary.Commons.Configurations.MassTransitSagaConfiguration`
- ✅ Updated: `AuthService.API.Configurations.ServiceConfiguration`
- ✅ Updated: `AuthService.Infrastructure.Context.AuthDbContext`

## 🗑️ Old Files (Keep for backward compatibility - will deprecate later)
- `SharedLibrary.Contracts.User.Saga.RegistrationSaga` (deprecated)
- `SharedLibrary.Contracts.User.Saga.RegistrationSagaState` (deprecated)
- `SharedLibrary.Commons.Extensions.SagaDbContextExtensions` (deprecated)

## 📝 Next Steps
1. Create new migration for AuthService: `Add-Migration MoveSagaToAuthService`
2. Test registration flow
3. Remove old saga files from SharedLibrary after verification
4. Update other services if they mistakenly reference old saga

## 🎯 Benefits
- ✅ Each service owns its saga tables
- ✅ Clear separation of concerns
- ✅ No unnecessary tables in other services
- ✅ Better maintainability
- ✅ Easier to add new sagas per service
