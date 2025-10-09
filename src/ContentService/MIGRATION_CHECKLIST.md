# API Migration Checklist

## 📋 Overview

Checklist để migrate từ old controllers sang new API structure (User/Creator/CMS).

---

## ✅ Phase 1: Infrastructure Setup (COMPLETED)

- [x] Tạo folder structure mới (`User/`, `Creator/`, `Cms/`)
- [x] Implement `UserPodcastsController`
- [x] Implement `CreatorPodcastsController`
- [x] Implement `CmsPodcastsController`
- [x] Implement `UserCommunityController`
- [x] Implement `CreatorCommunityController`
- [x] Implement `CmsCommunityController`
- [x] Implement `CreatorFileUploadController`
- [x] Configure Swagger with API groups
- [x] Update `ServiceConfiguration.cs`
- [x] Create documentation (`API_ARCHITECTURE.md`)
- [x] Build successfully without errors

---

## 🧪 Phase 2: Testing & Validation (IN PROGRESS)

### User APIs Testing
- [ ] Test GET `/api/user/podcasts` - List published podcasts
- [ ] Test GET `/api/user/podcasts/{id}` - Get podcast detail
- [ ] Test GET `/api/user/podcasts/by-emotion/{emotion}` - Filter by emotion
- [ ] Test GET `/api/user/podcasts/by-topic/{topic}` - Filter by topic
- [ ] Test GET `/api/user/podcasts/series/{name}` - Filter by series
- [ ] Test GET `/api/user/podcasts/search?keyword=` - Search
- [ ] Test GET `/api/user/podcasts/trending` - Trending podcasts
- [ ] Test GET `/api/user/podcasts/latest` - Latest podcasts
- [ ] Test GET `/api/user/community/stories` - List stories
- [ ] Verify all endpoints work without authentication
- [ ] Verify only published content is returned

### Creator APIs Testing
- [ ] Generate test JWT token for ContentCreator role
- [ ] Test GET `/api/creator/podcasts/my-podcasts` - List my podcasts
- [ ] Test POST `/api/creator/podcasts` - Create new podcast
- [ ] Test PUT `/api/creator/podcasts/{id}` - Update podcast
- [ ] Test DELETE `/api/creator/podcasts/{id}` - Delete podcast
- [ ] Test POST `/api/creator/upload/podcast/audio` - Upload audio
- [ ] Test POST `/api/creator/upload/podcast/thumbnail` - Upload thumbnail
- [ ] Test POST `/api/creator/community/stories` - Create story
- [ ] Verify ownership checks work correctly
- [ ] Verify 401 Unauthorized for unauthenticated requests
- [ ] Verify 403 Forbidden for unauthorized access

### CMS APIs Testing
- [ ] Generate test JWT token for CommunityModerator role
- [ ] Test GET `/api/cms/podcasts` - List all podcasts
- [ ] Test GET `/api/cms/podcasts/pending` - List pending podcasts
- [ ] Test POST `/api/cms/podcasts/{id}/approve` - Approve podcast
- [ ] Test POST `/api/cms/podcasts/{id}/reject` - Reject podcast
- [ ] Test GET `/api/cms/community/stories` - List all stories
- [ ] Test POST `/api/cms/community/stories/{id}/approve` - Approve story
- [ ] Generate test JWT token for Admin role
- [ ] Test DELETE `/api/cms/podcasts/{id}` - Force delete (admin only)
- [ ] Test POST `/api/cms/podcasts/bulk-approve` - Bulk approve
- [ ] Verify moderator role requirements
- [ ] Verify admin-only endpoints

### Swagger UI Testing
- [ ] Verify "User APIs" group displays correctly
- [ ] Verify "Creator APIs" group displays correctly
- [ ] Verify "CMS APIs" group displays correctly
- [ ] Test JWT authorization in Swagger UI
- [ ] Verify all endpoints have proper documentation
- [ ] Check XML comments are displayed

---

## 🔧 Phase 3: Feature Implementation (PLANNED)

### Missing Features to Implement
- [ ] Implement view tracking (increment view count)
- [ ] Implement like functionality
- [ ] Implement favorite/bookmark functionality
- [ ] Add GetPodcastsByCreatorId filter in repository
- [ ] Implement sorting options (by date, popularity, views)
- [ ] Add comprehensive analytics endpoints
- [ ] Implement creator dashboard with real data
- [ ] Add moderation activity logging
- [ ] Implement bulk operations for CMS
- [ ] Add notification system (approve/reject alerts)

### Query/Command Enhancements
- [ ] Add `CreatedBy` filter to `GetPodcastsQuery`
- [ ] Add `SortBy` and `SortOrder` to queries
- [ ] Implement `GetPodcastAnalyticsQuery`
- [ ] Implement `IncrementViewCountCommand`
- [ ] Implement `LikePodcastCommand`
- [ ] Implement `FavoritePodcastCommand`
- [ ] Implement `GetUserFavoritesQuery`

### Repository Enhancements
- [ ] Add `GetPodcastsByCreatorIdAsync` method
- [ ] Add sorting support in `GetPodcastsAsync`
- [ ] Add `IncrementViewCountAsync` method
- [ ] Add analytics methods

---

## 🌐 Phase 4: Frontend Integration (PLANNED)

