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
/// Página de edición de Funko. El identificador llega en la ruta (<c>/Editar/3</c>):
/// el GET precarga el formulario con él y el POST lo usa para saber qué Funko actualizar.
/// Si se sube una imagen nueva, la anterior se borra del disco.
/// </summary>
/// <param name="service">Servicio del catálogo.</param>
/// <param name="storage">Almacenamiento de las imágenes subidas.</param>
public class EditarModel(IFunkoService service, IStorageService storage) : PageModel
{
    /// <summary>Datos del formulario; la vista los pinta con asp-for.</summary>
    [BindProperty]
    public FunkoFormViewModel Formulario { get; set; } = new();

    /// <summary>Ruta de la imagen que tiene ahora el Funko, para enseñarla junto al campo de subida.</summary>
    public string? ImagenActual { get; private set; }

    /// <summary>
    /// Precarga el formulario con el Funko de la ruta. Si no existe, vuelve al catálogo con un aviso.
    /// </summary>
    /// <param name="id">Identificador del Funko a editar, tomado de la ruta.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>La propia página, o una redirección si el Funko no existe.</returns>
    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var funko = await service.GetByIdAsync(id, cancellationToken);

        if (funko is null)
        {
            TempData["Error"] = "El Funko que intentabas editar no existe.";
            return RedirectToPage("/Index");
        }

        Formulario = funko.ToFormViewModel();
        ImagenActual = funko.Imagen;
        await CargarCategoriasAsync(cancellationToken);
        return Page();
    }

    /// <summary>
    /// Recibe la edición. Manda el identificador de la ruta, no el del formulario.
    /// </summary>
    /// <param name="id">Identificador del Funko a actualizar, tomado de la ruta.</param>
    /// <param name="cancellationToken">Token de cancelación de la petición.</param>
    /// <returns>La propia página con los errores si los hay; redirección a la portada si se actualizó.</returns>
    public async Task<IActionResult> OnPostAsync(long id, CancellationToken cancellationToken)
    {
        // Se lee el Funko antes de tocar nada: hace falta saber cuál era su imagen para poder borrarla.
        var actual = await service.GetByIdAsync(id, cancellationToken);

        if (actual is null)
        {
            TempData["Error"] = "El Funko que intentabas editar ya no existe.";
            return RedirectToPage("/Index");
        }

        ImagenActual = actual.Imagen;

        if (!ModelState.IsValid)
        {
            await CargarCategoriasAsync(cancellationToken);
            return Page();
        }

        var cambios = Formulario.ToEntity();
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
            cambios.Imagen = rutaNueva;
        }

        var actualizado = await service.UpdateAsync(id, cambios, cancellationToken);

        if (actualizado is null)
        {
            // No se guardó nada: la imagen recién subida quedaría huérfana, así que se borra.
            storage.DeleteFile(rutaNueva);

            // El servicio devuelve null si otro Funko usa ese nombre o si la categoría no existe.
            ModelState.AddModelError(string.Empty, "No se han podido guardar los cambios: otro Funko usa ese nombre o la categoría no es válida.");
            await CargarCategoriasAsync(cancellationToken);
            return Page();
        }

        // Los cambios están guardados: ahora sí se borra la imagen vieja, y solo si se ha reemplazado.
        if (rutaNueva is not null)
        {
            storage.DeleteFile(actual.Imagen);
        }

        TempData["Exito"] = $"Funko «{actualizado.Nombre}» actualizado correctamente.";
        return RedirectToPage("/Index");
    }

    private async Task CargarCategoriasAsync(CancellationToken cancellationToken)
    {
        var categorias = await service.GetCategoriesAsync(cancellationToken);
        ViewData["Categorias"] = new SelectList(categorias, nameof(CategoryDto.Id), nameof(CategoryDto.Nombre));
    }
}
