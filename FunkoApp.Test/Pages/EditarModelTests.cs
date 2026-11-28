using FluentAssertions;
using FunkoApp.Pages;
using FunkoApp.Shared.Dtos;
using FunkoApp.Shared.Models;
using FunkoApp.Shared.Services;
using FunkoApp.Shared.Services.Storage;
using FunkoApp.Shared.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using NUnit.Framework;

namespace FunkoApp.Test.Pages;

/// <summary>
/// Pruebas de la página de edición. El servicio y el almacenamiento son Mocks: se comprueba,
/// sobre todo, cuándo se borra la imagen vieja y cuándo no.
/// </summary>
[TestFixture]
public class EditarModelTests
{
    private Mock<IFunkoService> _servicio = null!;
    private Mock<IStorageService> _almacen = null!;
    private EditarModel _pagina = null!;

    private const string RutaVieja = "/uploads/funkos/vieja.png";
    private const string RutaNueva = "/uploads/funkos/nueva.png";

    private static readonly FunkoDto Existente =
        new(3, "Goku Super Saiyan", 2022, 14m, RutaVieja, true, true, 2, "Dragon Ball");

    [SetUp]
    public void SetUp()
    {
        _servicio = new Mock<IFunkoService>();
        _servicio.Setup(s => s.GetCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new CategoryDto(2, "Dragon Ball")]);
        _servicio.Setup(s => s.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(Existente);
        _almacen = new Mock<IStorageService>();
        _pagina = new EditarModel(_servicio.Object, _almacen.Object).ConContexto();
    }

    private static FormFile Foto() => new(new MemoryStream([1, 2, 3]), 0, 3, "ImagenFile", "foto.png");

    private static FunkoFormViewModel Cambios(IFormFile? imagen = null) =>
        new() { Id = 999, Nombre = " Goku Ultra Instinto ", Anio = 2023, PrecioReferencia = 20m, CategoriaId = 2, ImagenFile = imagen };

