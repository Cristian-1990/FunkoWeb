namespace FunkoApp.Shared.Models;

/// <summary>
/// Categoría del catálogo. Una categoría agrupa muchos Funkos.
/// </summary>
public class Category
{
    /// <summary>Identificador único de la categoría (clave primaria).</summary>
    public long Id { get; set; }

    /// <summary>Nombre de la categoría. Es el que aparece en la barra de navegación.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Funkos que pertenecen a esta categoría.</summary>
    public List<Funko> Funkos { get; set; } = [];
}