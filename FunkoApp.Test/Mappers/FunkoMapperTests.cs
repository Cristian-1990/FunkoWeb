using FluentAssertions;
using FunkoApp.Shared.Mappers;
using FunkoApp.Shared.Models;
using NUnit.Framework;

namespace FunkoApp.Test.Mappers;

/// <summary>
/// Pruebas del mapper entre la entidad Funko y su DTO.
/// </summary>
[TestFixture]
public class FunkoMapperTests
{
    [TestFixture]
    public class CasosPositivos
    {
        [Test]
        public void ToDto_ConImagenPropia_ConservaLaImagen()
        {
            // Arrange
            var funko = new Funko { Id = 1, Nombre = "Rafael Nadal", Imagen = "/uploads/nadal.jpg" };

            // Act
            var dto = funko.ToDto();

            // Assert
            dto.Imagen.Should().Be("/uploads/nadal.jpg");
        }

        [Test]
        public void ToDto_ConCategoriaCargada_CopiaElNombreDeLaCategoria()
        {
            // Arrange
            var funko = new Funko
            {
                Id = 2,
                Nombre = "Goku Super Saiyan",
                CategoriaId = 7,
                Categoria = new Category { Id = 7, Nombre = "Dragon Ball" }
            };

            // Act
            var dto = funko.ToDto();

            // Assert
            dto.CategoriaId.Should().Be(7);
            dto.CategoriaNombre.Should().Be("Dragon Ball");
        }

        [Test]
        public void ToDtoList_VariosFunkos_DevuelveUnDtoPorFunko()
        {
            // Arrange
            var funkos = new[]
            {
                new Funko { Id = 1, Nombre = "Piccolo" },
                new Funko { Id = 2, Nombre = "Vegeta" }
            };

            // Act
            var dtos = funkos.ToDtoList();

            // Assert
            dtos.Select(d => d.Nombre).Should().Equal("Piccolo", "Vegeta");
        }
    }

    [TestFixture]
    public class CasosNegativos
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ToDto_SinImagen_UsaLaImagenPorDefecto(string? imagen)
        {
            // Arrange
            var funko = new Funko { Id = 3, Nombre = "Björn Borg", Imagen = imagen };

            // Act
            var dto = funko.ToDto();

            // Assert
            dto.Imagen.Should().Be(FunkoMapper.ImagenPorDefecto);
        }

        [Test]
        public void ToDto_SinCategoriaCargada_DevuelveNombreDeCategoriaVacio()
        {
            // Arrange
            var funko = new Funko { Id = 4, Nombre = "Nick Kyrgios", CategoriaId = 1, Categoria = null };

            // Act
            var dto = funko.ToDto();

            // Assert
            dto.CategoriaNombre.Should().BeEmpty();
        }

        [Test]
        public void ToDtoList_ListaVacia_DevuelveListaVacia()
        {
            // Arrange
            var funkos = Array.Empty<Funko>();

            // Act
            var dtos = funkos.ToDtoList();

            // Assert
            dtos.Should().BeEmpty();
        }
    }
}