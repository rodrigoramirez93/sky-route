using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using SkyRoute.Api.Composition;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "SkyRouteCors";
var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "SkyRoute.Api";

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console();
}, writeToProviders: true);

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
    logging.AddOtlpExporter();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddSource("SkyRoute.BusinessLogic")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSkyRoute();

var app = builder.Build();

app.Logger.LogInformation("SkyRoute.Api starting up. Service: {ServiceName}", serviceName);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CorsPolicyName);
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;