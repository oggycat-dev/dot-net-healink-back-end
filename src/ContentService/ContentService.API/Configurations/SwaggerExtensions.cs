using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Builder;

namespace ContentService.API.Configurations;

/// <summary>
/// Swagger configuration extensions for ContentService with API groups
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Configure Swagger with API groups (User, Creator, CMS)
    /// </summary>
    public static IServiceCollection ConfigureSwaggerWithGroups(this IServiceCollection services, string serviceName)
    {
        services.AddSwaggerGen(options =>
        {
            // Define multiple Swagger documents for different API groups
            options.SwaggerDoc("User", new OpenApiInfo
            {
                Title = $"{serviceName} - User APIs",
                Version = "v1",
                Description = "APIs for regular users to browse and consume content (podcasts, community stories, etc.)"
            });

            options.SwaggerDoc("Creator", new OpenApiInfo
            {
                Title = $"{serviceName} - Creator APIs",
                Version = "v1",
                Description = "APIs for content creators to create and manage their content (podcasts, stories, uploads)"
            });

            options.SwaggerDoc("CMS", new OpenApiInfo
            {
                Title = $"{serviceName} - CMS APIs",
                Version = "v1",
                Description = "APIs for administrators and moderators to manage all content (approve, reject, analytics)"
            });

            // Group APIs by ApiExplorerSettings GroupName
            options.DocInclusionPredicate((docName, apiDesc) =>
            {
                var groupName = apiDesc.GroupName;
                
                // Include Health and utility endpoints in all groups
                if (apiDesc.RelativePath?.Contains("health") == true ||
                    apiDesc.RelativePath?.Contains("debug") == true)
                {
                    return true;
                }

                return groupName == docName;
            });

            // Note: JWT authentication is already configured in SharedLibrary
            // No need to add SecurityDefinition and SecurityRequirement here

            // Include XML comments
            try
            {
                var xmlFile = $"{Assembly.GetEntryAssembly()?.GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                }
            }
            catch
            {
                // XML comments are optional
            }

            // Customize operation IDs
            options.CustomOperationIds(apiDesc =>
            {
                if (apiDesc?.ActionDescriptor?.RouteValues == null) return null;
                
                apiDesc.ActionDescriptor.RouteValues.TryGetValue("controller", out var controllerName);
                apiDesc.ActionDescriptor.RouteValues.TryGetValue("action", out var actionName);
                
                if (!string.IsNullOrEmpty(controllerName) && !string.IsNullOrEmpty(actionName))
                {
                    return $"{controllerName}_{actionName}";
                }
                
                return null;
            });
        });

        return services;
    }

    /// <summary>
    /// Use Swagger UI with multiple API groups
    /// </summary>
    public static IApplicationBuilder UseSwaggerWithGroups(this IApplicationBuilder app, string serviceName)
    {
        app.UseSwagger(c =>
        {
            c.RouteTemplate = "swagger/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(options =>
        {
            // Add endpoints for each API group
            options.SwaggerEndpoint("/swagger/User/swagger.json", $"{serviceName} - User APIs");
            options.SwaggerEndpoint("/swagger/Creator/swagger.json", $"{serviceName} - Creator APIs");
            options.SwaggerEndpoint("/swagger/CMS/swagger.json", $"{serviceName} - CMS APIs");

            options.RoutePrefix = "swagger";
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.EnableDeepLinking();
            options.DisplayOperationId();
            options.EnableFilter();
            options.EnableTryItOutByDefault();

            // Custom title
            options.DocumentTitle = $"{serviceName} API Documentation";

            // Inject custom CSS if exists
            var customCssPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "swagger-custom", "custom-swagger-ui.css");
            if (File.Exists(customCssPath))
            {
                options.InjectStylesheet("/swagger-custom/custom-swagger-ui.css");
            }
        });

        return app;
    }
}
