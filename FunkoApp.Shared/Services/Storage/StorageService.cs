using Microsoft.AspNetCore.Http;

namespace FunkoApp.Shared.Services.Storage;

/// <summary>
/// Almacenamiento de los ficheros subidos en la carpeta <c>wwwroot/uploads</c>.
/// Es el único sitio que escribe y borra en el disco del almacén. Los límites y la carpeta
/// viven en la sección Storage de la configuración, no en el código.
/// </summary>
/// <param name="environment">Entorno web: aporta la raíz de ficheros estáticos (wwwroot).</param>
/// <param name="configuration">Configuración: sección Storage (carpeta, techo y extensiones).</param>
/// <param name="logger">Logger del servicio.</param>
public class StorageService(
    IWebHostEnvironment environment,
    IConfiguration configuration,
    ILogger<StorageService> logger) : IStorageService
{
    /// <summary>Carpeta de subidas dentro de wwwroot.</summary>
    private readonly string _uploadPath = configuration["Storage:UploadPath"] ?? "uploads";

    /// <summary>Techo máximo por fichero, en bytes.</summary>
    private readonly long _maxFileSize = configuration.GetValue<long>("Storage:MaxFileSize", 5 * 1024 * 1024);

    /// <summary>Lista blanca de extensiones admitidas.</summary>
    private readonly string[] _allowedExtensions =
        configuration.GetSection("Storage:AllowedExtensions").Get<string[]>()
        ?? [".jpg", ".jpeg", ".png", ".gif"];

    /// <summary>Raíz de ficheros estáticos (wwwroot): contra ella se resuelve una URL.</summary>
    private readonly string _webRootPath = environment.WebRootPath
        ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

    /// <summary>Ruta absoluta del almacén: wwwroot más la carpeta de subidas.</summary>
    private string RaizSubidas => Path.Combine(_webRootPath, _uploadPath);

    /// <inheritdoc />
    public async Task<StorageResult> SaveFileAsync(IFormFile? file, string folder)
    {
        // 1. El fichero tiene que existir y traer algo dentro.
        if (file is null || file.Length == 0)
        {
            return StorageResult.Fallo("No has elegido ningún fichero");
        }

        // 2. Techo por fichero.
        if (file.Length > _maxFileSize)
        {
            var megas = _maxFileSize / 1024 / 1024;
            return StorageResult.Fallo($"El fichero es demasiado grande: el máximo es {megas} MB");
        }

        // 3. Lista blanca de extensiones, decidida por la configuración.
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            return StorageResult.Fallo(
                $"La extensión {extension} no está permitida; se admiten {string.Join(", ", _allowedExtensions)}");
        }

        // 4. Las firmas delatan el tipo real: un .exe renombrado a .png no empieza por la firma de un PNG.
        if (!await FirmaValidaAsync(file, extension))
        {
            return StorageResult.Fallo($"El contenido del fichero no corresponde a una imagen {extension}");
        }

        // 5. La subcarpeta la pone el código de la aplicación; GetFileName quita cualquier ruta.
        var carpeta = Path.GetFileName(folder);
        if (string.IsNullOrWhiteSpace(carpeta))
        {
            return StorageResult.Fallo("La carpeta de destino no es válida");
        }

        var destino = Path.Combine(RaizSubidas, carpeta);
        Directory.CreateDirectory(destino);

        // 6. Nombre único: marca temporal, guion corto y nombre saneado. El nombre del cliente
        //    solo aporta la parte legible; la ruta nunca se forma con él.
        var nombreUnico = $"{DateTime.UtcNow:yyyyMMddHHmmss}" +
                          $"_{Guid.NewGuid().ToString("N")[..8]}" +
                          $"_{NombreBase(file.FileName)}{extension}";

        await using (var stream = new FileStream(Path.Combine(destino, nombreUnico), FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var rutaPublica = $"/{_uploadPath}/{carpeta}/{nombreUnico}";
        logger.LogInformation("Fichero {Original} guardado como {Ruta}", file.FileName, rutaPublica);
        return StorageResult.Exito(rutaPublica);
    }

    /// <inheritdoc />
    public bool DeleteFile(string? publicPath)
    {
        if (string.IsNullOrWhiteSpace(publicPath))
        {
            return false;
        }

        // La ruta pública se resuelve contra wwwroot y después se comprueba que el resultado
        // siga DENTRO de la carpeta de subidas. Así ni "..", ni una ruta absoluta, ni la imagen
        // por defecto de wwwroot/images pueden borrarse por esta vía.
        var raiz = Path.GetFullPath(RaizSubidas);
        var relativo = publicPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var completo = Path.GetFullPath(Path.Combine(_webRootPath, relativo));

        if (!completo.StartsWith(raiz + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("La ruta {Ruta} no está en el almacén de subidas: no se borra", publicPath);
            return false;
        }

        if (!File.Exists(completo))
        {
            return false;
        }

        // El disco es un borde del sistema: aquí se capturan sus fallos y se traducen a un false.
        try
        {
            File.Delete(completo);
            logger.LogInformation("Fichero {Ruta} borrado", publicPath);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "No se pudo borrar el fichero {Ruta}", publicPath);
            return false;
        }
    }

    /// <summary>
    /// Comprueba los primeros bytes del fichero (los números mágicos) contra la firma de su extensión.
    /// </summary>
    /// <param name="file">Fichero subido.</param>
    /// <param name="extension">Extensión declarada, ya en minúsculas.</param>
    /// <returns>true si el contenido corresponde a esa extensión.</returns>
    private static async Task<bool> FirmaValidaAsync(IFormFile file, string extension)
    {
        await using var stream = file.OpenReadStream();

        var cabecera = new byte[8];
        var leidos = await stream.ReadAsync(cabecera);
        var inicio = cabecera.AsSpan(0, leidos);

        return extension switch
        {
            ".jpg" or ".jpeg" => Empieza(inicio, [0xFF, 0xD8, 0xFF]),
            ".png" => Empieza(inicio, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            ".gif" => Empieza(inicio, "GIF87a"u8) || Empieza(inicio, "GIF89a"u8),
            _ => false
        };
    }

    private static bool Empieza(ReadOnlySpan<byte> contenido, ReadOnlySpan<byte> firma) =>
        contenido.StartsWith(firma);

    /// <summary>
    /// Deja el nombre del cliente inofensivo: sin ruta, sin puntos y solo con letras, dígitos,
    /// guiones y guiones bajos.
    /// </summary>
    /// <param name="fileName">Nombre original, tal cual llega del navegador.</param>
    /// <returns>La parte legible del nombre, lista para componer el nombre único.</returns>
    private static string NombreBase(string fileName)
    {
        var nombre = Path.GetFileNameWithoutExtension(fileName);
        var saneado = new string(nombre.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());

        if (saneado.Length == 0)
        {
            return "imagen";
        }

        return saneado.Length > 40 ? saneado[..40] : saneado;
    }
}
