using Microsoft.EntityFrameworkCore;
using FunkoApp.Shared.Data;
using FunkoApp.Shared.Dtos;
using FunkoApp.Shared.Mappers;

namespace FunkoApp.Shared.Services;

/// <summary>
/// Implementación del catálogo sobre SQLite con EF Core.
/// </summary>
/// <param name="context">Contexto de la base de datos.</param>
/// <param name="logger">Logger del servicio.</param>
public class FunkoService(FunkoDbContext context, ILogger<FunkoService> logger) : IFunkoService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<FunkoDto>> GetAllAsync(
        string? search = null,
        long? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Leyendo Funkos (búsqueda: {Search}, categoría: {CategoryId})", search, categoryId);

        var consulta = context.Funkos
            .AsNoTracking()
            .Include(f => f.Categoria)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var texto = search.Trim();
            consulta = consulta.Where(f => f.Nombre.Contains(texto));
        }

        if (categoryId is not null)
        {
            consulta = consulta.Where(f => f.CategoriaId == categoryId);
        }

        var funkos = await consulta
            .OrderBy(f => f.Nombre)
            .ToListAsync(cancellationToken);

        return funkos.ToDtoList();
    }

    /// <inheritdoc />
    public async Task<FunkoDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Leyendo el Funko {Id}", id);

        var funko = await context.Funkos
            .AsNoTracking()
            .Include(f => f.Categoria)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        return funko?.ToDto();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Leyendo las categorías");

        return await context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoryDto(c.Id, c.Nombre))
            .ToListAsync(cancellationToken);
    }
}