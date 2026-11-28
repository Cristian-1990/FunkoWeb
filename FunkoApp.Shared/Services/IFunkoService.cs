using FunkoApp.Shared.Dtos;
using FunkoApp.Shared.Models;

namespace FunkoApp.Shared.Services;

/// <summary>
/// Catálogo de Funkos. El servicio devuelve DTOs: la vista nunca toca la entidad.
/// </summary>
public interface IFunkoService
{
    /// <summary>Devuelve los Funkos del catálogo ordenados por nombre.</summary>
    /// <param name="search">Texto que debe contener el nombre; null o vacío para no filtrar.</param>
    /// <param name="categoryId">Categoría a la que deben pertenecer; null para no filtrar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de Funkos en formato DTO.</returns>
    Task<IReadOnlyList<FunkoDto>> GetAllAsync(
        string? search = null,
        long? categoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Busca un Funko por su identificador.</summary>
    /// <param name="id">Identificador del Funko.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El Funko en formato DTO, o null si no existe.</returns>
    Task<FunkoDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Devuelve todas las categorías ordenadas por nombre.</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de categorías en formato DTO.</returns>
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Crea un Funko nuevo.</summary>
    /// <param name="funko">Funko a guardar; su Id lo asigna la base de datos.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El Funko creado en formato DTO, o null si el nombre ya existe o la categoría no existe.</returns>
    Task<FunkoDto?> CreateAsync(Funko funko, CancellationToken cancellationToken = default);}