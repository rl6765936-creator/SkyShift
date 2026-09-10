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

// Part D of the course spec: NLog.Web.AspNetCore, wired in as the provider
// underneath Microsoft.Extensions.Logging.ILogger<T> -- every existing
// _logger.LogXxx(...) call anywhere in the solution is unchanged; only what
// actually writes the log events out changes, per nlog.config.
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Stage 1 — API Foundation. JWT Bearer Authentication, Authorization, CORS,
// Exception Handling, Use Case registrations, and Controllers are
// deliberately NOT added here; they belong to later, explicitly approved
// implementation stages.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

// Stage 3.6 — Swagger Bearer support. Uses the current Swashbuckle.AspNetCore
// 10.2.3 API (Microsoft.OpenApi 2.x): AddSecurityRequirement now takes a
// Func<OpenApiDocument, OpenApiSecurityRequirement> delegate and references
// the scheme via OpenApiSecuritySchemeReference -- the older object-initializer
// pattern with OpenApiReference does not compile on this package version.
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

// Stage 2 — JWT token-generation foundation only. No AddAuthentication(),
// no AddJwtBearer(), no AddAuthorization() yet -- those belong to a later,
// explicitly approved stage.
//
// Teacher requirement: Configuration. AddOptions<JwtOptions>() (the modern
// equivalent of the plain Configure<JwtOptions>() this replaces) plus
// ValidateDataAnnotations().ValidateOnStart() makes a missing/blank
// Jwt:SigningKey/Issuer/Audience fail fast at startup with a clear message
// -- the same fail-fast treatment ConnectionStrings:SkyShiftDb already
// gets below -- instead of only surfacing later, cryptically, the first
// time a token is generated or validated. Still bound from the same "Jwt"
// configuration section, still consumed the same way via IOptions<JwtOptions>
// in JwtTokenService.
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<JwtTokenService>();

// Stage 3.1 — Exception Handler.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<SkyShiftExceptionHandler>();

// Stage 3.1 — Infrastructure composition (connection string is read and
// validated here, in the API composition root, then handed to Infrastructure
// as a plain string -- Infrastructure itself never reads IConfiguration).
var connectionString = builder.Configuration.GetConnectionString("SkyShiftDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'ConnectionStrings:SkyShiftDb' is not configured.");
}

builder.Services.AddInfrastructure(connectionString);

// Teacher requirement: AutoMapper, registered from the single Application-
// layer profile (SkyShift.Application.Mapping.MappingProfile) that replaces
// the previously-manual Entity -> DTO copies in the Use Cases below.
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Stage 3.1 — Application Use Case registrations. Exactly the 7 approved
// Use Cases, registered as concrete classes (no interfaces/abstractions),
// Scoped to match the lifetime of the Repositories/IUnitOfWork they depend
// on (registered Scoped by AddInfrastructure above).
builder.Services.AddScoped<RegisterUserUseCase>();
builder.Services.AddScoped<LoginUserUseCase>();
builder.Services.AddScoped<GetAvailableVehiclesUseCase>();
builder.Services.AddScoped<SelectVehicleUseCase>();
builder.Services.AddScoped<GetUserVehicleUseCase>();
builder.Services.AddScoped<RecordFlightUseCase>();
builder.Services.AddScoped<GetUserDashboardUseCase>();

// Manager MVP — the 9 approved Manager Use Cases, registered the same way
// (Scoped, concrete classes) as the 7 existing User Use Cases above.
builder.Services.AddScoped<CreateVehicleUseCase>();
builder.Services.AddScoped<GetAllVehiclesUseCase>();
builder.Services.AddScoped<ChangeVehicleStatusUseCase>();
builder.Services.AddScoped<GetAllFlightsUseCase>();
builder.Services.AddScoped<GetFlightStatsUseCase>();
builder.Services.AddScoped<GetActiveAssignmentsUseCase>();
builder.Services.AddScoped<GetAssignmentHistoryUseCase>();
builder.Services.AddScoped<GetAllUsersActivityUseCase>();
builder.Services.AddScoped<GetManagerDashboardUseCase>();

// Deterministic demo-user seeding (see SeedDemoUsersUseCase) -- registered
// the same way as every other Use Case; invoked once at startup below,
// after the app is built.
builder.Services.AddScoped<SeedDemoUsersUseCase>();

// Stage 3.1 — Domain Service registration. Stateless, no dependencies, so
// Singleton is safe and avoids recreating it per request.
builder.Services.AddSingleton<LotteryEligibilityService>();

// Stage 3.2 — JWT Authentication. Validation reads Issuer/Audience/SigningKey
// directly from the same "Jwt" configuration section already bound to
// JwtOptions and already used by JwtTokenService for token generation --
// there is exactly one SigningKey value, so generation and validation are
// guaranteed to use the same key. No second ServiceProvider is built here.
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

        // Teacher requirement: Swagger/API callers must see a clear,
        // human-readable message for auth failures, not an empty 401/403
        // body. These two short-circuit ASP.NET Core's default empty
        // response with the same ProblemDetails shape SkyShiftExceptionHandler
        // already uses for every other error -- no other response (success
        // or otherwise) is touched.
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

// Stage 3.2 — Authorization, extended for Manager MVP. Still no custom
// policies: [Authorize(Roles = "Manager")] on the 5 new Manager
// controllers is evaluated directly against the ClaimTypes.Role claim
// now issued by JwtTokenService, using ASP.NET Core's default
// RoleClaimType -- no extra AddAuthorization() configuration needed.
builder.Services.AddAuthorization();

// Stage 3.6 — CORS. Named "SkyShiftFrontend" policy, API layer only, origins
// read from the existing Cors:AllowedOrigins configuration (currently empty --
// left untouched in appsettings, per explicit instruction for this stage).
// No AllowAnyOrigin(); no AllowCredentials() since authentication travels via
// the Authorization header, not cookies.
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

// CorrelationId middleware runs first, ahead of everything else, so the
// logging scope it opens (and the response header it sets) covers the
// whole pipeline, exception handling included.
app.UseMiddleware<CorrelationIdMiddleware>();

// Stage 3.1 — must be the first middleware so it can catch everything
// downstream.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Stage 3.6 — CORS must run before Authentication/Authorization/MapControllers.
app.UseCors("SkyShiftFrontend");

// Stage 3.2 — Authentication must run before Authorization, and both must
// run before MapControllers().
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Deterministic demo-user seeding, run once at startup so the two demo
// accounts (one User, one Manager) exist immediately after the database is
// created -- see SeedDemoUsersUseCase for the idempotency guarantee (looked
// up by email, created only if missing) and for why no Vehicle/Flight/
// VehicleAssignment is ever seeded here.
using (var seedScope = app.Services.CreateScope())
{
    var seeder = seedScope.ServiceProvider.GetRequiredService<SeedDemoUsersUseCase>();
    await seeder.ExecuteAsync();
}

// Single informational log line, guaranteed to fire on every startup, so the
// NLog pipeline (file + console targets) has at least one real event to
// write. Framework noise (Microsoft.*/System.*) is intentionally filtered
// out in nlog.config, so without this line no log event ever reached the
// file target and the logs folder was never created.
app.Logger.LogInformation("SkyShift API started successfully.");

app.Run();
