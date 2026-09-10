using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SkyShift.Api.Middleware;

/// <summary>
/// Assigns a CorrelationId to every incoming request -- reusing the inbound
/// "X-Correlation-Id" request header when the caller supplies one, or
/// generating a new one otherwise -- echoes it back on the response header,
/// and pushes it into the logging scope so every NLog line written while
/// handling this request (including a caught exception logged by
/// SkyShiftExceptionHandler) can be tied back to a single request.
///
/// Registered first in the pipeline, in Program.cs, ahead of
/// app.UseExceptionHandler(): the logging scope this middleware opens wraps
/// the entire downstream call chain, so it is still active if an exception
/// later propagates back up into the global exception handler.
///
/// The scope alone does not write anything -- it only enriches log lines
/// that other code chooses to emit. On a normal (non-exception) request
/// nothing else in the pipeline logs at all, so this middleware also emits
/// the one Information line per request itself, after the response status
/// code is known, satisfying "log each incoming request with CorrelationId".
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
