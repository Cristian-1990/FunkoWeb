using FluentAssertions;
using FunkoApp.Pages;
using FunkoApp.Shared.Dtos;
using FunkoApp.Shared.Services;
using FunkoApp.Shared.Services.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using NUnit.Framework;

namespace FunkoApp.Test.Pages;

/// <summary>
/// Pruebas de la página de eliminación. El servicio y el almacenamiento son Mocks: se comprueba,
/// sobre todo, que no se borra nada hasta que llega el POST de confirmación y que la imagen
/// solo se borra si el Funko se eliminó de verdad.
/// </summary>
[TestFixture]
public class EliminarModelTests
{
    private Mock<IFunkoService> _servicio = null!;
    private Mock<IStorageService> _almacen = null!;
    private EliminarModel _pagina = null!;

    private const string RutaImagen = "/uploads/funkos/vegeta.png";

    private static readonly FunkoDto Existente =
        new(5, "Vegeta armadura Saiyan", 2021, 16m, RutaImagen, true, false, 3, "Dragon Ball");

    [SetUp]
    public void SetUp()
    {
        _servicio = new Mock<IFunkoService>();
        _almacen = new Mock<IStorageService>();
        _pagina = new EliminarModel(_servicio.Object, _almacen.Object).ConContexto();
    }

    [TestFixture]
    public class CasosPositivos : EliminarModelTests
    {
        [Test]
        public async Task OnGetAsync_FunkoExistente_EnseñaSusDatosSinBorrarNada()
        {
            // Arrange
            _servicio.Setup(s => s.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(Existente);

            // Act
            var resultado = await _pagina.OnGetAsync(5, CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<PageResult>();
            _pagina.Funko.Nombre.Should().Be("Vegeta armadura Saiyan");
            _servicio.Verify(s => s.DeleteAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
            _almacen.Verify(a => a.DeleteFile(It.IsAny<string?>()), Times.Never);
        }

        [Test]
        public async Task OnPostAsync_Existente_ElimináYBorraSuImagen()
        {
            // Arrange
            _servicio.Setup(s => s.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(Existente);
            _servicio.Setup(s => s.DeleteAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // Act
            var resultado = await _pagina.OnPostAsync(5, CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Index");
            _pagina.TempData.ContainsKey("Exito").Should().BeTrue();
            _pagina.TempData.ContainsKey("Error").Should().BeFalse();
            _servicio.Verify(s => s.DeleteAsync(5, It.IsAny<CancellationToken>()), Times.Once);
            _almacen.Verify(a => a.DeleteFile(RutaImagen), Times.Once);
        }
    }

    [TestFixture]
    public class CasosNegativos : EliminarModelTests
    {
        [Test]
        public async Task OnGetAsync_FunkoInexistente_RedirigeALaPortadaConError()
        {
            // Arrange
            _servicio.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((FunkoDto?)null);

            // Act
            var resultado = await _pagina.OnGetAsync(99, CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Index");
            _pagina.TempData.ContainsKey("Error").Should().BeTrue();
        }

        [Test]
        public async Task OnPostAsync_Inexistente_AvisaDeErrorYNoBorraNingunFichero()
        {
            // Arrange
            _servicio.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((FunkoDto?)null);
            _servicio.Setup(s => s.DeleteAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            // Act
            var resultado = await _pagina.OnPostAsync(99, CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<RedirectToPageResult>();
            _pagina.TempData.ContainsKey("Error").Should().BeTrue();
            _pagina.TempData.ContainsKey("Exito").Should().BeFalse();
            _almacen.Verify(a => a.DeleteFile(It.IsAny<string?>()), Times.Never);
        }

        [Test]
        public async Task OnPostAsync_ElServicioNoLoElimina_ConservaLaImagen()
        {
            // Arrange
            _servicio.Setup(s => s.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(Existente);
            _servicio.Setup(s => s.DeleteAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            // Act
            await _pagina.OnPostAsync(5, CancellationToken.None);

            // Assert
            _almacen.Verify(a => a.DeleteFile(It.IsAny<string?>()), Times.Never);
        }
    }
}
