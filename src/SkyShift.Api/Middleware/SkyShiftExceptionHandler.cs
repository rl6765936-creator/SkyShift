using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Api.Middleware;

/// <summary>
/// Translates known, specific exception types into ProblemDetails with a status code and safe
/// message; any other exception, including a bare InvalidOperationException or
/// KeyNotFoundException, becomes a generic 500. Expected errors include code, messageHe and
/// messageEn, and detail is Hebrew when Accept-Language is Hebrew. Validation and not-found errors
/// are logged at Information, conflicts and rejected logins at Warning, and unexpected failures at
/// Error.
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

        var code = exception switch
        {
            InvalidFlightDateException invalidDate => invalidDate.Code,
            VehicleDeletionNotAllowedException deletion => deletion.Code,
            VehicleNotFoundException => "vehicle_not_found",
            UserNotFoundException => "user_not_found",
            VehicleNotAvailableException => "vehicle_not_available",
            InvalidVehicleStatusTransitionException => "invalid_status_transition",
            ActiveAssignmentExistsException => "active_assignment_exists",
            NoActiveAssignmentException => "no_active_assignment",
            DuplicateFlightException => "duplicate_flight",
            InvalidVehicleCatalogException => "invalid_vehicle_catalog",
            CatalogItemNotFoundException => "catalog_item_not_found",
            CatalogItemInUseException => "catalog_item_in_use",
            EmailAlreadyRegisteredException => "duplicate_email",
            InvalidCredentialsException => "invalid_credentials",
            _ => null
        };
        var hebrewDetail = code switch
        {
            "flight_time_zone_required" => "יש לשלוח את מועד הטיסה עם אזור זמן UTC.",
            "flight_outside_trial" => "מועד הטיסה חייב להיות בתוך תקופת הניסיון, מתאריך ושעת תחילת ההקצאה ועד סיומה.",
            "flight_in_future" => "לא ניתן לדווח על טיסה שמועדה בעתיד.",
            "invalid_vehicle_catalog" => "פרטי הרכב אינם תקינים. יש לבדוק את שם הדגם, המהירות, מספר המושבים וקישור התמונה.",
            "duplicate_flight" => "כבר דווחה טיסה בתאריך ובשעה האלה בחשבון שלך.",
            "catalog_item_not_found" => "דגם הקטלוג שנבחר לא נמצא. יש לרענן את הרשימה ולבחור דגם קיים.",
            "catalog_item_in_use" => "הדגם הזה כבר נוסף לצי הרכבים. יש לבחור דגם אחר.",
            "duplicate_email" => "כתובת האימייל הזו כבר רשומה במערכת. אפשר להתחבר לחשבון הקיים או להשתמש בכתובת אחרת.",
            "invalid_credentials" => "כתובת האימייל או הסיסמה שגויות.",
            "vehicle_not_found" => "הרכב לא נמצא. יש לרענן ולנסות שוב.",
            "user_not_found" => "המשתמש לא נמצא.",
            "vehicle_not_available" => "הרכב הזה אינו פנוי כעת. יש לרענן את הרשימה ולבחור רכב אחר.",
            "invalid_status_transition" => "לא ניתן לבצע את שינוי המצב הזה מהמצב הנוכחי של הרכב. יש לרענן ולבחור מצב מותר.",
            "active_assignment_exists" => "כבר מוקצה לך רכב. אפשר לצפות בו במסך הרכב שלי.",
            "no_active_assignment" => "כדי לתעד טיסה יש לבחור קודם רכב פנוי.",
            VehicleDeletionNotAllowedException.NotAvailableCode => "ניתן למחוק רק רכב במצב זמין. יש לרענן ולבדוק את מצב הרכב.",
            VehicleDeletionNotAllowedException.HasHistoryCode => "לרכב יש היסטוריית הקצאות או טיסות ולכן לא ניתן למחוק אותו. אפשר להוציא אותו משימוש כדי לשמור על ההיסטוריה.",
            VehicleDeletionNotAllowedException.ChangedConcurrentlyCode => "הרכב שונה בבקשה אחרת. יש לרענן ולנסות שוב.",
            _ => detail
        };
        // User-facing English for the codes added with the specific exceptions. The
        // ProblemDetails "detail" keeps each exception's original English text, so
        // clients that match on it are unaffected; existing codes keep messageEn = detail.
        var englishDetail = code switch
        {
            "vehicle_not_found" => "The vehicle was not found. Refresh and try again.",
            "user_not_found" => "The user was not found.",
            "vehicle_not_available" => "This vehicle is no longer available. Refresh the list and choose another vehicle.",
            "invalid_status_transition" => "This status change is not allowed from the vehicle's current status. Refresh and choose an allowed status.",
            "active_assignment_exists" => "You already have an assigned vehicle. Open My vehicle to view it.",
            "no_active_assignment" => "Select an available vehicle before recording a flight.",
            VehicleDeletionNotAllowedException.NotAvailableCode => "Only an Available vehicle can be deleted. Refresh and check its status.",
            VehicleDeletionNotAllowedException.HasHistoryCode => "This vehicle has assignment or flight history, so it cannot be deleted. Retire it instead to keep the history.",
            VehicleDeletionNotAllowedException.ChangedConcurrentlyCode => "The vehicle was changed by another request. Refresh and try again.",
            _ => detail
        };

        Log(statusCode, code, exception);
        var problem = new ProblemDetails { Status = statusCode, Title = title, Detail = detail };
        if (code is not null)
        {
            problem.Extensions["code"] = code;
            problem.Extensions["messageHe"] = hebrewDetail;
            problem.Extensions["messageEn"] = englishDetail;
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

    // Expected outcomes are logged without a stack trace: validation and not-found
    // at Information, conflicts and rejected logins at Warning. Anything unmapped is
    // an unexpected failure: Error with the exception. Messages that may contain an
    // email address (duplicate registration, login) are not logged.
    private void Log(int statusCode, string? code, Exception exception)
    {
        var type = exception.GetType().Name;
        if (statusCode >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception");
        else if (statusCode == StatusCodes.Status401Unauthorized)
            _logger.LogWarning("Login rejected: invalid credentials");
        else if (statusCode == StatusCodes.Status409Conflict)
        {
            if (exception is EmailAlreadyRegisteredException)
                _logger.LogWarning("Resource conflict {Code} ({ExceptionType})", code, type);
            else
                _logger.LogWarning("Resource conflict {Code} ({ExceptionType}): {Message}", code, type, exception.Message);
        }
        else
            _logger.LogInformation("Request rejected with {StatusCode} {Code} ({ExceptionType}): {Message}", statusCode, code, type, exception.Message);
    }

    // Only specific, expected exception types are mapped. A bare
    // InvalidOperationException or KeyNotFoundException is a bug, not an expected
    // business outcome, so it stays a 500 instead of being disguised as 409/404.
    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            InvalidVehicleCatalogException => (StatusCodes.Status400BadRequest, "Invalid vehicle details", exception.Message),
            InvalidFlightDateException => (StatusCodes.Status400BadRequest, "Invalid flight date", exception.Message),
            DuplicateFlightException => (StatusCodes.Status409Conflict, "Duplicate flight", exception.Message),
            CatalogItemInUseException => (StatusCodes.Status409Conflict, "Catalog model already used", exception.Message),
            CatalogItemNotFoundException => (StatusCodes.Status404NotFound, "Catalog model not found", exception.Message),
            VehicleNotFoundException => (StatusCodes.Status404NotFound, "Not Found", exception.Message),
            UserNotFoundException => (StatusCodes.Status404NotFound, "Not Found", exception.Message),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message),
            EmailAlreadyRegisteredException => (StatusCodes.Status409Conflict, "Email already registered", exception.Message),
            VehicleNotAvailableException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            InvalidVehicleStatusTransitionException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            ActiveAssignmentExistsException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            NoActiveAssignmentException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            VehicleDeletionNotAllowedException => (StatusCodes.Status409Conflict, "Vehicle cannot be deleted", exception.Message),
            _ => (StatusCodes.Status500InternalServerError,
                  "An unexpected error occurred",
                  "An unexpected error occurred. Please try again later.")
        };
    }
}
