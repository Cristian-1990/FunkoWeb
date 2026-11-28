using FunkoApp.Shared.Dtos;
using FunkoApp.Shared.Mappers;
using FunkoApp.Shared.Services;
using FunkoApp.Shared.Services.Storage;
using FunkoApp.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FunkoApp.Pages;

/// <summary>
/// Página de alta de Funko.
/// Sigue el patrón Post-Redirect-Get: si el POST va bien, redirige a la portada
/// con el aviso de éxito en TempData; si no, vuelve a pintar el formulario con sus errores.
/// </summary>
/// <param name="service">Servicio del catálogo.</param>
/// <param name="storage">Almacenamiento de las imágenes subidas.</param>
public class CrearModel(IFunkoService service, IStorageService storage) : PageModel
{
    /// <summary>Datos del formulario; la vista los pinta con asp-for.</summary>
    [BindProperty]
    public FunkoFormViewModel Formulario { get; set; } = new();

    /// <summary>
    /// Primera visita: el formulario llega vacío y solo hay que cargar las categorías.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await CargarCategoriasAsync(cancellationToken);
    }

    /// <summary>
    /// Recibe el alta. El token antifalsificación lo exige Razor Pages por defecto en los POST.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>La propia página con los errores si los hay; redirección a la portada si se creó.</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        // Las DataAnnotations ya se ejecutaron solas al enlazar el modelo con [BindProperty].
        if (!ModelState.IsValid)
        {
            await CargarCategoriasAsync(cancellationToken);
            return Page();
        }

        var funko = Formulario.ToEntity();
        string? rutaNueva = null;

        if (Formulario.ImagenFile is not null)
        {
            var guardado = await storage.SaveFileAsync(Formulario.ImagenFile, StorageFolders.Funkos);

            if (!guardado.Ok)
            {
                ModelState.AddModelError($"{nameof(Formulario)}.{nameof(Formulario.ImagenFile)}", guardado.Mensaje);
                await CargarCategoriasAsync(cancellationToken);
                return Page();
            }

            rutaNueva = guardado.Ruta;
            funko.Imagen = rutaNueva;
        }

        var creado = await service.CreateAsync(funko, cancellationToken);

        if (creado is null)
        {
            // El Funko no se guardó: la imagen recién subida quedaría huérfana, así que se borra.
            storage.DeleteFile(rutaNueva);

            // El servicio devuelve null si el nombre ya existe o la categoría no existe.
            ModelState.AddModelError(string.Empty, "No se ha podido crear el Funko: ya existe uno con ese nombre o la categoría no es válida.");
            await CargarCategoriasAsync(cancellationToken);
            return Page();
        }

        TempData["Exito"] = $"Funko «{creado.Nombre}» creado correctamente.";
        return RedirectToPage("/Index");
    }

    private async Task CargarCategoriasAsync(CancellationToken cancellationToken)
    {
        var categorias = await service.GetCategoriesAsync(cancellationToken);
        ViewData["Categorias"] = new SelectList(categorias, nameof(CategoryDto.Id), nameof(CategoryDto.Nombre));
    }
}
