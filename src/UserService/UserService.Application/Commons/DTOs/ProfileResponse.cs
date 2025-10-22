namespace UserService.Application.Commons.DTOs;

public class ProfileResponse
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? AvatarPath { get; set; }
    public DateTime? CreatedAt { get; set; }
}