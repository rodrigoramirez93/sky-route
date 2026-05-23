using Serilog;
using SkyRoute.Api.Composition;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.AddSkyRouteServices();

var app = builder.Build();

app.Logger.LogInformation("SkyRoute.Api starting up in {Environment}", app.Environment.EnvironmentName);

app.UseSkyRoutePipeline();
app.Run();

public partial class Program;
