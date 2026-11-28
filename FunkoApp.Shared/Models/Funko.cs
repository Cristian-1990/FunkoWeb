namespace FunkoApp.Shared.Models;

/// <summary>
/// Figura coleccionable del catálogo. La entidad va limpia de atributos:
/// sus restricciones se declaran con Fluent API en el DbContext.
/// </summary>
public class Funko
{
    /// <summary>Identificador único del Funko (clave primaria).</summary>
    public long Id { get; set; }

    /// <summary>Nombre del Funko. No se puede repetir en el catálogo.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Año de lanzamiento de la figura.</summary>
    public int Anio { get; set; }

    /// <summary>Precio de referencia en euros.</summary>
    public decimal PrecioReferencia { get; set; }

    /// <summary>
    /// Ruta pública de la imagen subida. Si es null, la vista muestra la imagen por defecto.
    /// </summary>
    public string? Imagen { get; set; }

    /// <summary>Si el Funko está activo en el catálogo.</summary>
    public bool Activo { get; set; } = true;

    /// <summary>Si el Funko se muestra como novedad.</summary>
    public bool EsNovedad { get; set; }

    /// <summary>Identificador de la categoría a la que pertenece (clave foránea).</summary>
    public long CategoriaId { get; set; }

    /// <summary>Categoría del Funko. Solo viene cargada si la consulta la incluye.</summary>
    public Category? Categoria { get; set; }
}