using NSwag;

namespace JobTriggerPlatform.WebApi.OpenApi;

/// <summary>
/// Extensions for configuring OpenAPI.
/// </summary>
public static class OpenApiConfiguration
{
    /// <summary>
    /// Configures OpenAPI services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection ConfigureOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerDocument(config =>
        {
            config.Title = "JobTriggerPlatform API";
            config.Description = "API for triggering and managing deployment jobs";
            config.Version = "v1";

            // Add JWT authentication description
            config.AddSecurity("JWT", new NSwag.OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter JWT Bearer token **_only_**"
            });

            // Include XML documentation
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (System.IO.File.Exists(xmlPath))
            {
                config.SchemaSettings.UseXmlDocumentation = true;
            }
        });

        return services;
    }

    /// <summary>
    /// Configures OpenAPI middleware.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder ConfigureOpenApiMiddleware(this IApplicationBuilder app)
    {
        // Enable middleware to serve OpenAPI document and UI
        app.UseOpenApi();
        app.UseSwaggerUi();

        return app;
    }
}