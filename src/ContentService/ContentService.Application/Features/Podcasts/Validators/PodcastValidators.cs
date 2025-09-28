using FluentValidation;
using ContentService.Application.Features.Podcasts.Commands;
using Microsoft.AspNetCore.Http;

namespace ContentService.Application.Features.Podcasts.Validators;

public class CreatePodcastCommandValidator : AbstractValidator<CreatePodcastCommand>
{
    private readonly string[] _allowedAudioExtensions = { ".mp3", ".wav", ".m4a", ".aac", ".ogg" };
    private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxAudioFileSize = 100 * 1024 * 1024; // 100MB
    private const long MaxImageFileSize = 5 * 1024 * 1024; // 5MB

    public CreatePodcastCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .Length(1, 200).WithMessage("Title must be between 1 and 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .Length(10, 2000).WithMessage("Description must be between 10 and 2000 characters");

        RuleFor(x => x.AudioFile)
            .NotNull().WithMessage("Audio file is required")
            .Must(BeValidAudioFile).WithMessage($"Audio file must be one of: {string.Join(", ", _allowedAudioExtensions)}")
            .Must(BeWithinAudioSizeLimit).WithMessage($"Audio file size must not exceed {MaxAudioFileSize / (1024 * 1024)}MB");

        RuleFor(x => x.Duration)
            .GreaterThan(TimeSpan.Zero).WithMessage("Duration must be greater than zero")
            .LessThan(TimeSpan.FromHours(12)).WithMessage("Duration cannot exceed 12 hours");

        RuleFor(x => x.EpisodeNumber)
            .GreaterThan(0).WithMessage("Episode number must be positive");

        RuleFor(x => x.HostName)
            .Length(1, 100).When(x => !string.IsNullOrEmpty(x.HostName))
            .WithMessage("Host name must be between 1 and 100 characters");

        RuleFor(x => x.GuestName)
            .Length(1, 100).When(x => !string.IsNullOrEmpty(x.GuestName))
            .WithMessage("Guest name must be between 1 and 100 characters");

        RuleFor(x => x.SeriesName)
            .Length(1, 100).When(x => !string.IsNullOrEmpty(x.SeriesName))
            .WithMessage("Series name must be between 1 and 100 characters");

        RuleFor(x => x.ThumbnailFile)
            .Must(BeValidImageFile).When(x => x.ThumbnailFile != null)
            .WithMessage($"Thumbnail must be one of: {string.Join(", ", _allowedImageExtensions)}")
            .Must(BeWithinImageSizeLimit).When(x => x.ThumbnailFile != null)
            .WithMessage($"Thumbnail size must not exceed {MaxImageFileSize / (1024 * 1024)}MB");

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.Length <= 10)
            .WithMessage("Maximum 10 tags allowed")
            .Must(tags => tags == null || tags.All(tag => !string.IsNullOrWhiteSpace(tag) && tag.Length <= 50))
            .WithMessage("Each tag must be non-empty and not exceed 50 characters");

        RuleFor(x => x.EmotionCategories)
            .Must(emotions => emotions == null || emotions.Length <= 5)
            .WithMessage("Maximum 5 emotion categories allowed");

        RuleFor(x => x.TopicCategories)
            .Must(topics => topics == null || topics.Length <= 5)
            .WithMessage("Maximum 5 topic categories allowed");
    }

    private bool BeValidAudioFile(IFormFile? file)
    {
        if (file == null) return false;
        
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return _allowedAudioExtensions.Contains(extension);
    }

    private bool BeWithinAudioSizeLimit(IFormFile? file)
    {
        return file?.Length <= MaxAudioFileSize;
    }

    private bool BeValidImageFile(IFormFile? file)
    {
        if (file == null) return true; // Optional field
        
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return _allowedImageExtensions.Contains(extension);
    }

    private bool BeWithinImageSizeLimit(IFormFile? file)
    {
        return file?.Length <= MaxImageFileSize;
    }
}

public class UpdatePodcastCommandValidator : AbstractValidator<UpdatePodcastCommand>
{
    public UpdatePodcastCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Podcast ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .Length(1, 200).WithMessage("Title must be between 1 and 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .Length(10, 2000).WithMessage("Description must be between 10 and 2000 characters");

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.Length <= 10)
            .WithMessage("Maximum 10 tags allowed")
            .Must(tags => tags == null || tags.All(tag => !string.IsNullOrWhiteSpace(tag) && tag.Length <= 50))
            .WithMessage("Each tag must be non-empty and not exceed 50 characters");
    }
}

public class DeletePodcastCommandValidator : AbstractValidator<DeletePodcastCommand>
{
    public DeletePodcastCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Podcast ID is required");

        RuleFor(x => x.DeletedBy)
            .NotEmpty().WithMessage("DeletedBy user ID is required");
    }
}