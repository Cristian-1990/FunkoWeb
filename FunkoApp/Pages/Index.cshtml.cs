using FunkoApp.Shared.Dtos;
using FunkoApp.Shared.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FunkoApp.Pages;

/// <summary>
/// Portada: catálogo de Funkos con buscador por nombre. El PageModel solo pide los datos
/// al servicio y los deja en propiedades que la vista lee; no consulta la base de datos ni monta HTML.
/// </summary>
/// <param name="service">Servicio del catálogo.</param>
/// <param name="logger">Logger de la página.</param>
public class IndexModel(IFunkoService service, ILogger<IndexModel> logger) : PageModel
{
    /// <summary>Funkos del catálogo en formato DTO. La vista nunca ve la entidad.</summary>
    public IReadOnlyList<FunkoDto> Funkos { get; private set; } = [];

    /// <summary>Texto buscado, para que la caja de búsqueda lo conserve tras enviar el formulario.</summary>
    public string? Busqueda { get; private set; }

    /// <summary>
    /// Responde al GET de la página. El parámetro <paramref name="busqueda"/> se enlaza solo
    /// desde la query string (<c>?busqueda=...</c>) por su nombre.
    /// </summary>
    /// <param name="busqueda">Texto que debe contener el nombre; null o vacío para ver todo.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>Tarea asíncrona.</returns>
    public async Task OnGetAsync(string? busqueda, CancellationToken cancellationToken = default)
    {
        Busqueda = busqueda;
        Funkos = await service.GetAllAsync(search: busqueda, cancellationToken: cancellationToken);
        logger.LogInformation("Catálogo servido con {Total} Funkos (búsqueda: {Busqueda})", Funkos.Count, busqueda);
    }
}