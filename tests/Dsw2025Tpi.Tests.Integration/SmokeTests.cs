using System.Net;
using Dsw2025Tpi.Tests.Integration.Infrastructure;

namespace Dsw2025Tpi.Tests.Integration;

/// <summary>
/// Tests de ejemplo de TST-01: comprueban que la API levanta y que consulta SQLite.
/// </summary>
public class SmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SmokeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithEmptyDatabase_ReturnsNoContent()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        // El controller devuelve 204 cuando no hay productos activos
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
