using System.Linq;
using System.Linq.Expressions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Dsw2025Tpi.Application.Dtos.ProductModel;

namespace Dsw2025Tpi.Tests.Unit.Products;

// TST-04 — Tests unitarios de listado de productos (Admin)
// Servicio bajo prueba: ProductsManagementService.GetProducts
public class ProductsServiceTests
{
    private readonly Mock<IRepository> _repositoryMock = new();
    private readonly ProductsManagementService _sut;

    public ProductsServiceTests()
    {
        _sut = new ProductsManagementService(_repositoryMock.Object, NullLogger<ProductsManagementService>.Instance);
    }

    private static Product BuildProduct(string sku, string internalCode, string name, bool isActive, decimal price = 100m, int stock = 10)
        => new Product(sku, internalCode, name, $"Descripción de {name}", price, stock, isActive);

    /// <summary>
    /// Hace que el mock de GetFiltered evalúe de verdad el predicado que arma el
    /// servicio contra una lista de prueba en memoria, en vez de devolver datos fijos.
    /// Así los tests validan la lógica real de filtrado/búsqueda de GetProducts.
    /// </summary>
    private void SetupRepositoryWith(List<Product> products)
    {
        _repositoryMock
            .Setup(r => r.GetFiltered<Product>(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .Returns((Expression<Func<Product, bool>> predicate, string[] _) =>
                Task.FromResult<IEnumerable<Product>?>(products.Where(predicate.Compile()).ToList()));
    }

    [Fact]
    public async Task GetProducts_WhenIncludeInactiveIsFalse_ReturnsOnlyActiveProducts()
    {
        // Arrange: 4 activos y 3 inactivos
        var products = new List<Product>
        {
            BuildProduct("SKU-01", "COD-01", "Mouse Gamer", isActive: true),
            BuildProduct("SKU-02", "COD-02", "Teclado Mecánico", isActive: true),
            BuildProduct("SKU-03", "COD-03", "Monitor 24''", isActive: true),
            BuildProduct("SKU-04", "COD-04", "Auriculares", isActive: true),
            BuildProduct("SKU-05", "COD-05", "Mousepad", isActive: false),
            BuildProduct("SKU-06", "COD-06", "Webcam", isActive: false),
            BuildProduct("SKU-07", "COD-07", "Parlante", isActive: false),
        };
        SetupRepositoryWith(products);

        // Act: sin filtro de Status (comportamiento por defecto -> solo activos)
        var request = new FilterProduct(Status: null, Search: null, PageNumber: null, PageSize: null);
        var result = await _sut.GetProducts(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result!.Total);
        Assert.Equal(4, result.ProductItems.Count);
        Assert.All(result.ProductItems, p => Assert.True(p.IsActive));
    }

    [Fact]
    public async Task GetProducts_WithPaginationFilter_AppliesSkipAndTakeCorrectly()
    {
        // Arrange: 15 productos activos, ordenados por Sku (orden que usa el servicio)
        var products = Enumerable.Range(1, 15)
            .Select(i => BuildProduct($"SKU-{i:D2}", $"COD-{i:D2}", $"Producto {i:D2}", isActive: true))
            .ToList();
        SetupRepositoryWith(products);

        // Act: página 2, tamaño 5
        var request = new FilterProduct(Status: null, Search: null, PageNumber: 2, PageSize: 5);
        var result = await _sut.GetProducts(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(15, result!.Total);
        Assert.Equal(5, result.ProductItems.Count);

        var expectedSkus = products
            .OrderBy(p => p.Sku)
            .Skip(5)
            .Take(5)
            .Select(p => p.Sku);
        Assert.Equal(expectedSkus, result.ProductItems.Select(p => p.Sku));
    }

    [Fact]
    public async Task GetProducts_WithSearchTerm_FiltersByNameSkuOrInternalCode()
    {
        // Arrange: "mouse" debe matchear por Name, por Sku o por InternalCode,
        // sin distinguir mayúsculas, y no debe traer lo que no coincide en ningún campo.
        var matchByName = BuildProduct("MG-001", "INT-001", "Mouse Gamer", isActive: true);
        var matchBySku = BuildProduct("TK-MOUSE-02", "INT-002", "Teclado Mecánico", isActive: true);
        var matchByInternalCode = BuildProduct("MN-003", "MOUSE-COD-03", "Monitor 24''", isActive: true);
        var noMatch = BuildProduct("SG-004", "INT-004", "Silla Gamer", isActive: true);

        var products = new List<Product> { matchByName, matchBySku, matchByInternalCode, noMatch };
        SetupRepositoryWith(products);

        // Act
        var request = new FilterProduct(Status: null, Search: "mouse", PageNumber: null, PageSize: null);
        var result = await _sut.GetProducts(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result!.Total);
        Assert.Equal(3, result.ProductItems.Count);

        var returnedSkus = result.ProductItems.Select(p => p.Sku).ToList();
        Assert.Contains(matchByName.Sku, returnedSkus);
        Assert.Contains(matchBySku.Sku, returnedSkus);
        Assert.Contains(matchByInternalCode.Sku, returnedSkus);
        Assert.DoesNotContain(noMatch.Sku, returnedSkus);
    }
}
