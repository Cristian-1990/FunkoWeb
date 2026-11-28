using FunkoApp.Shared.Binders;

namespace FunkoApp.Shared.Infrastructure;

/// <summary>
/// Configuración de Razor Pages.
/// </summary>
public static class MvcConfig
{
    /// <summary>
    /// Registra Razor Pages con el binder de decimales propio.
    /// El binder no va en RazorPagesOptions (esa opción solo admite convenciones de página),
    /// sino en la configuración de MVC que Razor Pages usa por debajo: AddMvcOptions.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>La misma colección, para encadenar llamadas.</returns>
    public static IServiceCollection AddRazorPagesWeb(this IServiceCollection services)
    {
        services.AddRazorPages()
            .AddMvcOptions(options =>
            {
                // En primera posición: gana al binder por defecto, que usa la cultura del hilo.
                options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
            });

        return services;
    }
}
