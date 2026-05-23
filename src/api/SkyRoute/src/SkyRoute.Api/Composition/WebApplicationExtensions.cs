namespace SkyRoute.Api.Composition;

using SkyRoute.Api.Observability;

/// <summary>
/// Groups all SkyRoute.Api wiring (services + middleware pipeline) into
/// composable extension methods so <c>Program.cs</c> stays declarative.
/// </summary>
public static class WebApplicationExtensions
{
    public const string CorsPolicyName = "SkyRouteCors";

    /// <summary>
    /// Registers every service the API needs: observability, CORS, MVC,
    /// OpenAPI and the SkyRoute domain composition root.
    /// </summary>
    public static WebApplicationBuilder AddSkyRouteServices(this WebApplicationBuilder builder)
    {
        builder.AddSkyRouteObservability();
        builder.Services.AddSkyRouteCors();
        builder.Services.AddSkyRouteApi();
        builder.Services.AddSkyRoute();

        // missing: authentication / authorization (JWT bearer, OIDC, API keys...)
        // missing: rate limiting (AddRateLimiter with fixed/sliding window per IP or per user)
        // missing: response caching / output cache + distributed cache (Redis) setup
        // missing: custom metrics registration (Meter + AddMeter in OpenTelemetry)
        // missing: health checks (AddHealthChecks + downstream provider probes)
        // missing: problem details / global exception handling middleware
        // missing: API versioning (Asp.Versioning.Mvc)
        // missing: data protection keys persisted to shared store for multi-instance deployments
        // missing: feature flags (Microsoft.FeatureManagement)
        // missing: options validation (ValidateDataAnnotations + ValidateOnStart)

        return builder;
    }

    /// <summary>
    /// Wires the HTTP request pipeline in the correct order.
    /// </summary>
    public static WebApplication UseSkyRoutePipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseCors(CorsPolicyName);
        app.UseSkyRouteObservability();

        // missing: app.UseExceptionHandler() + ProblemDetails
        // missing: app.UseRateLimiter()
        // missing: app.UseAuthentication()
        // missing: app.UseOutputCache()
        // missing: app.MapHealthChecks("/health") and /health/ready
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    private static IServiceCollection AddSkyRouteCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
                policy.WithOrigins("http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .WithExposedHeaders(CorrelationIdMiddleware.HeaderName));
        });

        // missing: read allowed origins from configuration instead of hardcoding localhost:4200
        return services;
    }

    private static IServiceCollection AddSkyRouteApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();
        return services;
    }
}
