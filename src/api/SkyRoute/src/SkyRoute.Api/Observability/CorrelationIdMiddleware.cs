namespace SkyRoute.Api.Observability;

using Microsoft.AspNetCore.Http;
using SkyRoute.BusinessLogic.Diagnostics;

/// <summary>
/// Reads or creates the <c>X-Correlation-Id</c> header for every request, stamps
/// it on the current <see cref="System.Diagnostics.Activity"/> as
/// <c>enduser.correlation_id</c>, pushes it into the logger scope so every log
/// line on the request carries it, and echoes it back to the caller so a
/// browser session can correlate its own logs/spans with the backend trace.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private const string ScopeKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var values)
            && !string.IsNullOrWhiteSpace(values.ToString())
                ? values.ToString()
                : Guid.NewGuid().ToString("N");

        context.Items[ScopeKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        var activity = System.Diagnostics.Activity.Current;
        activity?.SetTag(SkyRouteDiagnostics.Attributes.CorrelationId, correlationId);

        var sessionId = context.Request.Headers.TryGetValue("X-Session-Id", out var sessionValues)
            ? sessionValues.ToString()
            : null;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            activity?.SetTag(SkyRouteDiagnostics.Attributes.SessionId, sessionId);
        }

        var scopeState = new Dictionary<string, object?>
        {
            [ScopeKey] = correlationId,
            ["RequestPath"] = context.Request.Path.Value,
            ["RequestMethod"] = context.Request.Method,
        };
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            scopeState["SessionId"] = sessionId;
        }

        using (_logger.BeginScope(scopeState))
        {
            await _next(context).ConfigureAwait(false);
        }
    }
}
