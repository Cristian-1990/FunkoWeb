using Microsoft.EntityFrameworkCore;
using FunkoApp.Shared.Data;
using FunkoApp.Shared.Data.Seed;

namespace FunkoApp.Shared.Infrastructure;

/// <summary>
/// Configuración de la base de datos. El DbContext y la semilla se registran aquí, no en Program.cs.
/// </summary>
public static class DatabaseConfig
{
    /// <summary>Cadena de conexión que se usa si la configuración no define ninguna.</summary>
    private const string DefaultConnectionString = "Data Source=funkos.db";

    /// <summary>
    /// Registra SQLite y la semilla del catálogo.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <returns>La misma colección, para encadenar llamadas.</returns>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? DefaultConnectionString;

        services.AddDbContext<FunkoDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<FunkoSeeder>();

        return services;
    }
}