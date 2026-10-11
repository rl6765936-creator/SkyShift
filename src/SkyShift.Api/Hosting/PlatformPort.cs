using Microsoft.Extensions.Configuration;

namespace SkyShift.Api.Hosting;

/// <summary>
/// Resolves the HTTP listen URL from the hosting platform's PORT variable (Render sets it).
/// Returns null when PORT is absent or invalid, so local runs keep launchSettings.json URLs.
/// </summary>
public static class PlatformPort
{
    public static string? ResolveUrl(IConfiguration configuration) =>
        int.TryParse(configuration["PORT"], out var port) && port is > 0 and <= 65535
            ? $"http://0.0.0.0:{port}"
            : null;
}
