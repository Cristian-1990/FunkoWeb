using FluentAssertions;
using FunkoApp.Pages;
using FunkoApp.Shared.Dtos;
using FunkoApp.Shared.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace FunkoApp.Test.Pages;

/// <summary>
/// Pruebas de la portada. El servicio es un Mock.
/// </summary>
[TestFixture]
public class IndexModelTests
{
    private Mock<IFunkoService> _servicio = null!;
    private IndexModel _pagina = null!;

    [SetUp]
    public void SetUp()
    {
        _servicio = new Mock<IFunkoService>();
        _pagina = new IndexModel(_servicio.Object, NullLogger<IndexModel>.Instance).ConContexto();
    }

    [TestFixture]
    public class CasosPositivos : IndexModelTests
    {
        [Test]
        public async Task OnGetAsync_ConBusqueda_PideAlServicioLosFunkosFiltradosYConservaElTexto()
        {
            // Arrange
            FunkoDto[] encontrados = [new(1, "Rafael Nadal", 2021, 17.5m, "/x.svg", true, false, 1, "Tenistas")];
            _servicio.Setup(s => s.GetAllAsync("nadal", null, It.IsAny<CancellationToken>())).ReturnsAsync(encontrados);

            // Act
            await _pagina.OnGetAsync("nadal");

            // Assert
            _pagina.Funkos.Should().ContainSingle().Which.Nombre.Should().Be("Rafael Nadal");
            _pagina.Busqueda.Should().Be("nadal");
            _servicio.Verify(s => s.GetAllAsync("nadal", null, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [TestFixture]
    public class CasosNegativos : IndexModelTests
    {
        [Test]
        public async Task OnGetAsync_SinCoincidencias_DejaLaListaVacia()
        {
            // Arrange
            _servicio.Setup(s => s.GetAllAsync("pikachu", null, It.IsAny<CancellationToken>())).ReturnsAsync([]);

            // Act
            await _pagina.OnGetAsync("pikachu");

            // Assert
            _pagina.Funkos.Should().BeEmpty();
            _pagina.Busqueda.Should().Be("pikachu");
        }
    }
}
