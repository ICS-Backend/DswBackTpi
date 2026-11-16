using Dsw2025Tpi.Data;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Dsw2025Tpi.Tests.Integration.Infrastructure;

/// <summary>
/// Levanta la API real en memoria, pero reemplaza SQL Server por SQLite en memoria.
/// Se usa en los tests con IClassFixture&lt;CustomWebApplicationFactory&gt;.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Dsw2025Tpi.Api.Program>
{
    // Clave solo para tests (32+ caracteres). La misma se usa para firmar los tokens de JwtTokenHelper.
    public const string JwtKey = "clave-de-tests-de-integracion-dsw2025tpi-0123456789";
    public const string JwtIssuer = "Dsw2025Tpi.Api";
    public const string JwtAudience = "Dsw2025Tpi.Client";

    // SQLite en memoria vive mientras la conexión esté abierta: por eso se abre una vez y se reutiliza.
    private readonly SqliteConnection _mainConnection = new("DataSource=:memory:");
    private readonly SqliteConnection _authConnection = new("DataSource=:memory:");

    public CustomWebApplicationFactory()
    {
        // Program.cs lee Jwt:Key ANTES de builder.Build(), así que se pasa por variable de entorno
        // (las lee WebApplication.CreateBuilder al arrancar).
        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        // Evita MigrateAsync (las migraciones son de SQL Server) y la carga del admin/roles.
        Environment.SetEnvironmentVariable("Initialization__RunOnStartup", "false");

        _mainConnection.Open();
        _authConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Quitar la configuración de SQL Server registrada en Program.cs
            services.RemoveAll<DbContextOptions<Dsw2025TpiContext>>();
            services.RemoveAll<DbContextOptions<AuthenticateContext>>();

            // Registrar los mismos contextos sobre SQLite
            services.AddDbContext<Dsw2025TpiContext>(o => o.UseSqlite(_mainConnection));
            services.AddDbContext<AuthenticateContext>(o => o.UseSqlite(_authConnection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Crear las tablas a partir del modelo (sin migraciones)
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>().Database.EnsureCreated();
        scope.ServiceProvider.GetRequiredService<AuthenticateContext>().Database.EnsureCreated();

        return host;
    }

    /// <summary>Inserta entidades (productos, clientes, etc.) directamente en SQLite.</summary>
    public async Task SeedAsync(params EntityBase[] entities)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        db.AddRange(entities);
        await db.SaveChangesAsync();
    }

    /// <summary>Permite consultar la base dentro de un test (ej: verificar que se insertó una fila).</summary>
    public async Task<T> QueryDbAsync<T>(Func<Dsw2025TpiContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        return await query(db);
    }

    /// <summary>Vacía las tablas de negocio. Llamarlo al inicio de cada test que siembre datos.</summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        await db.OrderItems.ExecuteDeleteAsync();
        await db.Orders.ExecuteDeleteAsync();
        await db.Products.ExecuteDeleteAsync();
        await db.Customers.ExecuteDeleteAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _mainConnection.Dispose();
            _authConnection.Dispose();
        }
    }
}
