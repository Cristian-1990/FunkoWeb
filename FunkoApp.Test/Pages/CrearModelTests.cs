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
using Microsoft.AspNetCore.Mvc.Rendering;
using Moq;
using NUnit.Framework;

namespace FunkoApp.Test.Pages;

/// <summary>
/// Pruebas de la página de alta. El servicio y el almacenamiento son Mocks: se comprueba qué devuelve
/// la página y, sobre todo, a quién llama y a quién no.
/// </summary>
[TestFixture]
public class CrearModelTests
{
    private Mock<IFunkoService> _servicio = null!;
    private Mock<IStorageService> _almacen = null!;
    private CrearModel _pagina = null!;

    private static readonly IReadOnlyList<CategoryDto> Categorias =
        [new CategoryDto(1, "Tenistas"), new CategoryDto(2, "Marvel")];

    private const string RutaImagen = "/uploads/funkos/nueva.png";

    [SetUp]
    public void SetUp()
    {
        _servicio = new Mock<IFunkoService>();
        _servicio.Setup(s => s.GetCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Categorias);
        _almacen = new Mock<IStorageService>();
        _pagina = new CrearModel(_servicio.Object, _almacen.Object).ConContexto();
    }

    private static FunkoDto Dto(string nombre) =>
        new(7, nombre, 2023, 15m, "/images/funko-sin-imagen.svg", true, false, 2, "Marvel");

    private static FormFile Foto() => new(new MemoryStream([1, 2, 3]), 0, 3, "ImagenFile", "foto.png");

    private static FunkoFormViewModel FormularioValido(IFormFile? imagen = null) =>
        new() { Nombre = "  Capitán América  ", Anio = 2023, PrecioReferencia = 15m, CategoriaId = 2, ImagenFile = imagen };

    [TestFixture]
    public class CasosPositivos : CrearModelTests
    {
        [Test]
        public async Task OnGetAsync_CargaLasCategoriasParaElDesplegable()
        {
            // Arrange

            // Act
            await _pagina.OnGetAsync(CancellationToken.None);

            // Assert
            var lista = _pagina.ViewData["Categorias"].Should().BeOfType<SelectList>().Subject;
            lista.Select(i => i.Text).Should().Equal("Tenistas", "Marvel");
        }

        [Test]
        public async Task OnPostAsync_ValidoSinImagen_CreaSinTocarElAlmacen()
        {
            // Arrange
            _pagina.Formulario = FormularioValido();
            _servicio.Setup(s => s.CreateAsync(It.IsAny<Funko>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Dto("Capitán América"));

            // Act
            var resultado = await _pagina.OnPostAsync(CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<RedirectToPageResult>().Which.PageName.Should().Be("/Index");
            _pagina.TempData["Exito"].Should().BeOfType<string>().Which.Should().Contain("Capitán América");
            _servicio.Verify(s => s.CreateAsync(It.Is<Funko>(f => f.Nombre == "Capitán América" && f.Imagen == null), It.IsAny<CancellationToken>()), Times.Once);
            _almacen.Verify(a => a.SaveFileAsync(It.IsAny<IFormFile?>(), It.IsAny<string>()), Times.Never);
            _almacen.Verify(a => a.DeleteFile(It.IsAny<string?>()), Times.Never);
        }

        [Test]
        public async Task OnPostAsync_ValidoConImagen_LaGuardaYPasaSuRutaAlServicio()
        {
            // Arrange
            _pagina.Formulario = FormularioValido(Foto());
            _almacen.Setup(a => a.SaveFileAsync(It.IsAny<IFormFile?>(), StorageFolders.Funkos))
                .ReturnsAsync(StorageResult.Exito(RutaImagen));
            _servicio.Setup(s => s.CreateAsync(It.IsAny<Funko>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Dto("Capitán América"));

            // Act
            var resultado = await _pagina.OnPostAsync(CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<RedirectToPageResult>();
            _almacen.Verify(a => a.SaveFileAsync(It.IsAny<IFormFile?>(), StorageFolders.Funkos), Times.Once);
            _servicio.Verify(s => s.CreateAsync(It.Is<Funko>(f => f.Imagen == RutaImagen), It.IsAny<CancellationToken>()), Times.Once);
            _almacen.Verify(a => a.DeleteFile(It.IsAny<string?>()), Times.Never);
        }
    }

    [TestFixture]
    public class CasosNegativos : CrearModelTests
    {
        [Test]
        public async Task OnPostAsync_ModeloInvalido_VuelveALaPaginaSinGuardarNiLlamarAlServicio()
        {
            // Arrange
            _pagina.Formulario = FormularioValido(Foto());
            _pagina.ModelState.AddModelError("Formulario.Nombre", "El nombre es obligatorio");

            // Act
            var resultado = await _pagina.OnPostAsync(CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<PageResult>();
            _pagina.ViewData["Categorias"].Should().BeOfType<SelectList>();
            _almacen.Verify(a => a.SaveFileAsync(It.IsAny<IFormFile?>(), It.IsAny<string>()), Times.Never);
            _servicio.Verify(s => s.CreateAsync(It.IsAny<Funko>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task OnPostAsync_ImagenRechazada_MuestraElMotivoEnElCampoYNoCreaNada()
        {
            // Arrange
            _pagina.Formulario = FormularioValido(Foto());
            _almacen.Setup(a => a.SaveFileAsync(It.IsAny<IFormFile?>(), It.IsAny<string>()))
                .ReturnsAsync(StorageResult.Fallo("El fichero es demasiado grande"));

            // Act
            var resultado = await _pagina.OnPostAsync(CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<PageResult>();
            _pagina.ModelState["Formulario.ImagenFile"]!.Errors.Should().ContainSingle()
                .Which.ErrorMessage.Should().Contain("demasiado grande");
            _servicio.Verify(s => s.CreateAsync(It.IsAny<Funko>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task OnPostAsync_ServicioDevuelveNullConImagen_BorraLaImagenHuerfana()
        {
            // Arrange
            _pagina.Formulario = FormularioValido(Foto());
            _almacen.Setup(a => a.SaveFileAsync(It.IsAny<IFormFile?>(), It.IsAny<string>()))
                .ReturnsAsync(StorageResult.Exito(RutaImagen));
            _servicio.Setup(s => s.CreateAsync(It.IsAny<Funko>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FunkoDto?)null);

            // Act
            var resultado = await _pagina.OnPostAsync(CancellationToken.None);

            // Assert
            resultado.Should().BeOfType<PageResult>();
            _pagina.ModelState[string.Empty]!.Errors.Should().ContainSingle();
            _pagina.TempData.ContainsKey("Exito").Should().BeFalse();
            _almacen.Verify(a => a.DeleteFile(RutaImagen), Times.Once);
        }
    }
}
