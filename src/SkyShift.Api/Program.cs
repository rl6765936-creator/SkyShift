using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NLog.Web;
using SkyShift.Api.Authentication;
using SkyShift.Api.Middleware;
using SkyShift.Application.Mapping;
using SkyShift.Application.UseCases.ChangeVehicleStatus;
using SkyShift.Application.UseCases.CreateVehicle;
using SkyShift.Application.UseCases.GetActiveAssignments;
using SkyShift.Application.UseCases.GetAllFlights;
using SkyShift.Application.UseCases.GetAllUsersActivity;
using SkyShift.Application.UseCases.GetAllVehicles;
using SkyShift.Application.UseCases.GetAssignmentHistory;
using SkyShift.Application.UseCases.GetAvailableVehicles;
using SkyShift.Application.UseCases.GetFlightStats;
using SkyShift.Application.UseCases.GetManagerDashboard;
using SkyShift.Application.UseCases.GetUserDashboard;
using SkyShift.Application.UseCases.GetUserVehicle;
using SkyShift.Application.UseCases.LoginUser;
using SkyShift.Application.UseCases.RecordFlight;
using SkyShift.Application.UseCases.RegisterUser;
using SkyShift.Application.UseCases.SeedDemoUsers;
using SkyShift.Application.UseCases.SelectVehicle;
using SkyShift.Domain.Services;
using SkyShift.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// NLog is wired in as the provider underneath ILogger<T>; actual output
// targets (file/console) are configured in nlog.config.
builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

// Bearer auth definition for Swagger UI. Microsoft.OpenApi 2.x requires the
// Func<OpenApiDocument, ...> overload of AddSecurityRequirement with
// OpenApiSecuritySchemeReference; the older OpenApiReference
// object-initializer pattern does not compile against this package version.
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

// Fail fast at startup if Jwt:Issuer/Audience/SigningKey is missing or blank,
// instead of surfacing the problem later when a token is generated or
// validated. Bound from the "Jwt" configuration section and consumed via
// IOptions<JwtOptions> in JwtTokenService.
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<JwtTokenService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<SkyShiftExceptionHandler>();

// Connection string is read and validated here, in the API composition root,
// then handed to Infrastructure as a plain string -- Infrastructure itself
// never reads IConfiguration.
var connectionString = builder.Configuration.GetConnectionString("SkyShiftDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'ConnectionStrings:SkyShiftDb' is not configured.");
}

builder.Services.AddInfrastructure(connectionString);

builder.Services.AddAutoMapper(config =>
{
    var licenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(licenseKey))
        config.LicenseKey = licenseKey;
}, typeof(MappingProfile));

// Use Cases are registered as concrete classes (no interfaces/abstractions),
// Scoped to match the lifetime of the Repositories/IUnitOfWork they depend
// on (registered Scoped by AddInfrastructure above).
builder.Services.AddScoped<RegisterUserUseCase>();
builder.Services.AddScoped<LoginUserUseCase>();
builder.Services.AddScoped<GetAvailableVehiclesUseCase>();
builder.Services.AddScoped<SelectVehicleUseCase>();
builder.Services.AddScoped<GetUserVehicleUseCase>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddScoped<RecordFlightUseCase>();
builder.Services.AddScoped<GetUserDashboardUseCase>();

builder.Services.AddScoped<CreateVehicleUseCase>();
builder.Services.AddScoped<GetAllVehiclesUseCase>();
builder.Services.AddScoped<ChangeVehicleStatusUseCase>();
builder.Services.AddScoped<GetAllFlightsUseCase>();
builder.Services.AddScoped<GetFlightStatsUseCase>();
builder.Services.AddScoped<GetActiveAssignmentsUseCase>();
builder.Services.AddScoped<GetAssignmentHistoryUseCase>();
builder.Services.AddScoped<GetAllUsersActivityUseCase>();
builder.Services.AddScoped<GetManagerDashboardUseCase>();

// Registered like any other Use Case; invoked once at startup below. See
// SeedDemoUsersUseCase for the idempotency guarantee.
builder.Services.AddScoped<SeedDemoUsersUseCase>();

// Stateless with no dependencies, so Singleton is safe and avoids
// recreating it per request.
builder.Services.AddSingleton<LotteryEligibilityService>();

// Validation reads Issuer/Audience/SigningKey from the same "Jwt"
// configuration section already bound to JwtOptions and used by
// JwtTokenService for token generation, so generation and validation are
// guaranteed to use the same key.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSection = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtSection.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection.SigningKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Return a ProblemDetails body (matching SkyShiftExceptionHandler's
        // shape) instead of ASP.NET Core's default empty 401/403 response.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "A valid Bearer token is required to access this endpoint."
                });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = "You do not have permission to perform this action. This endpoint requires the Manager role."
                });
            }
        };
    });

// [Authorize(Roles = "Manager")] is evaluated against the ClaimTypes.Role
// claim issued by JwtTokenService, using ASP.NET Core's default
// RoleClaimType -- no custom policies needed.
builder.Services.AddAuthorization();

// Origins come from the existing Cors:AllowedOrigins configuration. No
// AllowAnyOrigin()/AllowCredentials() since authentication travels via the
// Authorization header, not cookies.
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SkyShiftFrontend", policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Runs first, ahead of everything else, so the logging scope it opens (and
// the response header it sets) covers the whole pipeline, exception
// handling included.
app.UseMiddleware<CorrelationIdMiddleware>();

// Must be registered early so it can catch exceptions from everything
// downstream.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS must run before Authentication/Authorization/MapControllers.
app.UseCors("SkyShiftFrontend");

// Authentication must run before Authorization, and both must run before
// MapControllers().
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed the two demo accounts once at startup. SeedDemoUsersUseCase is
// idempotent (looked up by email, created only if missing); no Vehicle,
// Flight, or VehicleAssignment is ever seeded here.
using (var seedScope = app.Services.CreateScope())
{
    var seeder = seedScope.ServiceProvider.GetRequiredService<SeedDemoUsersUseCase>();
    await seeder.ExecuteAsync();
}

// Guarantees at least one log event reaches the NLog file target on every
// startup, since framework noise (Microsoft.*/System.*) is filtered out in
// nlog.config.
app.Logger.LogInformation("SkyShift API started successfully.");

app.Run();
