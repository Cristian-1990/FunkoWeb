using FunkoApp.Shared.Data;
using FunkoApp.Shared.Data.Seed;

namespace FunkoApp.Shared.Infrastructure;

/// <summary>
/// Inicialización de la base de datos al arrancar la aplicación.
/// </summary>
public static class DatabaseInitializationExtensions
{
    /// <summary>
    /// Crea la base de datos y la siembra si hace falta.
    /// En Development borra el fichero y lo recrea con datos conocidos en cada arranque.
    /// En cualquier otro entorno solo crea el esquema si no existe y no siembra nada.
    /// </summary>
    /// <param name="app">Aplicación construida.</param>
    /// <param name="isDevelopment">Si el entorno actual es Development.</param>
    /// <returns>Tarea asíncrona.</returns>
    public static async Task InitializeDatabaseAsync(this WebApplication app, bool isDevelopment)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FunkoDbContext>();

        if (isDevelopment)
        {
            await context.Database.EnsureDeletedAsync();
        }

        await context.Database.EnsureCreatedAsync();

        if (isDevelopment)
        {
            var seeder = scope.ServiceProvider.GetRequiredService<FunkoSeeder>();
            await seeder.SeedAsync();
        }
    }
}