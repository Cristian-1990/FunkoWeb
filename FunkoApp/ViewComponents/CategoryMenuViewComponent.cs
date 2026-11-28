using FunkoApp.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApp.ViewComponents;

/// <summary>
/// Componente de vista: los enlaces a las categorías del menú de navegación.
/// Su vista está en Pages/Shared/Components/CategoryMenu/Default.cshtml
/// y se invoca con <c>&lt;vc:category-menu /&gt;</c>.
/// </summary>
/// <param name="service">Servicio del catálogo.</param>
public class CategoryMenuViewComponent(IFunkoService service) : ViewComponent
{
    /// <summary>
    /// Carga las categorías y devuelve su vista por defecto.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>Vista del componente con las categorías ya cargadas.</returns>
    public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken = default)
    {
        var categorias = await service.GetCategoriesAsync(cancellationToken);
        return View(categorias);
    }
}
