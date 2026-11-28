using System.Text;
using FluentAssertions;
using FunkoApp.Shared.Services.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace FunkoApp.Test.Services;

/// <summary>
/// Pruebas del almacenamiento de ficheros sobre una carpeta temporal real: cada prueba tiene
/// su propio wwwroot y se borra al terminar, así que no se pisan entre ellas ni ensucian el proyecto.
/// </summary>
[TestFixture]
public class StorageServiceTests
{
    private string _wwwroot = null!;
    private readonly List<string> _ficherosExtra = [];

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01];
    private static readonly byte[] Jpg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    private static readonly byte[] Gif = Encoding.ASCII.GetBytes("GIF89a\x01\x00");
    private static readonly byte[] NoEsImagen = Encoding.ASCII.GetBytes("MZ-esto-es-un-ejecutable");

    [SetUp]
    public void SetUp()
    {
        _wwwroot = Path.Combine(Path.GetTempPath(), $"funkoapp-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_wwwroot);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_wwwroot))
        {
            Directory.Delete(_wwwroot, recursive: true);
        }

        foreach (var fichero in _ficherosExtra.Where(File.Exists))
        {
            File.Delete(fichero);
        }
    }

    private StorageService CrearServicio(long maxBytes = 1024 * 1024)
    {
        var entorno = new Mock<IWebHostEnvironment>();
        entorno.SetupGet(e => e.WebRootPath).Returns(_wwwroot);

        var valores = new Dictionary<string, string?>
        {
            ["Storage:UploadPath"] = "uploads",
            ["Storage:MaxFileSize"] = maxBytes.ToString(),
            ["Storage:AllowedExtensions:0"] = ".jpg",
            ["Storage:AllowedExtensions:1"] = ".jpeg",
            ["Storage:AllowedExtensions:2"] = ".png",
            ["Storage:AllowedExtensions:3"] = ".gif"
        };
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

        return new StorageService(entorno.Object, configuracion, NullLogger<StorageService>.Instance);
    }

    private static FormFile Fichero(string nombre, byte[] contenido) =>
        new(new MemoryStream(contenido), 0, contenido.Length, "ImagenFile", nombre);

    private string RutaFisica(string rutaPublica) =>
        Path.Combine(_wwwroot, rutaPublica.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

    [TestFixture]
    public class CasosPositivos : StorageServiceTests
    {
        [Test]
        public async Task SaveFileAsync_PngValido_LoGuardaYDevuelveLaRutaPublica()
        {
            // Arrange
            var servicio = CrearServicio();

            // Act
            var resultado = await servicio.SaveFileAsync(Fichero("goku.png", Png), StorageFolders.Funkos);

            // Assert
            resultado.Ok.Should().BeTrue();
            resultado.Ruta.Should().StartWith("/uploads/funkos/").And.EndWith(".png");
            File.Exists(RutaFisica(resultado.Ruta)).Should().BeTrue();
        }

        [TestCase("foto.jpg")]
        [TestCase("FOTO.JPEG")]
        [TestCase("animada.gif")]
        public async Task SaveFileAsync_JpegYGifValidos_LosGuarda(string nombre)
        {
            // Arrange
            var servicio = CrearServicio();
            var contenido = nombre.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ? Gif : Jpg;

            // Act
            var resultado = await servicio.SaveFileAsync(Fichero(nombre, contenido), StorageFolders.Funkos);

            // Assert
            resultado.Ok.Should().BeTrue();
            File.Exists(RutaFisica(resultado.Ruta)).Should().BeTrue();
        }

        [Test]
        public async Task SaveFileAsync_NombreConRutaTrucada_NoSaleDeLaCarpetaDeSubidas()
        {
            // Arrange
            var servicio = CrearServicio();

            // Act
            var resultado = await servicio.SaveFileAsync(Fichero("../../../secreto.png", Png), StorageFolders.Funkos);

            // Assert
            resultado.Ok.Should().BeTrue();
            resultado.Ruta.Should().NotContain("..");
            Path.GetFullPath(RutaFisica(resultado.Ruta))
                .Should().StartWith(Path.Combine(_wwwroot, "uploads", "funkos"));
        }

        [Test]
        public async Task SaveFileAsync_DosFicherosConElMismoNombre_NoSePisan()
        {
            // Arrange
            var servicio = CrearServicio();

            // Act
            var primero = await servicio.SaveFileAsync(Fichero("goku.png", Png), StorageFolders.Funkos);
            var segundo = await servicio.SaveFileAsync(Fichero("goku.png", Png), StorageFolders.Funkos);

            // Assert
            primero.Ruta.Should().NotBe(segundo.Ruta);
            Directory.GetFiles(Path.Combine(_wwwroot, "uploads", "funkos")).Should().HaveCount(2);
        }

        [Test]
        public async Task DeleteFile_FicheroDeSubidas_LoBorraYDevuelveTrue()
        {
            // Arrange
            var servicio = CrearServicio();
            var guardado = await servicio.SaveFileAsync(Fichero("goku.png", Png), StorageFolders.Funkos);

            // Act
            var borrado = servicio.DeleteFile(guardado.Ruta);

            // Assert
            borrado.Should().BeTrue();
            File.Exists(RutaFisica(guardado.Ruta)).Should().BeFalse();
        }
    }

    [TestFixture]
    public class CasosNegativos : StorageServiceTests
    {
        [Test]
        public async Task SaveFileAsync_SinFichero_DevuelveFalloYNoCreaNada()
        {
            // Arrange
            var servicio = CrearServicio();

            // Act
            var resultado = await servicio.SaveFileAsync(null, StorageFolders.Funkos);

            // Assert
            resultado.Ok.Should().BeFalse();
            Directory.Exists(Path.Combine(_wwwroot, "uploads")).Should().BeFalse();
        }

        [Test]
        public async Task SaveFileAsync_FicheroVacio_DevuelveFallo()
        {
            // Arrange
            var servicio = CrearServicio();

            // Act
            var resultado = await servicio.SaveFileAsync(Fichero("vacio.png", []), StorageFolders.Funkos);

            // Assert
            resultado.Ok.Should().BeFalse();
        }

        [Test]
        public async Task SaveFileAsync_FicheroDemasiadoGrande_DevuelveFalloYNoGuarda()
        {
            // Arrange
            var servicio = CrearServicio(maxBytes: 5);

            // Act
            var resultado = await servicio.SaveFileAsync(Fichero("goku.png", Png), StorageFolders.Funkos);

            // Assert
            resultado.Ok.Should().BeFalse();
            resultado.Mensaje.Should().Contain("demasiado grande");
            Directory.Exists(Path.Combine(_wwwroot, "uploads")).Should().BeFalse();
        }

        [TestCase("virus.exe")]
        [TestCase("pagina.html")]
        [TestCase("sinextension")]
        public async Task SaveFileAsync_ExtensionNoPermitida_DevuelveFallo(string nombre)
        {
            // Arrange
            var servicio = CrearServicio();

            // Act
            var resultado = await servicio.SaveFileAsync(Fichero(nombre, Png), StorageFolders.Funkos);

            // Assert
            resultado.Ok.Should().BeFalse();
            resultado.Mensaje.Should().Contain("no está permitida");
        }

        [Test]
        public async Task SaveFileAsync_ExtensionDeImagenPeroContenidoDistinto_DevuelveFalloYNoGuarda()
        {
            // Arrange
            var servicio = CrearServicio();

            // Act
            var resultado = await servicio.SaveFileAsync(Fichero("disfrazado.png", NoEsImagen), StorageFolders.Funkos);

            // Assert
            resultado.Ok.Should().BeFalse();
            resultado.Mensaje.Should().Contain("contenido");
            Directory.Exists(Path.Combine(_wwwroot, "uploads")).Should().BeFalse();
        }

        [Test]
        public void DeleteFile_ImagenPorDefectoFueraDeSubidas_NoLaBorra()
        {
            // Arrange
            var servicio = CrearServicio();
            var porDefecto = Path.Combine(_wwwroot, "images", "funko-sin-imagen.svg");
            Directory.CreateDirectory(Path.GetDirectoryName(porDefecto)!);
            File.WriteAllText(porDefecto, "<svg/>");

            // Act
            var borrado = servicio.DeleteFile("/images/funko-sin-imagen.svg");

            // Assert
            borrado.Should().BeFalse();
            File.Exists(porDefecto).Should().BeTrue();
        }

        [Test]
        public void DeleteFile_RutaTrucada_NoBorraNadaFueraDeSubidas()
        {
            // Arrange
            var servicio = CrearServicio();
            Directory.CreateDirectory(Path.Combine(_wwwroot, "uploads", "funkos"));
            var fuera = Path.Combine(Path.GetDirectoryName(_wwwroot)!, $"fuera-{Guid.NewGuid():N}.txt");
            File.WriteAllText(fuera, "no me toques");
            _ficherosExtra.Add(fuera);

            // Act
            var borrado = servicio.DeleteFile($"/uploads/funkos/../../../{Path.GetFileName(fuera)}");

            // Assert
            borrado.Should().BeFalse();
            File.Exists(fuera).Should().BeTrue();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("/uploads/funkos/no-existe.png")]
        public void DeleteFile_RutaVaciaOInexistente_DevuelveFalse(string? ruta)
        {
            // Arrange
            var servicio = CrearServicio();

            // Act
            var borrado = servicio.DeleteFile(ruta);

            // Assert
            borrado.Should().BeFalse();
        }
    }
}
