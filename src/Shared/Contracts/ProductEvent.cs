using ProductAuthMicroservice.Commons.EventBus;
using System.Text.Json.Serialization;

namespace ProductAuthMicroservice.Shared.Contracts.Events;

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

#region Product Events

/// <summary>
/// Event when product is created
/// </summary>
public record ProductCreatedEvent : IntegrationEvent
{
    [JsonPropertyName("product_id")]
    public Guid ProductId { get; init; }

    [JsonPropertyName("product_name")]
    public string ProductName { get; init; } = string.Empty;

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

    public ProductCreatedEvent() : base("ProductService") { }
}

/// <summary>
/// Event when product is updated
/// </summary>
public record ProductUpdatedEvent : IntegrationEvent
{
    [JsonPropertyName("product_id")]
    public Guid ProductId { get; init; }

    [JsonPropertyName("product_name")]
    public string ProductName { get; init; } = string.Empty;

    [JsonPropertyName("updated_by")]
    public Guid UpdatedBy { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; }

    [JsonPropertyName("changes")]
    public Dictionary<string, ChangeInfo> Changes { get; init; } = new();

    public ProductUpdatedEvent() : base("ProductService") { }
}

/// <summary>
/// Event when product is deleted
/// </summary>
public record ProductDeletedEvent : IntegrationEvent
{
    [JsonPropertyName("product_id")]
    public Guid ProductId { get; init; }

    [JsonPropertyName("product_name")]
    public string ProductName { get; init; } = string.Empty;

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

    public ProductDeletedEvent() : base("ProductService") { }
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

    public CategoryCreatedEvent() : base("ProductService") { }
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

    public CategoryUpdatedEvent() : base("ProductService") { }
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

    [JsonPropertyName("affected_products_count")]
    public int AffectedProductsCount { get; init; }

    [JsonPropertyName("sub_categories_count")]
    public int SubCategoriesCount { get; init; }

    public CategoryDeletedEvent() : base("ProductService") { }
}

#endregion

#region Product Inventory Events

/// <summary>
/// Event when product inventory is created (nhập hàng)
/// </summary>
public record ProductInventoryCreatedEvent : IntegrationEvent
{
    [JsonPropertyName("inventory_id")]
    public Guid InventoryId { get; init; }

    [JsonPropertyName("product_id")]
    public Guid ProductId { get; init; }

    [JsonPropertyName("product_name")]
    public string ProductName { get; init; } = string.Empty;

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

    public ProductInventoryCreatedEvent() : base("ProductService") { }
}

/// <summary>
/// Event when product inventory is updated
/// </summary>
public record ProductInventoryUpdatedEvent : IntegrationEvent
{
    [JsonPropertyName("inventory_id")]
    public Guid InventoryId { get; init; }

    [JsonPropertyName("product_id")]
    public Guid ProductId { get; init; }

    [JsonPropertyName("product_name")]
    public string ProductName { get; init; } = string.Empty;

    [JsonPropertyName("updated_by")]
    public Guid UpdatedBy { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; }

    [JsonPropertyName("changes")]
    public Dictionary<string, ChangeInfo> Changes { get; init; } = new();

    public ProductInventoryUpdatedEvent() : base("ProductService") { }
}

/// <summary>
/// Event when product inventory is deleted
/// </summary>
public record ProductInventoryDeletedEvent : IntegrationEvent
{
    [JsonPropertyName("inventory_id")]
    public Guid InventoryId { get; init; }

    [JsonPropertyName("product_id")]
    public Guid ProductId { get; init; }

    [JsonPropertyName("product_name")]
    public string ProductName { get; init; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; init; }

    [JsonPropertyName("deleted_by")]
    public Guid DeletedBy { get; init; }

    [JsonPropertyName("deleted_at")]
    public DateTime DeletedAt { get; init; }

    public ProductInventoryDeletedEvent() : base("ProductService") { }
}

#endregion

#region Product Image Events

/// <summary>
/// Event when product image is created (uploaded)
/// </summary>
public record ProductImageCreatedEvent : IntegrationEvent
{
    [JsonPropertyName("image_id")]
    public Guid ImageId { get; init; }

    [JsonPropertyName("product_id")]
    public Guid ProductId { get; init; }

    [JsonPropertyName("product_name")]
    public string ProductName { get; init; } = string.Empty;

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

    public ProductImageCreatedEvent() : base("ProductService") { }
}

/// <summary>
/// Event when product image is updated
/// </summary>
public record ProductImageUpdatedEvent : IntegrationEvent
{
    [JsonPropertyName("image_id")]
    public Guid ImageId { get; init; }

    [JsonPropertyName("product_id")]
    public Guid ProductId { get; init; }

    [JsonPropertyName("product_name")]
    public string ProductName { get; init; } = string.Empty;

    [JsonPropertyName("updated_by")]
    public Guid UpdatedBy { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; }

    [JsonPropertyName("changes")]
    public Dictionary<string, ChangeInfo> Changes { get; init; } = new();

    public ProductImageUpdatedEvent() : base("ProductService") { }
}

/// <summary>
/// Event when product image is deleted
/// </summary>
public record ProductImageDeletedEvent : IntegrationEvent
{
    [JsonPropertyName("image_id")]
    public Guid ImageId { get; init; }

    [JsonPropertyName("product_id")]
    public Guid ProductId { get; init; }

    [JsonPropertyName("product_name")]
    public string ProductName { get; init; } = string.Empty;

    [JsonPropertyName("image_path")]
    public string ImagePath { get; init; } = string.Empty;

    [JsonPropertyName("was_primary")]
    public bool WasPrimary { get; init; }

    [JsonPropertyName("deleted_by")]
    public Guid DeletedBy { get; init; }

    [JsonPropertyName("deleted_at")]
    public DateTime DeletedAt { get; init; }

    public ProductImageDeletedEvent() : base("ProductService") { }
}

#endregion