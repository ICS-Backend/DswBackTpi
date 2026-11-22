using System.Net;
using System.Net.Http.Json;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Tests.Integration.Infrastructure;

namespace Dsw2025Tpi.Tests.Integration.Products;

public class ProductsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProducts_PublicEndpoint_ReturnsOkWithPaginatedJsonStructure()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.SeedAsync(
            new Product(
                "SKU-001",
                "INT-001",
                "Mouse Logitech",
                "Mouse inalámbrico",
                25000m,
                10,
                true),
            new Product(
                "SKU-002",
                "INT-002",
                "Teclado Redragon",
                "Teclado mecánico",
                40000m,
                8,
                true)
        );

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/products?pageNumber=1&pageSize=6");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<
                ProductModel.PagedResult<ProductModel.ResponseProductModel>>();

        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetProducts_WithQueryStringFilters_ReturnsFilteredResultsFromSqlite()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.SeedAsync(
            new Product(
                "SKU-001",
                "INT-001",
                "Mouse Logitech",
                "Mouse inalámbrico",
                25000m,
                10,
                true),
            new Product(
                "SKU-002",
                "INT-002",
                "Mouse Gamer Redragon",
                "Mouse gamer",
                30000m,
                5,
                true),
            new Product(
                "SKU-003",
                "INT-003",
                "Teclado Mecánico",
                "Teclado gamer",
                45000m,
                7,
                true)
        );

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/products?pageNumber=1&pageSize=10&search=mouse");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<
                ProductModel.PagedResult<ProductModel.ResponseProductModel>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);

        Assert.All(
            result.Items,
            product => Assert.Contains(
                "mouse",
                product.Name,
                StringComparison.OrdinalIgnoreCase));
    }
[Fact]
public async Task GetAuthProducts_AdminEndpoint_WhenCalledByAdmin_IncludesInactiveProducts()
{
    // Arrange
    await _factory.ResetDatabaseAsync();

    await _factory.SeedAsync(
        new Product(
            "SKU-001",
            "INT-001",
            "Mouse Logitech",
            "Producto activo",
            25000m,
            10,
            true),
        new Product(
            "SKU-002",
            "INT-002",
            "Teclado Redragon",
            "Producto inactivo",
            40000m,
            8,
            false)
    );

    var client = _factory.CreateClientWithRole("Admin");

    // Act
    var response = await client.GetAsync(
        "/api/products/admin?Status=all");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var result =
        await response.Content.ReadFromJsonAsync<
            ProductModel.ResponsePagination>();

    Assert.NotNull(result);
    Assert.Equal(2, result.Total);
    Assert.Equal(2, result.ProductItems.Count);

    Assert.Contains(result.ProductItems, product => product.IsActive);
    Assert.Contains(result.ProductItems, product => !product.IsActive);
}    
}