    [TestFixture]
    public class CasosPositivos : EditarModelTests
    {
        [Test]
        public async Task OnGetAsync_FunkoExistente_PrecargaElFormularioYLaImagenActual()
        {
            // Arrange

            // Act
            var resultado = await _pagina.OnGetAsync(3, CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<PageResult>();
            _pagina.Formulario.Nombre.Should().Be("Goku Super Saiyan");
            _pagina.Formulario.CategoriaId.Should().Be(2);
            _pagina.Formulario.EsNovedad.Should().BeTrue();
            _pagina.ImagenActual.Should().Be(RutaVieja);
        }

        [Test]
        public async Task OnPostAsync_SinImagenNueva_ActualizaConservandoLaImagenYSinTocarElAlmacen()
        {
            // Arrange
            _pagina.Formulario = Cambios();
            _servicio.Setup(s => s.UpdateAsync(3, It.IsAny<Funko>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Existente with { Nombre = "Goku Ultra Instinto" });

            // Act
            var resultado = await _pagina.OnPostAsync(3, CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Index");
            _pagina.TempData["Exito"].Should().BeOfType<string>().Which.Should().Contain("Goku Ultra Instinto");
            _servicio.Verify(s => s.UpdateAsync(3, It.Is<Funko>(f => f.Nombre == "Goku Ultra Instinto" && f.Imagen == null), It.IsAny<CancellationToken>()), Times.Once);
            _servicio.Verify(s => s.UpdateAsync(999, It.IsAny<Funko>(), It.IsAny<CancellationToken>()), Times.Never);
            _almacen.Verify(a => a.SaveFileAsync(It.IsAny<IFormFile?>(), It.IsAny<string>()), Times.Never);
            _almacen.Verify(a => a.DeleteFile(It.IsAny<string?>()), Times.Never);
        }

        [Test]
        public async Task OnPostAsync_ConImagenNueva_LaGuardaYBorraLaVieja()
        {
            // Arrange
            _pagina.Formulario = Cambios(Foto());
            _almacen.Setup(a => a.SaveFileAsync(It.IsAny<IFormFile?>(), StorageFolders.Funkos))
                .ReturnsAsync(StorageResult.Exito(RutaNueva));
            _servicio.Setup(s => s.UpdateAsync(3, It.IsAny<Funko>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Existente with { Imagen = RutaNueva });

            // Act
            var resultado = await _pagina.OnPostAsync(3, CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<RedirectToPageResult>();
            _servicio.Verify(s => s.UpdateAsync(3, It.Is<Funko>(f => f.Imagen == RutaNueva), It.IsAny<CancellationToken>()), Times.Once);
            _almacen.Verify(a => a.DeleteFile(RutaVieja), Times.Once);
            _almacen.Verify(a => a.DeleteFile(RutaNueva), Times.Never);
        }
    }

    [TestFixture]
    public class CasosNegativos : EditarModelTests
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
            _servicio.Verify(s => s.GetCategoriesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task OnPostAsync_FunkoInexistente_RedirigeConErrorSinGuardarNada()
        {
            // Arrange
            _pagina.Formulario = Cambios(Foto());
            _servicio.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((FunkoDto?)null);

            // Act
            var resultado = await _pagina.OnPostAsync(99, CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<RedirectToPageResult>();
            _pagina.TempData.ContainsKey("Error").Should().BeTrue();
            _almacen.Verify(a => a.SaveFileAsync(It.IsAny<IFormFile?>(), It.IsAny<string>()), Times.Never);
            _servicio.Verify(s => s.UpdateAsync(It.IsAny<long>(), It.IsAny<Funko>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task OnPostAsync_ModeloInvalido_VuelveALaPaginaSinGuardarNiBorrarNada()
        {
            // Arrange
            _pagina.Formulario = Cambios(Foto());
            _pagina.ModelState.AddModelError("Formulario.Anio", "El año debe estar entre 1998 y 2100");

            // Act
            var resultado = await _pagina.OnPostAsync(3, CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<PageResult>();
            _pagina.ImagenActual.Should().Be(RutaVieja);
            _almacen.Verify(a => a.SaveFileAsync(It.IsAny<IFormFile?>(), It.IsAny<string>()), Times.Never);
            _almacen.Verify(a => a.DeleteFile(It.IsAny<string?>()), Times.Never);
            _servicio.Verify(s => s.UpdateAsync(It.IsAny<long>(), It.IsAny<Funko>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task OnPostAsync_ImagenRechazada_MuestraElMotivoSinActualizarNiBorrarLaVieja()
        {
            // Arrange
            _pagina.Formulario = Cambios(Foto());
            _almacen.Setup(a => a.SaveFileAsync(It.IsAny<IFormFile?>(), It.IsAny<string>()))
                .ReturnsAsync(StorageResult.Fallo("La extensión .exe no está permitida"));

            // Act
            var resultado = await _pagina.OnPostAsync(3, CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<PageResult>();
            _pagina.ModelState["Formulario.ImagenFile"]!.Errors.Should().ContainSingle();
            _servicio.Verify(s => s.UpdateAsync(It.IsAny<long>(), It.IsAny<Funko>(), It.IsAny<CancellationToken>()), Times.Never);
            _almacen.Verify(a => a.DeleteFile(It.IsAny<string?>()), Times.Never);
        }

        [Test]
        public async Task OnPostAsync_ServicioDevuelveNullConImagenNueva_BorraLaNuevaYConservaLaVieja()
        {
            // Arrange
            _pagina.Formulario = Cambios(Foto());
            _almacen.Setup(a => a.SaveFileAsync(It.IsAny<IFormFile?>(), It.IsAny<string>()))
                .ReturnsAsync(StorageResult.Exito(RutaNueva));
            _servicio.Setup(s => s.UpdateAsync(3, It.IsAny<Funko>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FunkoDto?)null);

            // Act
            var resultado = await _pagina.OnPostAsync(3, CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<PageResult>();
            _pagina.ModelState[string.Empty]!.Errors.Should().ContainSingle();
            _pagina.TempData.ContainsKey("Exito").Should().BeFalse();
            _almacen.Verify(a => a.DeleteFile(RutaNueva), Times.Once);
            _almacen.Verify(a => a.DeleteFile(RutaVieja), Times.Never);
        }

        [Test]
        public async Task OnPostAsync_ServicioDevuelveNullSinImagenNueva_NoBorraNada()
        {
            // Arrange
            _pagina.Formulario = Cambios();
            _servicio.Setup(s => s.UpdateAsync(3, It.IsAny<Funko>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FunkoDto?)null);

            // Act
            var resultado = await _pagina.OnPostAsync(3, CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<PageResult>();
            _almacen.Verify(a => a.DeleteFile(It.Is<string?>(r => r != null)), Times.Never);
        }
    }
}
