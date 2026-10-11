using Microsoft.Extensions.Configuration;
using SkyShift.Api.Hosting;
using Xunit;

namespace SkyShift.Tests.Api;

/// <summary>
/// The API listens on the platform PORT when one is provided, and otherwise leaves the
/// standard ASP.NET Core URL settings untouched.
/// </summary>
public class PlatformPortTests
{
    private static IConfiguration With(string? port) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["PORT"] = port })
        .Build();

    [Theory]
    [InlineData("10000", "http://0.0.0.0:10000")]
    [InlineData("8080", "http://0.0.0.0:8080")]
    public void ValidPort_ListensOnAllInterfaces(string port, string expected) =>
        Assert.Equal(expected, PlatformPort.ResolveUrl(With(port)));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("70000")]
    public void MissingOrInvalidPort_KeepsDefaultUrls(string? port) =>
        Assert.Null(PlatformPort.ResolveUrl(With(port)));
}
