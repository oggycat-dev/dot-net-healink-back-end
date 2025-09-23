namespace SharedLibrary.Commons.Enums;

/// <summary>
/// Các vai trò nghiệp vụ trong hệ thống Healink
/// </summary>
public enum BusinessRoleEnum
{
    // User roles
    FreeUser = 1,
    PremiumUser = 2,
    
    // Content roles
    ContentCreator = 10,
    ContentEditor = 11,
    ExpertCollaborator = 12,
    
    // Community roles
    CommunityMember = 20,
    CommunityModerator = 21,
    
    // Admin roles
    SystemAdministrator = 30,
    UserManager = 31,
    EcommerceManager = 32,
    MarketingManager = 33,
    DataAnalyst = 34,
    BusinessOwner = 35
}
