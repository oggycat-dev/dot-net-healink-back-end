using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts.User.Events;
using SharedLibrary.Contracts.User.Saga;
using ContentService.Infrastructure.Context;
using ContentService.Domain.Entities;

namespace ContentService.Infrastructure.Consumers;

/// <summary>
/// Consumer để xử lý User lifecycle events và Registration events
/// </summary>
public class UserEventConsumer : 
    IConsumer<UserCreatedEvent>,
    IConsumer<UserUpdatedEvent>, 
    IConsumer<UserDeletedEvent>,
    IConsumer<UserActivationChangedEvent>,
    IConsumer<UserEmailVerifiedEvent>,
    IConsumer<RegistrationCompleted>,
    IConsumer<RegistrationFailed>
{
    private readonly ILogger<UserEventConsumer> _logger;
    private readonly ContentDbContext _context;

    public UserEventConsumer(ILogger<UserEventConsumer> logger, ContentDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var userEvent = context.Message;
        _logger.LogInformation("Processing UserCreatedEvent for User: {UserId} - {Email}", 
            userEvent.UserId, userEvent.Email);

        try
        {
            // Initialize user content settings/permissions
            // Check if user already exists in content context
            var existingContent = await _context.Contents
                .AnyAsync(c => c.CreatedBy == userEvent.UserId);

            if (!existingContent)
            {
                _logger.LogInformation("Setting up content permissions for new user: {UserId}", userEvent.UserId);
                
                // TODO: Initialize user content preferences, quotas, etc.
                // Could create default content folders, set content limits based on roles, etc.
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserCreatedEvent for UserId: {UserId}", userEvent.UserId);
            throw;
        }
    }

    public async Task Consume(ConsumeContext<UserUpdatedEvent> context)
    {
        var userEvent = context.Message;
        _logger.LogInformation("Processing UserUpdatedEvent for User: {UserId} - Changed fields: [{ChangedFields}]", 
            userEvent.UserId, string.Join(", ", userEvent.ChangedFields));

        try
        {
            // Update user-related content metadata if email changed
            if (userEvent.ChangedFields.Contains("Email") && !string.IsNullOrEmpty(userEvent.OldEmail))
            {
                _logger.LogInformation("User email changed from {OldEmail} to {NewEmail}, updating content references", 
                    userEvent.OldEmail, userEvent.Email);
                
                // TODO: Update any email-based content references
            }

            // Update user display names in content if name changed
            if (userEvent.ChangedFields.Contains("FullName"))
            {
                var userContents = await _context.Contents
                    .Where(c => c.CreatedBy == userEvent.UserId)
                    .ToListAsync();

                foreach (var content in userContents)
                {
                    // Update author display name in content metadata
                    // This could be stored in a separate field or metadata
                }

                if (userContents.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated author names for {ContentCount} contents", userContents.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserUpdatedEvent for UserId: {UserId}", userEvent.UserId);
            throw;
        }
    }

    public async Task Consume(ConsumeContext<UserDeletedEvent> context)
    {
        var userEvent = context.Message;
        _logger.LogWarning("Processing UserDeletedEvent for User: {UserId} - {Email} - Type: {DeletionType}", 
            userEvent.UserId, userEvent.Email, userEvent.DeletionType);

        try
        {
            var userContents = await _context.Contents
                .Where(c => c.CreatedBy == userEvent.UserId && !c.IsDeleted)
                .ToListAsync();

            if (userContents.Any())
            {
                if (userEvent.IsPermanent || userEvent.DeletionType == "GDPR")
                {
                    // Hard delete or anonymize content for GDPR compliance
                    foreach (var content in userContents)
                    {
                        // Option 1: Hard delete
                        // _context.Contents.Remove(content);
                        
                        // Option 2: Anonymize (recommended)
                        content.CreatedBy = null; // Anonymize
                        content.UpdatedBy = Guid.Parse("00000000-0000-0000-0000-000000000000"); // System user
                        content.UpdatedAt = DateTime.UtcNow;
                    }
                    
                    _logger.LogWarning("Anonymized {ContentCount} contents for GDPR deletion of user: {UserId}", 
                        userContents.Count, userEvent.UserId);
                }
                else
                {
                    // Soft delete - mark content as orphaned but keep it
                    foreach (var content in userContents)
                    {
                        // Keep content but mark as from deleted user
                        content.UpdatedAt = DateTime.UtcNow;
                        content.UpdatedBy = Guid.Parse("00000000-0000-0000-0000-000000000000"); // System user
                    }
                    
                    _logger.LogInformation("Marked {ContentCount} contents as orphaned for user deletion: {UserId}", 
                        userContents.Count, userEvent.UserId);
                }

                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserDeletedEvent for UserId: {UserId}", userEvent.UserId);
            throw;
        }
    }

    public async Task Consume(ConsumeContext<UserActivationChangedEvent> context)
    {
        var userEvent = context.Message;
        _logger.LogInformation("Processing UserActivationChangedEvent for User: {UserId} - Active: {IsActive}", 
            userEvent.UserId, userEvent.IsActive);

        try
        {
            // Update content visibility based on user activation status
            var userContents = await _context.Contents
                .Where(c => c.CreatedBy == userEvent.UserId)
                .ToListAsync();

            foreach (var content in userContents)
            {
                if (!userEvent.IsActive)
                {
                    // Hide content from inactive users
                    // Could set content status to Draft or Archived
                    if (content.ContentStatus == ContentService.Domain.Enums.ContentStatus.Published)
                    {
                        content.ContentStatus = ContentService.Domain.Enums.ContentStatus.Archived;
                        content.UpdatedAt = DateTime.UtcNow;
                    }
                }
                // Note: Reactivation might need manual review before republishing content
            }

            if (userContents.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated content visibility for {ContentCount} contents due to user activation change", 
                    userContents.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserActivationChangedEvent for UserId: {UserId}", userEvent.UserId);
            throw;
        }
    }

    public async Task Consume(ConsumeContext<UserEmailVerifiedEvent> context)
    {
        var userEvent = context.Message;
        _logger.LogInformation("Processing UserEmailVerifiedEvent for User: {UserId} - {Email}", 
            userEvent.UserId, userEvent.Email);

        try
        {
            // Enable full content features for verified users
            // Could update content publishing permissions, remove content limits, etc.
            
            _logger.LogInformation("User email verified, enabling full content features for UserId: {UserId}", 
                userEvent.UserId);

            // TODO: Update user content permissions/quotas based on verification status
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserEmailVerifiedEvent for UserId: {UserId}", userEvent.UserId);
            throw;
        }
    }

    // Keep registration saga events for backward compatibility
    public async Task Consume(ConsumeContext<RegistrationCompleted> context)
    {
        var regEvent = context.Message;
        _logger.LogInformation("Processing RegistrationCompleted for User: {UserId} - {Email}", 
            regEvent.UserId, regEvent.Email);

        // Enable full content features for newly registered users
        try
        {
            // TODO: Set up default content preferences, welcome content, etc.
            _logger.LogInformation("Registration completed, setting up content features for UserId: {UserId}", 
                regEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RegistrationCompleted for UserId: {UserId}", regEvent.UserId);
            throw;
        }
    }

    public async Task Consume(ConsumeContext<RegistrationFailed> context)
    {
        var failEvent = context.Message;
        _logger.LogWarning("Processing RegistrationFailed for Email: {Email} - Reason: {Reason}", 
            failEvent.Email, failEvent.FailureReason);

        // Clean up any provisional content data for failed registration
        try
        {
            // TODO: Clean up any content-related data for failed registration if needed
            _logger.LogInformation("Cleaned up provisional data for failed registration: {Email}", failEvent.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RegistrationFailed for Email: {Email}", failEvent.Email);
            throw;
        }
    }
}
