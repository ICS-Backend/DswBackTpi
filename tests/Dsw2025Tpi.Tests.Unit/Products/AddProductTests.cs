using System.Linq.Expressions;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Dsw2025Tpi.Application.Dtos.ProductModel;
using InvalidDataException = Dsw2025Tpi.Application.Exceptions.InvalidDataException;

namespace Dsw2025Tpi.Tests.Unit.Products;

/// <summary>
/// TST-02 — Creación de productos (Admin).
/// En el código el método se llama CreateAsync (el enunciado lo llama AddProduct).
/// </summary>
public class AddProductTests
{
    private readonly Mock<IRepository> _repositoryMock = new();
    private readonly ProductsManagementService _service;

    public AddProductTests()
    {
        _service = new ProductsManagementService(
            _repositoryMock.Object,
            NullLogger<ProductsManagementService>.Instance);
    }

    private static RequestProductModel ValidRequest(string sku = "SKU-001") =>
        new(sku, "INT-001", "Mouse Gamer", "Mouse con 6 botones", 15000m, 10);

    [Fact]
    public async Task AddProduct_WithValidData_ReturnsProductAndCallsRepositoryAdd()
    {
        // Arrange: no existe otro producto con ese SKU y Add devuelve lo que recibe
        _repositoryMock
            .Setup(r => r.First<Product>(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Product?)null);
        _repositoryMock
            .Setup(r => r.Add(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        var request = ValidRequest();

        // Act
        var result = await _service.CreateAsync(request);

        // Assert: devuelve el producto con los datos enviados
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Sku, result.Sku);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.CurrentUnitPrice, result.CurrentUnitPrice);
        Assert.Equal(request.StockQuantity, result.StockQuantity);
        Assert.True(result.IsActive);

        // Assert: el repositorio se llamó exactamente una vez con ese producto
        _repositoryMock.Verify(
            r => r.Add(It.Is<Product>(p => p.Sku == request.Sku && p.Name == request.Name)),
            Times.Once);
    }

    [Fact]
    public async Task AddProduct_WithDuplicateSku_ThrowsDuplicatedEntityException()
    {
        // Arrange: el repositorio simula que ya existe un producto con el mismo SKU
        var existing = new Product("SKU-001", "INT-999", "Otro producto", "Ya cargado", 1000m, 5, true);
        _repositoryMock
            .Setup(r => r.First<Product>(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync(existing);

        // Act + Assert
        await Assert.ThrowsAsync<DuplicatedEntityException>(() => _service.CreateAsync(ValidRequest("SKU-001")));

        // No se debe intentar guardar
        _repositoryMock.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
    }

    [Theory]
    [InlineData(15000, 0)]   // stock igual a cero
    [InlineData(15000, -1)]  // stock negativo
    [InlineData(-10, 5)]     // precio negativo
    public async Task AddProduct_WithInvalidStockOrPrice_ThrowsInvalidDataException(int price, int stock)
    {
        // Arrange
        var request = ValidRequest() with { CurrentUnitPrice = price, StockQuantity = stock };

        // Act + Assert: debe lanzar InvalidDataException con un mensaje
        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => _service.CreateAsync(request));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));

        // Falla ANTES de consultar o guardar: el repositorio no se usa para nada
        _repositoryMock.VerifyNoOtherCalls();
    }
}
