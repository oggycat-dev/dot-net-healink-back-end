using SharedLibrary.Commons.EventBus;
using System.Text.Json.Serialization;

namespace SharedLibrary.SharedLibrary.Contracts.Events;

/// <summary>
/// Supporting models for change tracking
/// </summary>
public record ChangeInfo
{
    [JsonPropertyName("old_value")]
    public object? OldValue { get; init; }

    [JsonPropertyName("new_value")]
    public object? NewValue { get; init; }

    [JsonPropertyName("field_name")]
    public string FieldName { get; init; } = string.Empty;

    [JsonPropertyName("change_type")]
    public string ChangeType { get; init; } = string.Empty; // "Updated", "Added", "Removed"
}

#region User Events

/// <summary>
/// Event when User is created
/// </summary>
public record UserCreatedEvent : IntegrationEvent
{
    [JsonPropertyName("User_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("User_name")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("category_id")]
    public Guid CategoryId { get; init; }

    [JsonPropertyName("category_name")]
    public string CategoryName { get; init; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; init; }

    [JsonPropertyName("discount_price")]
    public decimal? DiscountPrice { get; init; }

    [JsonPropertyName("stock_quantity")]
    public int StockQuantity { get; init; }

    [JsonPropertyName("created_by")]
    public Guid CreatedBy { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    public UserCreatedEvent() : base("UserService") { }
}

/// <summary>
/// Event when User is updated
/// </summary>
public record UserUpdatedEvent : IntegrationEvent
{
    [JsonPropertyName("User_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("User_name")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("updated_by")]
    public Guid UpdatedBy { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; }

    [JsonPropertyName("changes")]
    public Dictionary<string, ChangeInfo> Changes { get; init; } = new();

    public UserUpdatedEvent() : base("UserService") { }
}

/// <summary>
/// Event when User is deleted
/// </summary>
public record UserDeletedEvent : IntegrationEvent
{
    [JsonPropertyName("User_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("User_name")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("category_name")]
    public string CategoryName { get; init; } = string.Empty;

    [JsonPropertyName("deleted_by")]
    public Guid DeletedBy { get; init; }

    [JsonPropertyName("deleted_at")]
    public DateTime DeletedAt { get; init; }

    [JsonPropertyName("is_soft_delete")]
    public bool IsSoftDelete { get; init; }

    [JsonPropertyName("deletion_reason")]
    public string? DeletionReason { get; init; }

    public UserDeletedEvent() : base("UserService") { }
}

#endregion

#region Category Events

/// <summary>
/// Event when category is created
/// </summary>
public record CategoryCreatedEvent : IntegrationEvent
{
    [JsonPropertyName("category_id")]
    public Guid CategoryId { get; init; }

    [JsonPropertyName("category_name")]
    public string CategoryName { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("parent_category_id")]
    public Guid? ParentCategoryId { get; init; }

    [JsonPropertyName("parent_category_name")]
    public string? ParentCategoryName { get; init; }

    [JsonPropertyName("image_path")]
    public string? ImagePath { get; init; }

    [JsonPropertyName("created_by")]
    public Guid CreatedBy { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    public CategoryCreatedEvent() : base("UserService") { }
}

/// <summary>
/// Event when category is updated
/// </summary>
public record CategoryUpdatedEvent : IntegrationEvent
{
    [JsonPropertyName("category_id")]
    public Guid CategoryId { get; init; }

    [JsonPropertyName("category_name")]
    public string CategoryName { get; init; } = string.Empty;

    [JsonPropertyName("updated_by")]
    public Guid UpdatedBy { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; }

    [JsonPropertyName("changes")]
    public Dictionary<string, ChangeInfo> Changes { get; init; } = new();

    public CategoryUpdatedEvent() : base("UserService") { }
}

/// <summary>
/// Event when category is deleted
/// </summary>
public record CategoryDeletedEvent : IntegrationEvent
{
    [JsonPropertyName("category_id")]
    public Guid CategoryId { get; init; }

    [JsonPropertyName("category_name")]
    public string CategoryName { get; init; } = string.Empty;

    [JsonPropertyName("deleted_by")]
    public Guid DeletedBy { get; init; }

    [JsonPropertyName("deleted_at")]
    public DateTime DeletedAt { get; init; }

    [JsonPropertyName("affected_Users_count")]
    public int AffectedUsersCount { get; init; }

    [JsonPropertyName("sub_categories_count")]
    public int SubCategoriesCount { get; init; }

    public CategoryDeletedEvent() : base("UserService") { }
}

#endregion

#region User Inventory Events

/// <summary>
/// Event when User inventory is created (nhập hàng)
/// </summary>
public record UserInventoryCreatedEvent : IntegrationEvent
{
    [JsonPropertyName("inventory_id")]
    public Guid InventoryId { get; init; }

    [JsonPropertyName("User_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("User_name")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; init; }

    [JsonPropertyName("previous_total_stock")]
    public int PreviousTotalStock { get; init; }

    [JsonPropertyName("new_total_stock")]
    public int NewTotalStock { get; init; }

    [JsonPropertyName("created_by")]
    public Guid CreatedBy { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    public UserInventoryCreatedEvent() : base("UserService") { }
}

/// <summary>
/// Event when User inventory is updated
/// </summary>
public record UserInventoryUpdatedEvent : IntegrationEvent
{
    [JsonPropertyName("inventory_id")]
    public Guid InventoryId { get; init; }

    [JsonPropertyName("User_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("User_name")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("updated_by")]
    public Guid UpdatedBy { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; }

    [JsonPropertyName("changes")]
    public Dictionary<string, ChangeInfo> Changes { get; init; } = new();

    public UserInventoryUpdatedEvent() : base("UserService") { }
}

/// <summary>
/// Event when User inventory is deleted
/// </summary>
public record UserInventoryDeletedEvent : IntegrationEvent
{
    [JsonPropertyName("inventory_id")]
    public Guid InventoryId { get; init; }

    [JsonPropertyName("User_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("User_name")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; init; }

    [JsonPropertyName("deleted_by")]
    public Guid DeletedBy { get; init; }

    [JsonPropertyName("deleted_at")]
    public DateTime DeletedAt { get; init; }

    public UserInventoryDeletedEvent() : base("UserService") { }
}

#endregion

#region User Image Events

/// <summary>
/// Event when User image is created (uploaded)
/// </summary>
public record UserImageCreatedEvent : IntegrationEvent
{
    [JsonPropertyName("image_id")]
    public Guid ImageId { get; init; }

    [JsonPropertyName("User_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("User_name")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("image_path")]
    public string ImagePath { get; init; } = string.Empty;

    [JsonPropertyName("is_primary")]
    public bool IsPrimary { get; init; }

    [JsonPropertyName("display_order")]
    public int DisplayOrder { get; init; }

    [JsonPropertyName("created_by")]
    public Guid CreatedBy { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    public UserImageCreatedEvent() : base("UserService") { }
}

/// <summary>
/// Event when User image is updated
/// </summary>
public record UserImageUpdatedEvent : IntegrationEvent
{
    [JsonPropertyName("image_id")]
    public Guid ImageId { get; init; }

    [JsonPropertyName("User_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("User_name")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("updated_by")]
    public Guid UpdatedBy { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; }

    [JsonPropertyName("changes")]
    public Dictionary<string, ChangeInfo> Changes { get; init; } = new();

    public UserImageUpdatedEvent() : base("UserService") { }
}

/// <summary>
/// Event when User image is deleted
/// </summary>
public record UserImageDeletedEvent : IntegrationEvent
{
    [JsonPropertyName("image_id")]
    public Guid ImageId { get; init; }

    [JsonPropertyName("User_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("User_name")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("image_path")]
    public string ImagePath { get; init; } = string.Empty;

    [JsonPropertyName("was_primary")]
    public bool WasPrimary { get; init; }

    [JsonPropertyName("deleted_by")]
    public Guid DeletedBy { get; init; }

    [JsonPropertyName("deleted_at")]
    public DateTime DeletedAt { get; init; }

    public UserImageDeletedEvent() : base("UserService") { }
}

#endregion