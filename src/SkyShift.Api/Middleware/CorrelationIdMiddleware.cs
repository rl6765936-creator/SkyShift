using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SkyShift.Api.Middleware;

/// <summary>
/// Gives every request a CorrelationId, reusing the inbound X-Correlation-Id header when present,
/// and returns it in the response header. It runs first in the pipeline and opens a logging scope,
/// so every log line for the request, including errors logged by SkyShiftExceptionHandler, carries
/// the id. It also writes one Information line per request once the response status is known.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming)
            && !string.IsNullOrWhiteSpace(incoming)
            ? incoming.ToString()
            : Guid.NewGuid().ToString();

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode);
        }
    }
}
