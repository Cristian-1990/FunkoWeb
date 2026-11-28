using FunkoApp.Shared.Dtos;
using FunkoApp.Shared.Models;
using FunkoApp.Shared.ViewModels;

namespace FunkoApp.Shared.Mappers;

/// <summary>
/// Conversión entre el formulario de Funko, su entidad y su DTO.
/// Son métodos de extensión, como los de <see cref="FunkoMapper"/>.
/// </summary>
public static class FunkoFormMapper
{
    /// <summary>
    /// Pasa del formulario a la entidad que guarda el servicio. Quita los espacios sobrantes del nombre.
    /// </summary>
    /// <param name="formulario">Formulario ya validado.</param>
    /// <returns>Entidad lista para guardar.</returns>
    public static Funko ToEntity(this FunkoFormViewModel formulario) =>
        new()
        {
            Nombre = formulario.Nombre.Trim(),
            Anio = formulario.Anio,
            PrecioReferencia = formulario.PrecioReferencia,
            CategoriaId = formulario.CategoriaId,
            EsNovedad = formulario.EsNovedad
        };

    /// <summary>
    /// Pasa de un Funko ya guardado al formulario que se precarga al editar.
    /// </summary>
    /// <param name="funko">Funko en formato DTO.</param>
    /// <returns>Formulario relleno con sus datos.</returns>
    public static FunkoFormViewModel ToFormViewModel(this FunkoDto funko) =>
        new()
        {
            Id = funko.Id,
            Nombre = funko.Nombre,
            Anio = funko.Anio,
            PrecioReferencia = funko.PrecioReferencia,
            CategoriaId = funko.CategoriaId,
            EsNovedad = funko.EsNovedad
        };
}
