using FunkoApp.Shared.Dtos;
using FunkoApp.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FunkoApp.Pages;

/// <summary>
/// Funkos de una categoría. El identificador llega en la ruta (<c>/Categoria/2</c>)
/// y el Model Binding lo convierte solo, sin ningún <c>Parse</c> en el handler.
/// </summary>
/// <param name="service">Servicio del catálogo.</param>
/// <param name="logger">Logger de la página.</param>
public class CategoriaModel(IFunkoService service, ILogger<CategoriaModel> logger) : PageModel
{
    /// <summary>Categoría pedida, para el título de la página.</summary>
    public CategoryDto Categoria { get; private set; } = null!;

    /// <summary>Funkos que pertenecen a esa categoría.</summary>
    public IReadOnlyList<FunkoDto> Funkos { get; private set; } = [];

    /// <summary>
    /// Responde al GET de la página. Si la categoría no existe devuelve un 404.
    /// </summary>
    /// <param name="id">Identificador de la categoría, tomado de la ruta.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>La página con los Funkos, o un 404.</returns>
    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken = default)
    {
        var categorias = await service.GetCategoriesAsync(cancellationToken);
        var categoria = categorias.FirstOrDefault(c => c.Id == id);

        if (categoria is null)
        {
            logger.LogWarning("Categoría {Id} no encontrada", id);
            return NotFound();
        }

        Categoria = categoria;
        Funkos = await service.GetAllAsync(categoryId: id, cancellationToken: cancellationToken);

        return Page();
    }
}