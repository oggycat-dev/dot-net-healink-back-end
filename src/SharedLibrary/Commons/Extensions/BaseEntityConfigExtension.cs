using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Commons.Entities;
using SharedLibrary.Commons.Enums;

namespace SharedLibrary.Commons.Extensions;

/// <summary>
/// Static helper for shared entity configuration patterns
/// </summary>
public static class BaseEntityConfigExtension
{
    /// <summary>
    /// Configure common patterns for all BaseEntity-derived entities
    /// </summary>
    public static void ConfigureBaseEntities(ModelBuilder modelBuilder)
    {
        // Apply soft delete filter only to root entities inheriting from BaseEntity
        // In inheritance scenarios, filters should only be applied to the root type
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) && entityType.BaseType == null)
            {
                // Apply soft delete filter only to root entity types
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var falseConstant = Expression.Constant(false);
                var condition = Expression.Equal(property, falseConstant);
                var lambda = Expression.Lambda(condition, parameter);
                
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        // Configure common BaseEntity properties for all derived entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
        {
            ConfigureBaseEntityProperties(modelBuilder, entityType.ClrType);
        }
    }

    /// <summary>
    /// Configure standard BaseEntity properties
    /// </summary>
    private static void ConfigureBaseEntityProperties(ModelBuilder modelBuilder, Type entityType)
    {
        // Only configure primary key on root entity types, not derived types
        // This is crucial for Entity Framework inheritance scenarios
        var entityTypeInfo = modelBuilder.Model.FindEntityType(entityType);
        if (entityTypeInfo?.BaseType == null)
        {
            // This is a root entity type, configure the primary key
            modelBuilder.Entity(entityType).HasKey("Id");
        }
        
        // Default values for audit fields
        modelBuilder.Entity(entityType)
            .Property<DateTime?>("CreatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnType("timestamp with time zone");
            
        modelBuilder.Entity(entityType)
            .Property<DateTime?>("UpdatedAt")
            .HasColumnType("timestamp with time zone");
        
        modelBuilder.Entity(entityType)
            .Property<DateTime?>("DeletedAt")
            .HasColumnType("timestamp with time zone");
            
        modelBuilder.Entity(entityType)
            .Property<bool>("IsDeleted")
            .HasDefaultValue(false);

        // Enum conversions
        modelBuilder.Entity(entityType)
            .Property<EntityStatusEnum>("Status")
            .HasConversion<int>();
    }
}