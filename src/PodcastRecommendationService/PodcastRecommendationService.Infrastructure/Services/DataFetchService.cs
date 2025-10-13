using PodcastRecommendationService.Application.Services;
using PodcastRecommendationService.Application.DTOs;
using SharedLibrary.Commons.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SharedLibrary.Commons.Enums;

namespace PodcastRecommendationService.Infrastructure.Services;

/// <summary>
/// Service for fetching real data from other microservices
/// </summary>
public class DataFetchService : IDataFetchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DataFetchService> _logger;
    private readonly string _userServiceBaseUrl;
    private readonly string _contentServiceBaseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public DataFetchService(
        IHttpClientFactory httpClientFactory,
        ILogger<DataFetchService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("DataFetchClient");
        _logger = logger;
        
        // Get service URLs from configuration
        _userServiceBaseUrl = configuration.GetValue<string>("ServiceUrls:UserService") ?? "http://userservice-api";
        _contentServiceBaseUrl = configuration.GetValue<string>("ServiceUrls:ContentService") ?? "http://contentservice-api";
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        _logger.LogInformation("DataFetchService initialized with UserService: {UserService}, ContentService: {ContentService}",
            _userServiceBaseUrl, _contentServiceBaseUrl);
    }

    public async Task<Result<List<UserDataDto>>> GetAllUsersAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all users from UserService");

            // Call UserService internal API to get all users
            var response = await _httpClient.GetAsync($"{_userServiceBaseUrl}/api/internal/users");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("UserService returned error: {StatusCode}", response.StatusCode);
                return Result<List<UserDataDto>>.Failure("Failed to fetch users", ErrorCodeEnum.ExternalServiceError);
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("UserService response: {Response}", jsonContent);

            // Parse the response - adjust based on actual UserService API structure
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<UserApiDto>>>(jsonContent, _jsonOptions);
            
            if (apiResponse?.Data == null)
            {
                _logger.LogWarning("Invalid response from UserService");
                return Result<List<UserDataDto>>.Failure("Invalid response from UserService", ErrorCodeEnum.ExternalServiceError);
            }

            // Map to our DTOs
            var users = apiResponse.Data.Select(u => new UserDataDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName ?? string.Empty,
                LastName = u.LastName ?? string.Empty,
                CreatedAt = u.CreatedAt,
                Preferences = u.Preferences
            }).ToList();

            _logger.LogInformation("Successfully fetched {Count} users", users.Count);
            return Result<List<UserDataDto>>.Success(users);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching users from UserService");
            return Result<List<UserDataDto>>.Failure("UserService is unavailable", ErrorCodeEnum.ExternalServiceError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users from UserService");
            return Result<List<UserDataDto>>.Failure("Failed to fetch users", ErrorCodeEnum.InternalError);
        }
    }

    public async Task<Result<List<PodcastDataDto>>> GetAllPodcastsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all podcasts from ContentService");

            // Call ContentService user podcasts endpoint (published podcasts only)
            var response = await _httpClient.GetAsync($"{_contentServiceBaseUrl}/api/user/podcasts?page=1&pageSize=100");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ContentService returned error: {StatusCode}", response.StatusCode);
                return Result<List<PodcastDataDto>>.Failure("Failed to fetch podcasts", ErrorCodeEnum.ExternalServiceError);
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("ContentService response: {Response}", jsonContent.Substring(0, Math.Min(500, jsonContent.Length)));

            // Parse the paginated response from ContentService
            var podcastListResponse = JsonSerializer.Deserialize<PodcastListResponse>(jsonContent, _jsonOptions);
            
            if (podcastListResponse?.Podcasts == null)
            {
                _logger.LogWarning("Invalid response from ContentService");
                return Result<List<PodcastDataDto>>.Failure("Invalid response from ContentService", ErrorCodeEnum.ExternalServiceError);
            }

            // Map to our DTOs
            var podcasts = podcastListResponse.Podcasts
                .Select(p => new PodcastDataDto
                {
                    Id = p.Id.ToString(),
                    Title = p.Title,
                    Description = p.Description ?? string.Empty,
                    Category = GetCategoryFromTopics(p.TopicCategories) ?? "General",
                    Tags = p.Tags != null ? string.Join(",", p.Tags) : null,
                    CreatorId = p.CreatedBy.ToString(),
                    CreatedAt = p.CreatedAt,
                    Duration = ParseDuration(p.Duration),
                    AudioUrl = p.AudioUrl ?? string.Empty
                }).ToList();

            _logger.LogInformation("Successfully fetched {Count} podcasts from ContentService", podcasts.Count);
            return Result<List<PodcastDataDto>>.Success(podcasts);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching podcasts from ContentService");
            return Result<List<PodcastDataDto>>.Failure("ContentService is unavailable", ErrorCodeEnum.ExternalServiceError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching podcasts from ContentService");
            return Result<List<PodcastDataDto>>.Failure("Failed to fetch podcasts", ErrorCodeEnum.InternalError);
        }
    }

    public async Task<Result<List<UserRatingDto>>> GetUserRatingsAsync()
    {
        try
        {
            _logger.LogInformation("Generating synthetic user ratings from user interactions");

            // For now, we'll generate synthetic ratings based on user-podcast interactions
            // In the future, this could come from a dedicated analytics service or user behavior tracking

            var usersResult = await GetAllUsersAsync();
            var podcastsResult = await GetAllPodcastsAsync();

            if (!usersResult.IsSuccess || !podcastsResult.IsSuccess)
            {
                _logger.LogWarning("Failed to fetch users or podcasts for rating generation");
                return Result<List<UserRatingDto>>.Failure("Failed to generate ratings", ErrorCodeEnum.ExternalServiceError);
            }

            var users = usersResult.Data!;
            var podcasts = podcastsResult.Data!;
            var ratings = new List<UserRatingDto>();

            // Generate realistic ratings based on user creation date and podcast categories
            var random = new Random(42); // Fixed seed for reproducible results

            foreach (var user in users.Take(Math.Min(users.Count, 500))) // Limit to 500 users for performance
            {
                // Each user rates 5-20 podcasts
                var ratingCount = random.Next(5, 21);
                var userPodcasts = podcasts.OrderBy(x => random.Next()).Take(ratingCount);

                foreach (var podcast in userPodcasts)
                {
                    // Generate realistic ratings (3-5 for most, some 1-2)
                    var rating = random.NextDouble() < 0.8 
                        ? random.Next(3, 6) // 80% get good ratings (3-5)
                        : random.Next(1, 3); // 20% get poor ratings (1-2)

                    var interactionType = rating >= 4 ? "complete" : rating >= 3 ? "listen" : "skip";
                    var listenDuration = rating >= 3 
                        ? random.Next((int)(podcast.Duration * 0.5), podcast.Duration)
                        : random.Next(10, (int)(podcast.Duration * 0.3));

                    ratings.Add(new UserRatingDto
                    {
                        UserId = user.Id,
                        PodcastId = podcast.Id,
                        Rating = rating,
                        InteractionType = interactionType,
                        InteractionAt = user.CreatedAt.AddDays(random.Next(1, 30)),
                        ListenDuration = listenDuration,
                        Completed = rating >= 4
                    });
                }
            }

            _logger.LogInformation("Generated {Count} synthetic user ratings", ratings.Count);
            return Result<List<UserRatingDto>>.Success(ratings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating user ratings");
            return Result<List<UserRatingDto>>.Failure("Failed to generate ratings", ErrorCodeEnum.InternalError);
        }
    }

    public async Task<Result<UserDataDto>> GetUserByIdAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Fetching user {UserId} from UserService", userId);

            var response = await _httpClient.GetAsync($"{_userServiceBaseUrl}/api/internal/users/{userId}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Result<UserDataDto>.Failure("User not found", ErrorCodeEnum.NotFound);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("UserService returned error for user {UserId}: {StatusCode}", userId, response.StatusCode);
                return Result<UserDataDto>.Failure("Failed to fetch user", ErrorCodeEnum.ExternalServiceError);
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<UserApiDto>>(jsonContent, _jsonOptions);
            
            if (apiResponse?.Data == null)
            {
                return Result<UserDataDto>.Failure("Invalid response from UserService", ErrorCodeEnum.ExternalServiceError);
            }

            var user = new UserDataDto
            {
                Id = apiResponse.Data.Id,
                Email = apiResponse.Data.Email,
                FirstName = apiResponse.Data.FirstName ?? string.Empty,
                LastName = apiResponse.Data.LastName ?? string.Empty,
                CreatedAt = apiResponse.Data.CreatedAt,
                Preferences = apiResponse.Data.Preferences
            };

            return Result<UserDataDto>.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId}", userId);
            return Result<UserDataDto>.Failure("Failed to fetch user", ErrorCodeEnum.InternalError);
        }
    }

    public async Task<Result<PodcastDataDto>> GetPodcastByIdAsync(string podcastId)
    {
        try
        {
            _logger.LogInformation("Fetching podcast {PodcastId} from ContentService", podcastId);

            // Call ContentService podcast endpoint
            var response = await _httpClient.GetAsync($"{_contentServiceBaseUrl}/api/user/podcasts/{podcastId}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Result<PodcastDataDto>.Failure("Podcast not found", ErrorCodeEnum.NotFound);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ContentService returned error for podcast {PodcastId}: {StatusCode}", podcastId, response.StatusCode);
                return Result<PodcastDataDto>.Failure("Failed to fetch podcast", ErrorCodeEnum.ExternalServiceError);
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var podcastDto = JsonSerializer.Deserialize<ContentApiDto>(jsonContent, _jsonOptions);
            
            if (podcastDto == null)
            {
                return Result<PodcastDataDto>.Failure("Invalid response from ContentService", ErrorCodeEnum.ExternalServiceError);
            }

            var podcast = new PodcastDataDto
            {
                Id = podcastDto.Id.ToString(),
                Title = podcastDto.Title,
                Description = podcastDto.Description ?? string.Empty,
                Category = GetCategoryFromTopics(podcastDto.TopicCategories) ?? "General",
                Tags = podcastDto.Tags != null ? string.Join(",", podcastDto.Tags) : null,
                CreatorId = podcastDto.CreatedBy.ToString(),
                CreatedAt = podcastDto.CreatedAt,
                Duration = ParseDuration(podcastDto.Duration),
                AudioUrl = podcastDto.AudioUrl ?? string.Empty
            };

            return Result<PodcastDataDto>.Success(podcast);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching podcast {PodcastId}", podcastId);
            return Result<PodcastDataDto>.Failure("Failed to fetch podcast", ErrorCodeEnum.InternalError);
        }
    }

    // Helper methods for data transformation
    private string? GetCategoryFromTopics(List<int>? topicCategories)
    {
        if (topicCategories == null || topicCategories.Count == 0)
            return "General";

        // Map topic category IDs to category names
        // Based on ContentService TopicCategory enum
        var categoryMapping = new Dictionary<int, string>
        {
            { 1, "Health" },
            { 2, "Career" },
            { 3, "Psychology" },
            { 4, "Personal_Development" },
            { 5, "Lifestyle" },
            { 6, "Relationships" },
            { 7, "Finance" },
            { 8, "Education" }
        };

        var firstTopic = topicCategories.FirstOrDefault();
        return categoryMapping.TryGetValue(firstTopic, out var category) ? category : "General";
    }

    private int ParseDuration(string? duration)
    {
        if (string.IsNullOrEmpty(duration))
            return 0;

        // Try parse as integer minutes
        if (int.TryParse(duration, out var minutes))
            return minutes;

        // Try parse time format like "HH:MM:SS" or "MM:SS"
        if (TimeSpan.TryParse(duration, out var timeSpan))
            return (int)timeSpan.TotalMinutes;

        return 0;
    }
}

// Internal DTOs for API responses - adjust these based on actual service APIs
internal class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// DTO for ContentService paginated podcast list response
/// </summary>
internal class PodcastListResponse
{
    public List<ContentApiDto> Podcasts { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

internal class UserApiDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Preferences { get; set; }
}

/// <summary>
/// DTO matching ContentService podcast response format
/// Based on actual ContentService API response structure
/// </summary>
internal class ContentApiDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? Duration { get; set; }
    public string? TranscriptUrl { get; set; }
    public string? HostName { get; set; }
    public string? GuestName { get; set; }
    public int EpisodeNumber { get; set; }
    public string? SeriesName { get; set; }
    public List<string>? Tags { get; set; }
    public List<int>? EmotionCategories { get; set; }
    public List<int>? TopicCategories { get; set; }
    public int ContentStatus { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid CreatedBy { get; set; }
}