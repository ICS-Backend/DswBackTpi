using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Tests.Integration.Orders;

// TST-06 — Tests de integración de POST /api/orders
// El endpoint exige el rol "User" (no Admin).
public class OrdersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Url = "/api/orders";
    private readonly CustomWebApplicationFactory _factory;

    public OrdersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Arma el body que espera la API (RequestOrderModel)
    private static object BuildOrderBody(Guid customerId, Guid productId, int quantity) => new
    {
        customerId,
        shippingAddress = "Av. Mate de Luna 1234, Tucumán",
        billingAddress = "Av. Mate de Luna 1234, Tucumán",
        orderItems = new[] { new { quantity, productId } }
    };

    // Siembra un cliente y un producto con el stock indicado
    private async Task<(Customer customer, Product product)> SeedCustomerAndProductAsync(int stock)
    {
        await _factory.ResetDatabaseAsync();

        var customer = new Customer("cliente@test.com", "Cliente de Prueba", "3815000000");
        var product = new Product("SKU-ORD-01", "INT-ORD-01", "Teclado", "Teclado para pruebas", 1500m, stock, true);

        await _factory.SeedAsync(customer, product);
        return (customer, product);
    }

    [Fact]
    public async Task CreateOrder_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Arrange: cliente HTTP sin token
        var client = _factory.CreateClient();
        var body = BuildOrderBody(Guid.NewGuid(), Guid.NewGuid(), 1);

        // Act
        var response = await client.PostAsJsonAsync(Url, body);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WhenAuthenticatedWithValidData_ReturnsCreatedAndPersistsOrderAndStockInSqlite()
    {
        // Arrange: producto con stock 10, se piden 3
        var (customer, product) = await SeedCustomerAndProductAsync(stock: 10);
        var client = _factory.CreateClientWithRole("User");
        var body = BuildOrderBody(customer.Id, product.Id, 3);

        // Act
        var response = await client.PostAsJsonAsync(Url, body);

        // Assert: 201 con header Location
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        // Assert: la orden quedó guardada en SQLite con su ítem
        var orders = await _factory.QueryDbAsync(db => db.Orders.Include(o => o.OrderItems).ToListAsync());
        var order = Assert.Single(orders);
        Assert.Equal(customer.Id, order.CustomerId);
        var item = Assert.Single(order.OrderItems);
        Assert.Equal(3, item.Quantity);

        // Assert: el stock bajó de 10 a 7
        var stock = await _factory.QueryDbAsync(db =>
            db.Products.Where(p => p.Id == product.Id).Select(p => p.StockQuantity).SingleAsync());
        Assert.Equal(7, stock);
    }

    [Fact]
    public async Task CreateOrder_WhenStockIsInsufficient_ReturnsBadRequestWithErrorMessage()
    {
        // Arrange: producto con stock 2, se piden 5
        var (customer, product) = await SeedCustomerAndProductAsync(stock: 2);
        var client = _factory.CreateClientWithRole("User");
        var body = BuildOrderBody(customer.Id, product.Id, 5);

        // Act
        var response = await client.PostAsJsonAsync(Url, body);

        // Assert: 400 con un JSON que indica la falta de stock
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var message = json.GetProperty("message").GetString();
        Assert.Contains("stock", message, StringComparison.OrdinalIgnoreCase);

        // Assert: no se guardó ninguna orden y el stock sigue en 2
        Assert.Equal(0, await _factory.QueryDbAsync(db => db.Orders.CountAsync()));
        var stock = await _factory.QueryDbAsync(db =>
            db.Products.Where(p => p.Id == product.Id).Select(p => p.StockQuantity).SingleAsync());
        Assert.Equal(2, stock);
    }
}
