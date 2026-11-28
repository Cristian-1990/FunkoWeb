namespace FunkoApp.Shared.Dtos;

/// <summary>
/// Representación pública de una categoría para las vistas.
/// </summary>
/// <param name="Id">Identificador de la categoría.</param>
/// <param name="Nombre">Nombre visible.</param>
public record CategoryDto(long Id, string Nombre);