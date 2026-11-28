using FunkoApp.Shared.Dtos;
using FunkoApp.Shared.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FunkoApp.Pages;

/// <summary>
/// Portada: catálogo de Funkos. El PageModel solo pide los datos al servicio
/// y los deja en una propiedad que la vista lee; no consulta la base de datos ni monta HTML.
/// </summary>
/// <param name="service">Servicio del catálogo.</param>
/// <param name="logger">Logger de la página.</param>
public class IndexModel(IFunkoService service, ILogger<IndexModel> logger) : PageModel
{
    /// <summary>Funkos del catálogo en formato DTO. La vista nunca ve la entidad.</summary>
    public IReadOnlyList<FunkoDto> Funkos { get; private set; } = [];

    /// <summary>
    /// Responde al GET de la página. El nombre <c>OnGetAsync</c> lo busca el framework por el verbo HTTP.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>Tarea asíncrona.</returns>
    public async Task OnGetAsync(CancellationToken cancellationToken = default)
    {
        Funkos = await service.GetAllAsync(cancellationToken: cancellationToken);
        logger.LogInformation("Catálogo servido con {Total} Funkos", Funkos.Count);
    }
}