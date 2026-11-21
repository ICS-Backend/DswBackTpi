using System.Net;
using System.Net.Http.Json;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Tests.Integration.Products;

/// <summary>
/// TST-05 — Pruebas de integración de POST /api/products.
/// Levantan la API real con CustomWebApplicationFactory (SQLite en memoria) y verifican
/// la seguridad del endpoint, la persistencia real de la fila y el manejo del SKU duplicado.
/// En el código el método del servicio se llama CreateAsync (el enunciado lo llama AddProduct).
/// </summary>
public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Cuerpo válido para POST /api/products (coincide con RequestProductModel).</summary>
    private static object ValidBody(string sku = "SKU-INT-001") => new
    {
        sku,
        internalCode = "INT-001",
        name = "Teclado mecánico",
        description = "Teclado mecánico retroiluminado",
        currentUnitPrice = 85000m,
        stockQuantity = 10
    };

    [Fact]
    public async Task AddProduct_WhenUnauthenticatedOrNotAdmin_ReturnsUnauthorizedOrForbidden()
    {
        await _factory.ResetDatabaseAsync();

        // Arrange: un cliente sin token y otro con un rol distinto de Admin
        var anonymousClient = _factory.CreateClient();
        var userClient = _factory.CreateClientWithRole("User");

        // Act
        var anonymousResponse = await anonymousClient.PostAsJsonAsync("/api/products", ValidBody());
        var userResponse = await userClient.PostAsJsonAsync("/api/products", ValidBody("SKU-INT-002"));

        // Assert: sin token 401, con rol no Admin 403
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, userResponse.StatusCode);

        // Assert: ninguno de los dos llegó a persistir nada
        var total = await _factory.QueryDbAsync(db => db.Products.CountAsync());
        Assert.Equal(0, total);
    }

    [Fact]
    public async Task AddProduct_WhenAuthenticatedAsAdmin_PersistsInSqliteAndReturnsCreated()
    {
        await _factory.ResetDatabaseAsync();

        // Arrange
        var adminClient = _factory.CreateClientWithRole("Admin");
        var body = ValidBody("SKU-INT-010");

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/products", body);

        // Assert: 201 con la cabecera Location que arma CreatedAtAction
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        // Assert: la respuesta trae el producto creado, activo y con Id generado
        var created = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created!.Id);
        Assert.Equal("SKU-INT-010", created.Sku);
        Assert.True(created.IsActive);

        // Assert: la fila existe de verdad en SQLite con los valores enviados
        var saved = await _factory.QueryDbAsync(db =>
            db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Sku == "SKU-INT-010"));

        Assert.NotNull(saved);
        Assert.Equal(created.Id, saved!.Id);
        Assert.Equal("INT-001", saved.InternalCode);
        Assert.Equal("Teclado mecánico", saved.Name);
        Assert.Equal(85000m, saved.CurrentUnitPrice);
        Assert.Equal(10, saved.StockQuantity);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task AddProduct_WhenSkuAlreadyExistsInDatabase_ReturnsConflictStatusCode()
    {
        await _factory.ResetDatabaseAsync();

        // Arrange: ya hay un producto con ese SKU en la base
        await _factory.SeedAsync(new Product(
            "SKU-DUP-001", "INT-DUP", "Mouse inalámbrico", "Mouse 2.4 GHz", 30000m, 5, true));

        var adminClient = _factory.CreateClientWithRole("Admin");

        // Act: se intenta crear otro con el mismo SKU
        var response = await adminClient.PostAsJsonAsync("/api/products", ValidBody("SKU-DUP-001"));

        // Assert: error de negocio controlado (409 o 400), nunca 500
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Conflict, HttpStatusCode.BadRequest });

        // Assert: no se duplicó la fila
        var total = await _factory.QueryDbAsync(db =>
            db.Products.CountAsync(p => p.Sku == "SKU-DUP-001"));
        Assert.Equal(1, total);
    }

    /// <summary>Forma de la respuesta de ProductsController (ResponseProductModel) para deserializar.</summary>
    private record ProductResponse(
        Guid Id,
        string Sku,
        string InternalCode,
        string Name,
        string Description,
        decimal CurrentUnitPrice,
        int StockQuantity,
        bool IsActive);
}
