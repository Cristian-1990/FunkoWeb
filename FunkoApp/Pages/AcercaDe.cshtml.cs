using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FunkoApp.Pages;

/// <summary>
/// Página con la ficha del autor de la aplicación.
/// </summary>
public class AcercaDeModel : PageModel
{
    /// <summary>
    /// Atiende la petición GET. No necesita datos: la ficha es estática.
    /// </summary>
    public void OnGet()
    {
    }
}