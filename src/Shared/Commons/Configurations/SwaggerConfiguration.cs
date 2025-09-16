using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ProductAuthMicroservice.Commons.Configurations;

/// <summary>
/// Configuration for Swagger UI across microservices
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// Configure Swagger generation options
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services, 
        string serviceName, string version = "v1", string? description = null)
    {
        services.AddSwaggerGen(options =>
        {
            // Configure basic information
            options.SwaggerDoc(version, new OpenApiInfo
            {
                Title = $"{serviceName} API",
                Version = version,
                Description = description ?? $"API for {serviceName} microservice"
            });
            
            // Configure API grouping by controller
            options.TagActionsBy(api =>
            {
                // Use Tags from attribute if available
                var controllerTags = api.ActionDescriptor.EndpointMetadata
                    .OfType<TagsAttribute>()
                    .SelectMany(attr => attr.Tags)
                    .Distinct();
                    
                if (controllerTags.Any())
                {
                    return controllerTags.ToList();
                }
                
                // Get controller name
                var controllerName = api.ActionDescriptor.RouteValues["controller"];
                
                // Determine main group based on path
                var relativePath = api.RelativePath?.ToLower();
                string mainTag;
                
                if (relativePath?.Contains("/admin/") == true || relativePath?.Contains("/cms/") == true)
                {
                    mainTag = "Admin";
                }
                else if (relativePath?.Contains("/customer/") == true || relativePath?.Contains("/public/") == true)
                {
                    mainTag = "Public";
                }
                else if (relativePath?.Contains("/api/") == true)
                {
                    mainTag = "API";
                }
                else
                {
                    return new[] { controllerName ?? "Default" };
                }
                
                // Create combined tag
                var combinedTag = $"{mainTag}_{controllerName}";
                return new[] { mainTag, combinedTag };
            });
            
            // Sort by tag
            options.OrderActionsBy(apiDesc => $"{apiDesc.GroupName}");
            
            // Configure JWT authentication in Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            
            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
            
            // Customize operation IDs to include controller name
            options.CustomOperationIds(apiDesc =>
            {
                if (apiDesc.TryGetMethodInfo(out MethodInfo methodInfo))
                {
                    var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
                    return $"{controllerName}_{methodInfo.Name}";
                }
                return null;
            });
            
            // Add service name to operation descriptions
            options.DocumentFilter<ServiceNameDocumentFilter>(serviceName);
        });
        
        return services;
    }
    
    /// <summary>
    /// Configure Swagger middleware
    /// </summary>
    public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app, 
        string serviceName, string version = "v1")
    {
        var environment = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
        
        if (environment.IsDevelopment())
        {
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "swagger/{documentName}/swagger.json";
            });
            
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"{serviceName} API {version}");
                options.RoutePrefix = "swagger";
                options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                options.EnableDeepLinking();
                options.DisplayOperationId();
                options.EnableFilter();
                options.EnableTryItOutByDefault();
                
                // Custom title
                options.DocumentTitle = $"{serviceName} API Documentation";
            });
        }
        
        return app;
    }
}

/// <summary>
/// Attribute to specify tags for API controllers/actions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class TagsAttribute : Attribute
{
    /// <summary>
    /// Gets the tags used by the action or controller
    /// </summary>
    public string[] Tags { get; }

    /// <summary>
    /// Creates a new TagsAttribute with the specified tags
    /// </summary>
    /// <param name="tags">The tags to apply to the action or controller</param>
    public TagsAttribute(params string[] tags)
    {
        Tags = tags;
    }
}

/// <summary>
/// Document filter to add service name to Swagger documentation
/// </summary>
public class ServiceNameDocumentFilter : IDocumentFilter
{
    private readonly string _serviceName;

    public ServiceNameDocumentFilter(string serviceName)
    {
        _serviceName = serviceName;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Info.Title = $"{_serviceName} API";
        
        // Add service name tag to all operations
        foreach (var pathItem in swaggerDoc.Paths.Values)
        {
            foreach (var operation in pathItem.Operations.Values)
            {
                if (!operation.Tags.Any(t => t.Name.Contains(_serviceName)))
                {
                    operation.Tags.Add(new OpenApiTag { Name = _serviceName });
                }
            }
        }
    }
}
