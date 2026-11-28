using FunkoApp.Shared.Dtos;
using FunkoApp.Shared.Models;

namespace FunkoApp.Shared.Mappers;

/// <summary>
/// Conversión entre la entidad <see cref="Funko"/> y su DTO.
/// Son métodos de extensión: se escribe <c>funko.ToDto()</c>.
/// </summary>
public static class FunkoMapper
{
    /// <summary>Ruta de la imagen que se usa cuando un Funko no tiene la suya.</summary>
    public const string ImagenPorDefecto = "/images/funko-sin-imagen.svg";

    /// <summary>
    /// Convierte un Funko en su DTO. Para que traiga el nombre de la categoría,
    /// la consulta debe incluir la categoría.
    /// </summary>
    /// <param name="funko">Funko a convertir.</param>
    /// <returns>El DTO del Funko.</returns>
    public static FunkoDto ToDto(this Funko funko) =>
        new(
            funko.Id,
            funko.Nombre,
            funko.Anio,
            funko.PrecioReferencia,
            string.IsNullOrWhiteSpace(funko.Imagen) ? ImagenPorDefecto : funko.Imagen,
            funko.Activo,
            funko.EsNovedad,
            funko.CategoriaId,
            funko.Categoria?.Nombre ?? string.Empty);

    /// <summary>Convierte una colección de Funkos en una lista de DTOs.</summary>
    /// <param name="funkos">Funkos a convertir.</param>
    /// <returns>Lista de DTOs.</returns>
    public static IReadOnlyList<FunkoDto> ToDtoList(this IEnumerable<Funko> funkos) =>
        funkos.Select(f => f.ToDto()).ToList();
}