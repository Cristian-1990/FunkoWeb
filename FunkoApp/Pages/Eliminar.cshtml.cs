using FunkoApp.Shared.Dtos;
using FunkoApp.Shared.Services;
using FunkoApp.Shared.Services.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FunkoApp.Pages;

/// <summary>
/// Página de eliminación de Funko. El GET enseña los datos del Funko que se va a borrar
/// y el POST, tras confirmar, lo elimina junto con su imagen. El identificador llega en la
/// ruta (<c>/Eliminar/3</c>).
/// </summary>
/// <param name="service">Servicio del catálogo.</param>
/// <param name="storage">Almacenamiento de las imágenes subidas.</param>
public class EliminarModel(IFunkoService service, IStorageService storage) : PageModel
{
    /// <summary>Funko que se va a eliminar, para que la vista enseñe sus datos.</summary>
    public FunkoDto Funko { get; private set; } = null!;

    /// <summary>
    /// Enseña los datos del Funko. Si no existe, vuelve al catálogo con un aviso.
    /// </summary>
    /// <param name="id">Identificador del Funko, tomado de la ruta.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>La propia página, o una redirección si el Funko no existe.</returns>
    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var funko = await service.GetByIdAsync(id, cancellationToken);

        if (funko is null)
        {
            TempData["Error"] = "El Funko que intentabas eliminar no existe.";
            return RedirectToPage("/Index");
        }

        Funko = funko;
        return Page();
    }

    /// <summary>
    /// Elimina el Funko tras la confirmación y, si tenía imagen propia, la borra del disco.
    /// </summary>
    /// <param name="id">Identificador del Funko, tomado de la ruta.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>Redirección a la portada con el aviso que corresponda.</returns>
    public async Task<IActionResult> OnPostAsync(long id, CancellationToken cancellationToken)
    {
        // Se lee antes de borrar: después ya no habría forma de saber cuál era su imagen.
        var funko = await service.GetByIdAsync(id, cancellationToken);
        var eliminado = await service.DeleteAsync(id, cancellationToken);

        if (eliminado && funko is not null)
        {
            // El servicio de almacenamiento solo borra ficheros de la carpeta de subidas:
            // la imagen por defecto de wwwroot/images no se toca.
            storage.DeleteFile(funko.Imagen);
        }

        TempData[eliminado ? "Exito" : "Error"] = eliminado
            ? "Funko eliminado correctamente."
            : "El Funko que intentabas eliminar ya no existe.";

        return RedirectToPage("/Index");
    }
}
