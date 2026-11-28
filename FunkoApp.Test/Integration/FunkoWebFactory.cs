using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace FunkoApp.Test.Integration;

/// <summary>
/// Arranca la aplicación real en memoria para las pruebas de integración.
/// Cada fábrica trabaja en una carpeta temporal propia: su base de datos SQLite y su wwwroot
/// (donde caen las imágenes subidas). Así las pruebas no tocan el funkos.db ni los uploads
/// del proyecto, y se borra todo al terminar.
/// El entorno es Testing: la aplicación crea el esquema pero no siembra datos; los pone cada prueba.
/// </summary>
public class FunkoWebFactory : WebApplicationFactory<Program>
{
    /// <summary>Carpeta temporal de esta fábrica.</summary>
    public string Raiz { get; } = Path.Combine(Path.GetTempPath(), $"funkoapp-it-{Guid.NewGuid():N}");

    /// <summary>Raíz de ficheros estáticos de la prueba: ahí se guardan las imágenes subidas.</summary>
    public string WebRoot => Path.Combine(Raiz, "wwwroot");

    /// <summary>Ruta del fichero SQLite de la prueba.</summary>
    public string RutaBaseDeDatos => Path.Combine(Raiz, "funkos-test.db");

    public FunkoWebFactory()
    {
        Directory.CreateDirectory(WebRoot);
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseWebRoot(WebRoot);
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={RutaBaseDeDatos}");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        // SQLite mantiene el fichero abierto en su pool de conexiones: hay que soltarlo antes de borrar.
        SqliteConnection.ClearAllPools();

        try
        {
            Directory.Delete(Raiz, recursive: true);
        }
        catch (IOException)
        {
            // Borde del sistema: si el sistema operativo aún retiene algo, queda en la carpeta temporal.
        }
    }
}
