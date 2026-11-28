using Microsoft.EntityFrameworkCore;
using FunkoApp.Shared.Models;

namespace FunkoApp.Shared.Data.Seed;

/// <summary>
/// Semilla del catálogo. Es una clase inyectable con un <c>SeedAsync()</c> y no un método
/// estático, para poder usar el contexto y el logger y sustituirla en los tests.
/// Los años y los precios son datos de ejemplo, no los reales de cada figura.
/// </summary>
/// <param name="context">Contexto de la base de datos.</param>
/// <param name="logger">Logger para dejar constancia de lo que se siembra.</param>
public class FunkoSeeder(FunkoDbContext context, ILogger<FunkoSeeder> logger)
{
    /// <summary>
    /// Siembra categorías y Funkos solo si no hay ningún Funko: recargar la app no duplica datos.
    /// </summary>
    /// <returns>Tarea asíncrona.</returns>
    public async Task SeedAsync()
    {
        if (await context.Funkos.AnyAsync())
        {
            return;
        }

        logger.LogInformation("Sembrando categorías y Funkos de ejemplo");

        var tenistas = new Category { Nombre = "Tenistas" };
        var marvel = new Category { Nombre = "Marvel" };
        var dragonBall = new Category { Nombre = "Dragon Ball" };

        context.Categories.AddRange(tenistas, marvel, dragonBall);

        context.Funkos.AddRange(
            new Funko { Nombre = "Venus Williams (equipación clásica)", Anio = 2020, PrecioReferencia = 14.95m, Categoria = tenistas },
            new Funko { Nombre = "Maria Sharapova", Anio = 2020, PrecioReferencia = 14.95m, Categoria = tenistas },
            new Funko { Nombre = "John McEnroe", Anio = 2019, PrecioReferencia = 15.50m, Categoria = tenistas },
            new Funko { Nombre = "Björn Borg", Anio = 2019, PrecioReferencia = 15.50m, Categoria = tenistas },
            new Funko { Nombre = "Amanda Anisimova", Anio = 2024, PrecioReferencia = 16.00m, EsNovedad = true, Categoria = tenistas },
            new Funko { Nombre = "Nick Kyrgios", Anio = 2023, PrecioReferencia = 16.00m, EsNovedad = true, Categoria = tenistas },
            new Funko { Nombre = "Rafael Nadal", Anio = 2021, PrecioReferencia = 17.50m, Categoria = tenistas },
            new Funko { Nombre = "Roger Federer", Anio = 2021, PrecioReferencia = 17.50m, Categoria = tenistas },
            new Funko { Nombre = "Venus Williams (Pop! With Purpose)", Anio = 2022, PrecioReferencia = 18.00m, EsNovedad = true, Categoria = tenistas },

            new Funko { Nombre = "Spider-Man traje negro", Anio = 2021, PrecioReferencia = 14.95m, Categoria = marvel },
            new Funko { Nombre = "Iron Man Mark 85", Anio = 2019, PrecioReferencia = 12.50m, Categoria = marvel },
            new Funko { Nombre = "Capitán América", Anio = 2018, PrecioReferencia = 11.90m, Categoria = marvel },

            new Funko { Nombre = "Goku Super Saiyan", Anio = 2023, PrecioReferencia = 18.50m, EsNovedad = true, Categoria = dragonBall },
            new Funko { Nombre = "Vegeta armadura Saiyan", Anio = 2022, PrecioReferencia = 17.00m, Categoria = dragonBall },
            new Funko { Nombre = "Piccolo", Anio = 2021, PrecioReferencia = 15.25m, Categoria = dragonBall });

        await context.SaveChangesAsync();
    }
}