using System.Linq;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Moq;
using static Dsw2025Tpi.Application.Dtos.OrderModel;
using static Dsw2025Tpi.Application.Dtos.OrderItemModel;

namespace Dsw2025Tpi.Tests.Unit.Orders;

// TST-03 — Tests unitarios de creación de orden (Cliente)
// Servicio bajo prueba: OrdersManagementService.AddOrder (servicio de órdenes / CreateOrder)
public class OrdersServiceTests
{
    private readonly Mock<IRepository> _repositoryMock = new();
    private readonly OrdersManagementService _sut;

    public OrdersServiceTests()
    {
        _sut = new OrdersManagementService(_repositoryMock.Object);
    }

    private static Customer BuildCustomer(Guid id)
        => new Customer("cliente@test.com", "Cliente de Prueba", "3815000000") { Id = id };

    private static Product BuildProduct(Guid id, int stock, decimal unitPrice = 100m)
        => new Product("SKU-001", "COD-001", "Mouse Gamer", "Mouse para pruebas", unitPrice, stock, true) { Id = id };

    private static RequestOrderModel BuildRequest(Guid customerId, Guid productId, int quantity)
        => new RequestOrderModel(
            customerId,
            "Av. Siempre Viva 742",
            "Av. Siempre Viva 742",
            new List<RequestOrderItemModel> { new RequestOrderItemModel(quantity, productId) }
        );

    [Fact]
    public async Task CreateOrder_WithValidItemsAndStock_CalculatesTotalAndDecrementsProductStock()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        const int initialStock = 10;
        const int quantity = 3;

        var customer = BuildCustomer(customerId);
        var product = BuildProduct(productId, initialStock, unitPrice: 50m);
        var request = BuildRequest(customerId, productId, quantity);

        _repositoryMock.Setup(r => r.GetById<Customer>(customerId, It.IsAny<string[]>()))
            .ReturnsAsync(customer);
        _repositoryMock.Setup(r => r.GetById<Product>(productId, It.IsAny<string[]>()))
            .ReturnsAsync(product);
        _repositoryMock.Setup(r => r.Update(It.IsAny<Product>()))
            .Returns<Product>(p => Task.FromResult(p));
        _repositoryMock.Setup(r => r.Add(It.IsAny<Order>()))
            .Returns<Order>(o => Task.FromResult(o));

        // Act
        var result = await _sut.AddOrder(request);

        // Assert
        var expectedTotal = quantity * product.CurrentUnitPrice;
        var actualTotal = result.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);
        Assert.Equal(expectedTotal, actualTotal);

        Assert.Equal(initialStock - quantity, product.StockQuantity);
        _repositoryMock.Verify(r => r.Update(It.Is<Product>(p => p.Id == productId)), Times.Once);
        _repositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_WhenProductStockIsInsufficient_ThrowsInsufficientStockException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        const int initialStock = 2;
        const int quantity = 5;

        var customer = BuildCustomer(customerId);
        var product = BuildProduct(productId, initialStock);
        var request = BuildRequest(customerId, productId, quantity);

        _repositoryMock.Setup(r => r.GetById<Customer>(customerId, It.IsAny<string[]>()))
            .ReturnsAsync(customer);
        _repositoryMock.Setup(r => r.GetById<Product>(productId, It.IsAny<string[]>()))
            .ReturnsAsync(product);

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientStockException>(() => _sut.AddOrder(request));

        Assert.Equal(initialStock, product.StockQuantity); // no se modificó el stock
        _repositoryMock.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
        _repositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_WhenCustomerDoesNotExist_ThrowsCustomerNotFoundException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var request = BuildRequest(customerId, productId, quantity: 1);

        _repositoryMock.Setup(r => r.GetById<Customer>(customerId, It.IsAny<string[]>()))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        await Assert.ThrowsAsync<CustomerNotFoundException>(() => _sut.AddOrder(request));

        // no se debe tocar ningún producto ni persistir la orden
        _repositoryMock.Verify(r => r.GetById<Product>(It.IsAny<Guid>(), It.IsAny<string[]>()), Times.Never);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
        _repositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }
}
