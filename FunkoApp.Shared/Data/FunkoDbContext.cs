using Microsoft.EntityFrameworkCore;
using FunkoApp.Shared.Models;

namespace FunkoApp.Shared.Data;

/// <summary>
/// Contexto de EF Core contra SQLite. Los servicios lo reciben por inyección
/// de dependencias y nunca crean uno a mano.
/// </summary>
/// <param name="options">Opciones del contexto, las monta la configuración de infraestructura.</param>
public class FunkoDbContext(DbContextOptions<FunkoDbContext> options) : DbContext(options)
{
    /// <summary>Funkos del catálogo.</summary>
    public DbSet<Funko> Funkos => Set<Funko>();

    /// <summary>Categorías del catálogo.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categorias");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nombre).HasMaxLength(100).IsRequired();
            entity.HasIndex(c => c.Nombre).IsUnique();
        });

        modelBuilder.Entity<Funko>(entity =>
        {
            entity.ToTable("funkos");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Nombre).HasMaxLength(200).IsRequired();
            entity.Property(f => f.PrecioReferencia).HasPrecision(10, 2);
            entity.Property(f => f.Imagen).HasMaxLength(500);
            entity.HasIndex(f => f.Nombre).IsUnique();

            entity.HasOne(f => f.Categoria)
                .WithMany(c => c.Funkos)
                .HasForeignKey(f => f.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}