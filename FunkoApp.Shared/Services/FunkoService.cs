using Microsoft.EntityFrameworkCore;
using FunkoApp.Shared.Data;
using FunkoApp.Shared.Dtos;
using FunkoApp.Shared.Mappers;
using FunkoApp.Shared.Models;

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
            consulta = consulta.Where(f => EF.Functions.Like(f.Nombre, $"%{texto}%"));
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

    /// <inheritdoc />
    public async Task<FunkoDto?> CreateAsync(Funko funko, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Creando el Funko {Nombre}", funko.Nombre);

        // Comprobación previa: el índice único es la red de seguridad de la base de datos,
        // pero avisar aquí permite devolver null sin llegar a lanzar la excepción.
        var yaExiste = await context.Funkos
            .AnyAsync(f => f.Nombre == funko.Nombre, cancellationToken);

        if (yaExiste)
        {
            logger.LogWarning("Ya existe un Funko llamado {Nombre}", funko.Nombre);
            return null;
        }

        var categoria = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == funko.CategoriaId, cancellationToken);

        if (categoria is null)
        {
            logger.LogWarning("No existe la categoría {CategoriaId} del Funko {Nombre}", funko.CategoriaId, funko.Nombre);
            return null;
        }

        try
        {
            funko.Categoria = categoria;
            context.Funkos.Add(funko);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Caso de dos peticiones simultáneas con el mismo nombre: gana una, la otra recibe null.
            logger.LogWarning("No se pudo crear {Nombre}: el nombre ya existe en la base de datos", funko.Nombre);
            context.Entry(funko).State = EntityState.Detached;
            return null;
        }

        return funko.ToDto();
    }
    /// <inheritdoc />
    public async Task<FunkoDto?> UpdateAsync(long id, Funko funko, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Actualizando el Funko {Id}", id);

        var existente = await context.Funkos
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (existente is null)
        {
            logger.LogWarning("No existe el Funko {Id} que se quería actualizar", id);
            return null;
        }

        // Otro Funko distinto no puede llamarse igual; el mismo Funko sí puede conservar su nombre.
        var nombreEnUso = await context.Funkos
            .AnyAsync(f => f.Nombre == funko.Nombre && f.Id != id, cancellationToken);

        if (nombreEnUso)
        {
            logger.LogWarning("Ya existe otro Funko llamado {Nombre}", funko.Nombre);
            return null;
        }

        var categoria = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == funko.CategoriaId, cancellationToken);

        if (categoria is null)
        {
            logger.LogWarning("No existe la categoría {CategoriaId} del Funko {Id}", funko.CategoriaId, id);
            return null;
        }

        // El Id no se toca: manda el de la ruta. La imagen solo cambia si llega una nueva.
        existente.Nombre = funko.Nombre;
        existente.Anio = funko.Anio;
        existente.PrecioReferencia = funko.PrecioReferencia;
        existente.EsNovedad = funko.EsNovedad;
        existente.Categoria = categoria;

        if (funko.Imagen is not null)
        {
            existente.Imagen = funko.Imagen;
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Caso de dos peticiones simultáneas con el mismo nombre: gana una, la otra recibe null.
            logger.LogWarning("No se pudo actualizar el Funko {Id}: el nombre ya existe en la base de datos", id);
            context.Entry(existente).State = EntityState.Detached;
            return null;
        }

        return existente.ToDto();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Eliminando el Funko {Id}", id);

        var existente = await context.Funkos
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (existente is null)
        {
            logger.LogWarning("No existe el Funko {Id} que se quería eliminar", id);
            return false;
        }

        context.Funkos.Remove(existente);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }}