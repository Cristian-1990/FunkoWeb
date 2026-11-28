using FluentAssertions;
using FunkoApp.Shared.Data;
using FunkoApp.Shared.Models;
using FunkoApp.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace FunkoApp.Test.Services;

/// <summary>
/// Pruebas del servicio de catálogo sobre una base de datos en memoria.
/// Cada prueba crea su propio contexto con un nombre único, así que no se pisan entre ellas.
/// </summary>
[TestFixture]
public class FunkoServiceTests
{
    private static FunkoDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<FunkoDbContext>()
            .UseInMemoryDatabase($"funkos-test-{Guid.NewGuid()}")
            .Options;

        var contexto = new FunkoDbContext(opciones);

        var tenistas = new Category { Nombre = "Tenistas" };
        var marvel = new Category { Nombre = "Marvel" };
        contexto.Categories.AddRange(tenistas, marvel);

        contexto.Funkos.AddRange(
            new Funko { Nombre = "Rafael Nadal", Anio = 2021, PrecioReferencia = 17.50m, Categoria = tenistas },
            new Funko { Nombre = "Roger Federer", Anio = 2021, PrecioReferencia = 17.50m, Categoria = tenistas },
            new Funko { Nombre = "Iron Man", Anio = 2019, PrecioReferencia = 12.50m, Categoria = marvel });

        contexto.SaveChanges();
        return contexto;
    }

    [TestFixture]
    public class CasosPositivos
    {
        private FunkoDbContext _contexto = null!;
        private FunkoService _servicio = null!;

        [SetUp]
        public void SetUp()
        {
            _contexto = CrearContexto();
            _servicio = new FunkoService(_contexto, NullLogger<FunkoService>.Instance);
        }

        [TearDown]
        public void TearDown() => _contexto.Dispose();

        [Test]
        public async Task GetAllAsync_SinFiltros_DevuelveTodosOrdenadosPorNombre()
        {
            // Arrange

            // Act
            var funkos = await _servicio.GetAllAsync();

            // Assert
            funkos.Select(f => f.Nombre).Should().Equal("Iron Man", "Rafael Nadal", "Roger Federer");
        }

        [Test]
        public async Task GetAllAsync_ConBusqueda_FiltraPorNombre()
        {
            // Arrange

            // Act
            var funkos = await _servicio.GetAllAsync(search: "Roger");

            // Assert
            funkos.Should().ContainSingle().Which.Nombre.Should().Be("Roger Federer");
        }

        [Test]
        public async Task GetAllAsync_ConCategoria_DevuelveSoloEsaCategoria()
        {
            // Arrange
            var marvelId = _contexto.Categories.First(c => c.Nombre == "Marvel").Id;

            // Act
            var funkos = await _servicio.GetAllAsync(categoryId: marvelId);

            // Assert
            funkos.Should().ContainSingle().Which.CategoriaNombre.Should().Be("Marvel");
        }

        [Test]
        public async Task GetByIdAsync_Existente_DevuelveElFunkoConSuCategoria()
        {
            // Arrange
            var id = _contexto.Funkos.First(f => f.Nombre == "Rafael Nadal").Id;

            // Act
            var funko = await _servicio.GetByIdAsync(id);

            // Assert
            funko.Should().NotBeNull();
            funko!.Nombre.Should().Be("Rafael Nadal");
            funko.CategoriaNombre.Should().Be("Tenistas");
        }

        [Test]
        public async Task GetCategoriesAsync_DevuelveLasCategoriasOrdenadasPorNombre()
        {
            // Arrange

            // Act
            var categorias = await _servicio.GetCategoriesAsync();

            // Assert
            categorias.Select(c => c.Nombre).Should().Equal("Marvel", "Tenistas");
        }
        [Test]
        public async Task CreateAsync_FunkoNuevo_LoGuardaYDevuelveSuDto()
        {
            // Arrange
            var marvelId = _contexto.Categories.First(c => c.Nombre == "Marvel").Id;
            var nuevo = new Funko { Nombre = "Capitán América", Anio = 2022, PrecioReferencia = 13.00m, CategoriaId = marvelId };

            // Act
            var creado = await _servicio.CreateAsync(nuevo);

            // Assert
            creado.Should().NotBeNull();
            creado!.Id.Should().BeGreaterThan(0);
            creado.Nombre.Should().Be("Capitán América");
            creado.CategoriaNombre.Should().Be("Marvel");
            _contexto.Funkos.Count().Should().Be(4);
        }

        [Test]
        public async Task GetAllAsync_BusquedaEnMinusculas_EncuentraElNombreIgualmente()
        {
            // Arrange

            // Act
            var funkos = await _servicio.GetAllAsync(search: "rafael");

            // Assert
            funkos.Should().ContainSingle().Which.Nombre.Should().Be("Rafael Nadal");
        }
    }

    [TestFixture]
    public class CasosNegativos
    {
        private FunkoDbContext _contexto = null!;
        private FunkoService _servicio = null!;

        [SetUp]
        public void SetUp()
        {
            _contexto = CrearContexto();
            _servicio = new FunkoService(_contexto, NullLogger<FunkoService>.Instance);
        }

        [TearDown]
        public void TearDown() => _contexto.Dispose();

        [Test]
        public async Task GetByIdAsync_Inexistente_DevuelveNull()
        {
            // Arrange

            // Act
            var funko = await _servicio.GetByIdAsync(9999);

            // Assert
            funko.Should().BeNull();
        }

        [Test]
        public async Task GetAllAsync_BusquedaSinCoincidencias_DevuelveListaVacia()
        {
            // Arrange

            // Act
            var funkos = await _servicio.GetAllAsync(search: "Pikachu");

            // Assert
            funkos.Should().BeEmpty();
        }

        [Test]
        public async Task CreateAsync_NombreRepetido_DevuelveNullYNoGuarda()
        {
            // Arrange
            var tenistasId = _contexto.Categories.First(c => c.Nombre == "Tenistas").Id;
            var repetido = new Funko { Nombre = "Rafael Nadal", Anio = 2023, PrecioReferencia = 20m, CategoriaId = tenistasId };

            // Act
            var creado = await _servicio.CreateAsync(repetido);

            // Assert
            creado.Should().BeNull();
            _contexto.Funkos.Count().Should().Be(3);
        }

        [Test]
        public async Task CreateAsync_CategoriaInexistente_DevuelveNullYNoGuarda()
        {
            // Arrange
            var huerfano = new Funko { Nombre = "Sin categoría", Anio = 2023, PrecioReferencia = 20m, CategoriaId = 9999 };

            // Act
            var creado = await _servicio.CreateAsync(huerfano);

            // Assert
            creado.Should().BeNull();
            _contexto.Funkos.Count().Should().Be(3);
        }

        [Test]
        public async Task GetAllAsync_CategoriaInexistente_DevuelveListaVacia()
        {
            // Arrange

            // Act
            var funkos = await _servicio.GetAllAsync(categoryId: 9999);

            // Assert
            funkos.Should().BeEmpty();
        }
    }
}