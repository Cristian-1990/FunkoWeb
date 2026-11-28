namespace FunkoApp.Shared.Dtos;

/// <summary>
/// Representación pública de un Funko para las vistas.
/// Las vistas solo conocen este tipo, nunca la entidad de la base de datos.
/// </summary>
/// <param name="Id">Identificador del Funko.</param>
/// <param name="Nombre">Nombre visible.</param>
/// <param name="Anio">Año de lanzamiento.</param>
/// <param name="PrecioReferencia">Precio de referencia en euros.</param>
/// <param name="Imagen">Ruta de la imagen, ya resuelta con la de por defecto.</param>
/// <param name="Activo">Si está activo en el catálogo.</param>
/// <param name="EsNovedad">Si se muestra como novedad.</param>
/// <param name="CategoriaId">Identificador de su categoría.</param>
/// <param name="CategoriaNombre">Nombre de su categoría, para pintarlo sin otra consulta.</param>
public record FunkoDto(
    long Id,
    string Nombre,
    int Anio,
    decimal PrecioReferencia,
    string Imagen,
    bool Activo,
    bool EsNovedad,
    long CategoriaId,
    string CategoriaNombre);