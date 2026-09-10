using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Api.Middleware;

/// <summary>
/// Global translation of Domain/Application/technical exceptions into HTTP
/// responses (ProblemDetails). Contains no business logic -- it only maps
/// already-known exception types to a status code and a safe message. The
/// one deliberate exclusion is Login's "invalid credentials"
/// InvalidOperationException, which is NOT handled here: it carries a
/// different HTTP meaning (401) than every other InvalidOperationException
/// (409), and will be caught locally inside AuthController.Login in a
/// later stage, before it ever reaches this global handler.
///
/// Manager MVP addition: InvalidVehicleStatusTransitionException also maps
/// to 409, alongside the existing VehicleNotAvailableException.
///
/// Part D of the course spec (NLog): every conflict on the competed-over
/// resource logs at Warning, since it is an expected, handled business
/// outcome, not a bug; every other caught exception logs at Error. Nothing
/// about the response body or status code changes -- this is purely a
/// server-side logging addition.
/// </summary>
public class SkyShiftExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<SkyShiftExceptionHandler> _logger;

    public SkyShiftExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<SkyShiftExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = MapException(exception);

        if (statusCode == StatusCodes.Status409Conflict)
        {
            _logger.LogWarning(exception, "Resource conflict");
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception");
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            }
        });
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found", exception.Message),
            VehicleNotAvailableException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            InvalidVehicleStatusTransitionException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            _ => (StatusCodes.Status500InternalServerError,
                  "An unexpected error occurred",
                  "An unexpected error occurred. Please try again later.")
        };
    }
}
