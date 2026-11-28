using FluentAssertions;
using FunkoApp.Shared.Dtos;
using FunkoApp.Shared.Mappers;
using FunkoApp.Shared.ViewModels;
using NUnit.Framework;

namespace FunkoApp.Test.Mappers;

/// <summary>
/// Pruebas de la conversión entre el formulario, la entidad y el DTO.
/// </summary>
[TestFixture]
public class FunkoFormMapperTests
{
    [Test]
    public void ToEntity_QuitaLosEspaciosDelNombreYCopiaLosCampos()
    {
        // Arrange
        var formulario = new FunkoFormViewModel
        {
            Id = 42,
            Nombre = "  Spider-Man traje negro  ",
            Anio = 2020,
            PrecioReferencia = 13.5m,
            CategoriaId = 2,
            EsNovedad = true
        };

        // Act
        var funko = formulario.ToEntity();

        // Assert
        funko.Nombre.Should().Be("Spider-Man traje negro");
        funko.Anio.Should().Be(2020);
        funko.PrecioReferencia.Should().Be(13.5m);
        funko.CategoriaId.Should().Be(2);
        funko.EsNovedad.Should().BeTrue();
        funko.Id.Should().Be(0);
    }

    [Test]
    public void ToFormViewModel_CopiaLosCamposDelDto()
    {
        // Arrange
        var dto = new FunkoDto(8, "Piccolo", 2019, 12m, "/images/funko-sin-imagen.svg", true, false, 3, "Dragon Ball");

        // Act
        var formulario = dto.ToFormViewModel();

        // Assert
        formulario.Id.Should().Be(8);
        formulario.Nombre.Should().Be("Piccolo");
        formulario.Anio.Should().Be(2019);
        formulario.PrecioReferencia.Should().Be(12m);
        formulario.CategoriaId.Should().Be(3);
        formulario.EsNovedad.Should().BeFalse();
    }
}