### Frontend Updates Needed
- [ ] Update API client to use new User endpoints
- [ ] Create creator dashboard using Creator APIs
- [ ] Create admin panel using CMS APIs
- [ ] Update authentication to include role-based access
- [ ] Test all frontend flows with new APIs
- [ ] Update API documentation for frontend team

### API Client Updates
- [ ] Update `PodcastService` to use `/api/user/podcasts`
- [ ] Create `CreatorPodcastService` for `/api/creator/podcasts`
- [ ] Create `CmsPodcastService` for `/api/cms/podcasts`
- [ ] Update error handling for new response formats
- [ ] Add retry logic for file uploads

---

## 🗑️ Phase 5: Deprecation & Cleanup (FUTURE)

### Deprecate Old Controllers
- [ ] Mark old `PodcastsController` as `[Obsolete]`
- [ ] Mark old `CommunityController` as `[Obsolete]`
- [ ] Mark old `FileUploadController` as `[Obsolete]`
- [ ] Add deprecation notices in Swagger
- [ ] Update documentation to point to new endpoints

### Remove Old Code
- [ ] Remove old `PodcastsController.cs`
- [ ] Remove old `CommunityController.cs`
- [ ] Remove old `FileUploadController.cs`
- [ ] Clean up unused DTOs
- [ ] Remove unused dependencies

### Code Quality
- [ ] Add unit tests for new controllers
- [ ] Add integration tests
- [ ] Fix all compiler warnings
- [ ] Run code analysis and fix issues
- [ ] Update code coverage

---

## 📊 Phase 6: Performance & Optimization (FUTURE)

### Performance Improvements
- [ ] Add caching for published podcasts
- [ ] Implement pagination optimization
- [ ] Add database indexes for common queries
- [ ] Optimize file upload performance
- [ ] Add CDN for static content

### Monitoring & Logging
- [ ] Add detailed logging for all operations
- [ ] Implement request tracing
- [ ] Add performance metrics
- [ ] Set up alerts for errors
- [ ] Create dashboard for monitoring

---

## 📝 Testing Scripts

### PowerShell Test Script

Create `test-content-apis.ps1`:

```powershell
# Test User APIs (No Auth)
Write-Host "Testing User APIs..." -ForegroundColor Green

$baseUrl = "http://localhost:5003"

# Test get podcasts
$response = Invoke-RestMethod -Uri "$baseUrl/api/user/podcasts" -Method Get
Write-Host "✓ GET /api/user/podcasts - $($response.totalCount) podcasts found"

# Test search
$response = Invoke-RestMethod -Uri "$baseUrl/api/user/podcasts/search?keyword=test" -Method Get
Write-Host "✓ GET /api/user/podcasts/search"

# Test Creator APIs (Auth Required)
Write-Host "`nTesting Creator APIs..." -ForegroundColor Yellow

$token = "YOUR_JWT_TOKEN_HERE"
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/creator/podcasts/my-podcasts" -Method Get -Headers $headers
    Write-Host "✓ GET /api/creator/podcasts/my-podcasts - $($response.totalCount) podcasts found"
} catch {
    Write-Host "✗ Creator API test failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test CMS APIs (Admin Auth Required)
Write-Host "`nTesting CMS APIs..." -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/cms/podcasts/pending" -Method Get -Headers $headers
    Write-Host "✓ GET /api/cms/podcasts/pending - $($response.totalCount) pending podcasts"
} catch {
    Write-Host "✗ CMS API test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nTest completed!" -ForegroundColor Green
```

---

## 🎯 Success Criteria

### Must Have (Phase 2)
- ✅ All new controllers compile without errors
- [ ] All User APIs work without authentication
- [ ] All Creator APIs work with ContentCreator token
- [ ] All CMS APIs work with Moderator/Admin token
- [ ] Swagger UI displays 3 groups correctly

### Should Have (Phase 3)
- [ ] Analytics endpoints implemented
- [ ] Interaction features (like, favorite) implemented
- [ ] Creator dashboard has real data
- [ ] Moderation logging works

### Nice to Have (Phase 4-6)
- [ ] Frontend fully migrated to new APIs
- [ ] Old controllers deprecated and removed
- [ ] Comprehensive test coverage
- [ ] Performance optimization done
- [ ] Monitoring and alerts set up

---

## 📅 Timeline Estimate

- **Phase 1**: ✅ Completed (2-3 hours)
- **Phase 2**: 🔄 In Progress (1-2 days for testing)
- **Phase 3**: ⏳ Planned (3-5 days for feature implementation)
- **Phase 4**: ⏳ Planned (1-2 weeks for frontend integration)
- **Phase 5**: ⏳ Future (1 week for cleanup)
- **Phase 6**: ⏳ Future (1-2 weeks for optimization)

---

## 📞 Support & Questions

For questions or issues during migration:

1. Check `API_ARCHITECTURE.md` for detailed API documentation
2. Review Swagger UI for endpoint details
3. Check logs for error messages
4. Verify JWT token has correct roles

---

## 🎉 Completion Status

**Current Phase**: Phase 2 - Testing & Validation
**Overall Progress**: 20% (1 of 6 phases completed)
**Next Action**: Test User APIs endpoints

---

Last Updated: 2025-10-07
