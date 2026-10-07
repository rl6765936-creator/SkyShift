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

        var code = exception switch
        {
            InvalidFlightDateException invalidDate => invalidDate.Code,
            DuplicateFlightException => "duplicate_flight",
            InvalidVehicleCatalogException => "invalid_vehicle_catalog",
            _ => null
        };
        var hebrewDetail = code switch
        {
            "flight_time_zone_required" => "יש לשלוח את מועד הטיסה עם אזור זמן UTC.",
            "flight_outside_trial" => "מועד הטיסה חייב להיות בתוך תקופת הניסיון, מתאריך ושעת תחילת ההקצאה ועד סיומה.",
            "flight_in_future" => "לא ניתן לדווח על טיסה שמועדה בעתיד.",
            "invalid_vehicle_catalog" => "פרטי הרכב אינם תקינים. יש לבדוק את שם הדגם, המהירות, מספר המושבים וקישור התמונה.",
            "duplicate_flight" => "כבר דווחה טיסה בתאריך ובשעה האלה בחשבון שלך.",
            _ => detail
        };
        var problem = new ProblemDetails { Status = statusCode, Title = title, Detail = detail };
        if (code is not null)
        {
            problem.Extensions["code"] = code;
            problem.Extensions["messageHe"] = hebrewDetail;
            problem.Extensions["messageEn"] = detail;
            if (httpContext.Request.Headers.AcceptLanguage.ToString().StartsWith("he", StringComparison.OrdinalIgnoreCase))
                problem.Detail = hebrewDetail;
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            InvalidVehicleCatalogException => (StatusCodes.Status400BadRequest, "Invalid vehicle details", exception.Message),
            InvalidFlightDateException => (StatusCodes.Status400BadRequest, "Invalid flight date", exception.Message),
            DuplicateFlightException => (StatusCodes.Status409Conflict, "Duplicate flight", exception.Message),
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
