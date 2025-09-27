using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.Commons.Cache;
using SharedLibrary.Contracts.User.Events;
using Microsoft.EntityFrameworkCore;
using ContentService.Infrastructure.Context;
using ContentService.Domain.Entities;

namespace ContentService.Infrastructure.Consumers;

public class CreatorApplicationConsumer : 
    IConsumer<CreatorApplicationApprovedEvent>
{
    private readonly ILogger<CreatorApplicationConsumer> _logger;
    private readonly ContentDbContext _context;
    private readonly IUserStateCache _userStateCache;

    public CreatorApplicationConsumer(
        ILogger<CreatorApplicationConsumer> logger,
        ContentDbContext context,
        IUserStateCache userStateCache)
    {
        _logger = logger;
        _context = context;
        _userStateCache = userStateCache;
    }

    public async Task Consume(ConsumeContext<CreatorApplicationApprovedEvent> context)
    {
        var approvedEvent = context.Message;
        _logger.LogInformation(
            "ContentService received CreatorApplicationApprovedEvent for User: {UserId} with Role: {Role}",
            approvedEvent.UserId, approvedEvent.BusinessRoleName);

        try
        {
            // Cập nhật cache phân quyền người dùng
            var userState = await _userStateCache.GetUserStateAsync(approvedEvent.UserId);
            if (userState != null)
            {
                var userRoles = userState.Roles.ToList();
                if (!userRoles.Contains("ContentCreator"))
                {
                    userRoles.Add("ContentCreator");
                    var updatedState = userState with { Roles = userRoles };
                    await _userStateCache.SetUserStateAsync(updatedState);
                    _logger.LogInformation("Added ContentCreator role to user state cache for: {UserId}", approvedEvent.UserId);
                }
            }

            // Tạo Creator settings trong Content Service (tuỳ chọn)
            var creatorSettingsExists = await _context.CreatorSettings
                .AnyAsync(cs => cs.CreatorId == approvedEvent.UserId);

            if (!creatorSettingsExists)
            {
                // Giả sử có entity CreatorSettings để lưu trữ cài đặt của creator
                var creatorSettings = new CreatorSettings
                {
                    CreatorId = approvedEvent.UserId,
                    DisplayName = approvedEvent.UserEmail.Split('@')[0], // Default display name
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    MaxContentQuota = 50, // Default quota
                    AutoPublish = false // Require moderation by default
                };

                _context.CreatorSettings.Add(creatorSettings);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created new creator settings for user: {UserId}", approvedEvent.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreatorApplicationApprovedEvent for UserId: {UserId}", 
                approvedEvent.UserId);
            throw;
        }
    }
}
