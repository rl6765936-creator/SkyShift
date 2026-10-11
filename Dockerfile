# SkyShift API image for Render (or any Docker host). .NET 8, matching every project's net8.0.
# No secrets here: the database provider, connection string and JWT settings come from environment variables.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Project files first, so the restore layer is cached until a dependency changes.
COPY src/SkyShift.Domain/SkyShift.Domain.csproj src/SkyShift.Domain/
COPY src/SkyShift.Application/SkyShift.Application.csproj src/SkyShift.Application/
COPY src/SkyShift.Infrastructure/SkyShift.Infrastructure.csproj src/SkyShift.Infrastructure/
COPY src/SkyShift.Api/SkyShift.Api.csproj src/SkyShift.Api/
# PostgreSQL migrations (referenced by the Api; used when Database__Provider=PostgreSQL).
COPY src/SkyShift.Infrastructure.PostgreSql/SkyShift.Infrastructure.PostgreSql.csproj src/SkyShift.Infrastructure.PostgreSql/
RUN dotnet restore src/SkyShift.Api/SkyShift.Api.csproj
COPY src/ src/
RUN dotnet publish src/SkyShift.Api/SkyShift.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# Production by default. Render's proxy terminates HTTPS and sends X-Forwarded-*,
# which ASP.NET Core honors when ASPNETCORE_FORWARDEDHEADERS_ENABLED is true.
# 8080 is only the fallback; Program.cs listens on $PORT when the host sets it.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
COPY --from=build /app/publish .
# Non-root user provided by the official .NET 8 images.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "SkyShift.Api.dll"]
