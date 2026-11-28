using FunkoApp.Shared.Services;
using FunkoApp.Shared.Services.Storage;

namespace FunkoApp.Shared.Infrastructure;

/// <summary>
/// Configuración de los servicios de negocio.
/// </summary>
public static class ServicesConfig
{
    /// <summary>
    /// Registra los servicios del catálogo y del almacenamiento de ficheros en el contenedor de dependencias.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>La misma colección, para encadenar llamadas.</returns>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IFunkoService, FunkoService>();
        services.AddScoped<IStorageService, StorageService>();
        return services;
    }
}