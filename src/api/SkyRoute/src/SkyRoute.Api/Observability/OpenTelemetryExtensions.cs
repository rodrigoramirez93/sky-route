namespace SkyRoute.Api.Observability;

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using SkyRoute.BusinessLogic.Diagnostics;

/// <summary>
/// Centralizes every observability concern for the API: resource attributes,
/// Serilog → OTLP, OpenTelemetry traces/metrics/logs, and an enrichment hook
/// that renames generic ASP.NET span names ("POST api/flights/search") to the
/// business operation ("Flights.Search").
/// </summary>
public static class OpenTelemetryExtensions
{
    private const string ServiceVersionFallback = "1.0.0";

    private static readonly IReadOnlyDictionary<string, string> RouteToBusinessName =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["POST /api/flights/search"] = SkyRouteDiagnostics.SpanNames.FlightsSearch,
            ["POST /api/bookings"] = SkyRouteDiagnostics.SpanNames.BookingCreate,
            ["GET /api/airports"] = SkyRouteDiagnostics.SpanNames.AirportsList,
        };

    public static WebApplicationBuilder AddSkyRouteObservability(this WebApplicationBuilder builder)
    {
        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
            ?? builder.Configuration["OpenTelemetry:ServiceName"]
            ?? "SkyRoute.Api";
        var serviceVersion = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION")
            ?? builder.Configuration["OpenTelemetry:ServiceVersion"]
            ?? ServiceVersionFallback;
        var environmentName = builder.Environment.EnvironmentName;
        var instanceId = Environment.MachineName + ":" + Environment.ProcessId;

        var resource = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion, serviceInstanceId: instanceId)
            .AddAttributes(new KeyValuePair<string, object>[]
            {
                new("deployment.environment", environmentName),
            });

        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        // Serilog: console for humans + OTLP sink so structured logs ride the
        // same pipeline as Microsoft.Extensions.Logging (one log stream).
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("service.name", serviceName)
                .Enrich.WithProperty("service.version", serviceVersion)
                .Enrich.WithProperty("deployment.environment", environmentName)
                .WriteTo.Console();

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                configuration.WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = otlpEndpoint;
                    options.Protocol = OtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = serviceName,
                        ["service.version"] = serviceVersion,
                        ["service.instance.id"] = instanceId,
                        ["deployment.environment"] = environmentName,
                    };
                });
            }
        }, writeToProviders: true);

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;
            logging.SetResourceBuilder(resource);
            logging.AddOtlpExporter();
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(serviceName, serviceVersion: serviceVersion, serviceInstanceId: instanceId)
                .AddAttributes(new KeyValuePair<string, object>[]
                {
                    new("deployment.environment", environmentName),
                }))
            .WithTracing(tracing => tracing
                .AddSource(SkyRouteDiagnostics.SourceName)
                .AddAspNetCoreInstrumentation(o =>
                {
                    o.RecordException = true;
                    o.Filter = context =>
                        !context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
                        && !context.Request.Path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase);
                    o.EnrichWithHttpRequest = (activity, request) =>
                    {
                        var key = $"{request.Method} {request.Path}";
                        if (RouteToBusinessName.TryGetValue(key, out var businessName))
                        {
                            activity.DisplayName = businessName;
                        }
                    };
                })
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddMeter(SkyRouteDiagnostics.SourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());

        return builder;
    }

    public static WebApplication UseSkyRouteObservability(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        return app;
    }
}
