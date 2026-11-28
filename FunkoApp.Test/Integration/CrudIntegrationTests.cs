using System.Net;
using FluentAssertions;
using FunkoApp.Shared.Models;
using NUnit.Framework;

namespace FunkoApp.Test.Integration;

/// <summary>
/// Pruebas de integración del alta, la edición y la eliminación, con sus imágenes.
/// Cada prueba usa nombres de Funko propios, así que no dependen unas de otras aunque
/// compartan la aplicación y la base de datos de su clase.
/// </summary>
[TestFixture]
public class CrudIntegrationTests
{
    [TestFixture]
    public class CasosPositivos : IntegracionBase
    {
        [Test]
        public async Task Crear_Valido_RedirigeALaPortadaConAvisoYElFunkoApareceEnElCatalogo()
        {
            // Arrange

            // Act
            var respuesta = await EnviarFormularioAsync("/Crear", Campos("Goku Ultra Instinto"));
            var portada = await HtmlDeAsync("/");

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.Redirect);
            respuesta.Headers.Location!.OriginalString.Should().Be("/");
            portada.Should().Contain("creado correctamente").And.Contain("Goku Ultra Instinto");
            Consultar(c => c.Funkos.Count(f => f.Nombre == "Goku Ultra Instinto")).Should().Be(1);
        }

        [Test]
        public async Task Crear_PrecioConPuntoOConComa_GuardaElMismoNumero()
        {
            // Arrange

            // Act
            await EnviarFormularioAsync("/Crear", Campos("Funko precio punto", precio: "15.50"));
            await EnviarFormularioAsync("/Crear", Campos("Funko precio coma", precio: "15,50"));

            // Assert
            Consultar(c => c.Funkos.Single(f => f.Nombre == "Funko precio punto").PrecioReferencia).Should().Be(15.50m);
            Consultar(c => c.Funkos.Single(f => f.Nombre == "Funko precio coma").PrecioReferencia).Should().Be(15.50m);
        }

        [Test]
        public async Task Crear_ConImagen_LaGuardaEnDiscoYLaServidaPorHttp()
        {
            // Arrange

            // Act
            var respuesta = await EnviarFormularioAsync("/Crear", Campos("Funko con foto"), imagen: ("mi foto.png", Png));
            var ruta = Consultar(c => c.Funkos.Single(f => f.Nombre == "Funko con foto").Imagen);
            var descarga = await Cliente.GetAsync(ruta);

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.Redirect);
            ruta.Should().StartWith("/uploads/funkos/").And.EndWith(".png");
            File.Exists(RutaFisica(ruta!)).Should().BeTrue();
            descarga.StatusCode.Should().Be(HttpStatusCode.OK);
            descarga.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        }

        [Test]
        public async Task Crear_SinImagen_LaPortadaUsaLaImagenPorDefecto()
        {
            // Arrange

            // Act
            await EnviarFormularioAsync("/Crear", Campos("Funko sin foto"));
            var portada = await HtmlDeAsync("/");

            // Assert
            Consultar(c => c.Funkos.Single(f => f.Nombre == "Funko sin foto").Imagen).Should().BeNull();
            portada.Should().Contain("/images/funko-sin-imagen.svg");
        }

        [Test]
        public async Task Editar_ConOtraImagen_ActualizaYBorraLaImagenVieja()
        {
            // Arrange
            await EnviarFormularioAsync("/Crear", Campos("Funko a reeditar"), imagen: ("vieja.png", Png));
            var id = Consultar(c => c.Funkos.Single(f => f.Nombre == "Funko a reeditar").Id);
            var rutaVieja = Consultar(c => c.Funkos.Single(f => f.Id == id).Imagen)!;

            // Act
            var respuesta = await EnviarFormularioAsync($"/Editar/{id}", Campos("Funko reeditado", precio: "20"), imagen: ("nueva.png", Png));
            var rutaNueva = Consultar(c => c.Funkos.Single(f => f.Id == id).Imagen)!;

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.Redirect);
            Consultar(c => c.Funkos.Single(f => f.Id == id).Nombre).Should().Be("Funko reeditado");
            rutaNueva.Should().NotBe(rutaVieja);
            File.Exists(RutaFisica(rutaVieja)).Should().BeFalse();
            File.Exists(RutaFisica(rutaNueva)).Should().BeTrue();
        }

        [Test]
        public async Task Editar_SinImagenNueva_ConservaLaImagenYSuFichero()
        {
            // Arrange
            await EnviarFormularioAsync("/Crear", Campos("Funko que conserva"), imagen: ("foto.png", Png));
            var id = Consultar(c => c.Funkos.Single(f => f.Nombre == "Funko que conserva").Id);
            var ruta = Consultar(c => c.Funkos.Single(f => f.Id == id).Imagen)!;

            // Act
            await EnviarFormularioAsync($"/Editar/{id}", Campos("Funko que conserva", precio: "99"));

            // Assert
            Consultar(c => c.Funkos.Single(f => f.Id == id).PrecioReferencia).Should().Be(99m);
            Consultar(c => c.Funkos.Single(f => f.Id == id).Imagen).Should().Be(ruta);
            File.Exists(RutaFisica(ruta)).Should().BeTrue();
        }

        [Test]
        public async Task Editar_EnseñaElFormularioPrecargadoConLaImagenActual()
        {
            // Arrange
            await EnviarFormularioAsync("/Crear", Campos("Funko precargado"), imagen: ("foto.png", Png));
            var id = Consultar(c => c.Funkos.Single(f => f.Nombre == "Funko precargado").Id);

            // Act
            var html = await HtmlDeAsync($"/Editar/{id}");

            // Assert
            html.Should().Contain("value=\"Funko precargado\"").And.Contain("Imagen actual").And.Contain("/uploads/funkos/");
        }

        [Test]
        public async Task Eliminar_Confirmado_BorraElFunkoYSuFichero()
        {
            // Arrange
            await EnviarFormularioAsync("/Crear", Campos("Funko a borrar"), imagen: ("foto.png", Png));
            var id = Consultar(c => c.Funkos.Single(f => f.Nombre == "Funko a borrar").Id);
            var ruta = Consultar(c => c.Funkos.Single(f => f.Id == id).Imagen)!;

            // Act
            var paginaDeConfirmacion = await HtmlDeAsync($"/Eliminar/{id}");
            var existeTrasElGet = Consultar(c => c.Funkos.Any(f => f.Id == id));
            var respuesta = await EnviarFormularioAsync($"/Eliminar/{id}", []);
            var portada = await HtmlDeAsync("/");

            // Assert
            paginaDeConfirmacion.Should().Contain("Funko a borrar").And.Contain("Sí, eliminar");
            existeTrasElGet.Should().BeTrue("el GET solo enseña los datos, no borra");
            respuesta.StatusCode.Should().Be(HttpStatusCode.Redirect);
            Consultar(c => c.Funkos.Any(f => f.Id == id)).Should().BeFalse();
            File.Exists(RutaFisica(ruta)).Should().BeFalse();
            portada.Should().Contain("eliminado correctamente");
        }

        [Test]
        public async Task Eliminar_FunkoSinImagenPropia_NoToca_LaImagenPorDefecto()
        {
            // Arrange
            var id = InsertarFunko("Funko sin imagen propia");
            var porDefecto = Path.Combine(Fabrica.WebRoot, "images", "funko-sin-imagen.svg");
            Directory.CreateDirectory(Path.GetDirectoryName(porDefecto)!);
            await File.WriteAllTextAsync(porDefecto, "<svg/>");

            // Act
            var respuesta = await EnviarFormularioAsync($"/Eliminar/{id}", []);

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.Redirect);
            Consultar(c => c.Funkos.Any(f => f.Id == id)).Should().BeFalse();
            File.Exists(porDefecto).Should().BeTrue();
        }
    }

    [TestFixture]
    public class CasosNegativos : IntegracionBase
    {
        [Test]
        public async Task Crear_SinTokenAntifalsificacion_Devuelve400YNoCreaNada()
        {
            // Arrange

            // Act
            var respuesta = await EnviarFormularioAsync("/Crear", Campos("Funko sin token"), conToken: false);

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            Consultar(c => c.Funkos.Any(f => f.Nombre == "Funko sin token")).Should().BeFalse();
        }

        [Test]
        public async Task Crear_SinNombreNiCategoria_VuelveAlFormularioConLosAvisosYNoCrea()
        {
            // Arrange
            var campos = Campos("");
            campos["Formulario.CategoriaId"] = "0";
            var antes = Consultar(c => c.Funkos.Count());

            // Act
            var respuesta = await EnviarFormularioAsync("/Crear", campos);
            var html = await HtmlDeAsync(respuesta);

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
            html.Should().Contain("El nombre es obligatorio").And.Contain("Elige una categoría");
            Consultar(c => c.Funkos.Count()).Should().Be(antes);
        }

        [Test]
        public async Task Crear_NombreRepetido_MuestraElErrorYNoDuplica()
        {
            // Arrange

            // Act
            var respuesta = await EnviarFormularioAsync("/Crear", Campos("Rafael Nadal"));
            var html = await HtmlDeAsync(respuesta);

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
            html.Should().Contain("No se ha podido crear el Funko");
            Consultar(c => c.Funkos.Count(f => f.Nombre == "Rafael Nadal")).Should().Be(1);
        }

        [Test]
        public async Task Crear_PrecioNoNumerico_MuestraElErrorDelBinderYNoCrea()
        {
            // Arrange

            // Act
            var respuesta = await EnviarFormularioAsync("/Crear", Campos("Funko barato", precio: "barato"));
            var html = await HtmlDeAsync(respuesta);

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
            html.Should().Contain("«barato» no es un número válido");
            Consultar(c => c.Funkos.Any(f => f.Nombre == "Funko barato")).Should().BeFalse();
        }

        [Test]
        public async Task Crear_ImagenConContenidoFalso_SeRechazaYNoSeCreaNiSeGuardaNada()
        {
            // Arrange
            byte[] ejecutable = [.. "MZ esto no es una imagen"u8];

            // Act
            var respuesta = await EnviarFormularioAsync("/Crear", Campos("Funko disfrazado"), imagen: ("disfrazado.png", ejecutable));
            var html = await HtmlDeAsync(respuesta);

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
            html.Should().Contain("no corresponde a una imagen");
            Consultar(c => c.Funkos.Any(f => f.Nombre == "Funko disfrazado")).Should().BeFalse();
            FicherosSubidosQueContienen("disfrazado").Should().BeEmpty("el contenido falso no debe llegar a guardarse");
        }

        [Test]
        public async Task Crear_ImagenConExtensionNoPermitida_SeRechaza()
        {
            // Arrange

            // Act
            var respuesta = await EnviarFormularioAsync("/Crear", Campos("Funko exe"), imagen: ("virus.exe", Png));
            var html = await HtmlDeAsync(respuesta);

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
            html.Should().Contain("no está permitida");
            Consultar(c => c.Funkos.Any(f => f.Nombre == "Funko exe")).Should().BeFalse();
        }

        [Test]
        public async Task Editar_ConNombreDeOtroFunko_MuestraElErrorYBorraLaImagenRecienSubida()
        {
            // Arrange
            var id = InsertarFunko("Funko que no debe cambiar");

            // Act
            var respuesta = await EnviarFormularioAsync($"/Editar/{id}", Campos("Rafael Nadal"), imagen: ("nueva.png", Png));
            var html = await HtmlDeAsync(respuesta);

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
            html.Should().Contain("No se han podido guardar los cambios");
            Consultar(c => c.Funkos.Single(f => f.Id == id).Nombre).Should().Be("Funko que no debe cambiar");
            FicherosSubidosQueContienen("nueva").Should().BeEmpty("la imagen subida no debe quedar huérfana");
        }

        [Test]
        public async Task Editar_Inexistente_RedirigeALaPortadaConAviso()
        {
            // Arrange

            // Act
            var respuesta = await Cliente.GetAsync("/Editar/9999");
            var portada = await HtmlDeAsync("/");

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.Redirect);
            respuesta.Headers.Location!.OriginalString.Should().Be("/");
            portada.Should().Contain("no existe");
        }

        [Test]
        public async Task Eliminar_Inexistente_RedirigeALaPortadaConAviso()
        {
            // Arrange

            // Act
            var respuesta = await Cliente.GetAsync("/Eliminar/9999");
            var portada = await HtmlDeAsync("/");

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.Redirect);
            portada.Should().Contain("no existe");
        }

        [Test]
        public async Task Eliminar_SinTokenAntifalsificacion_Devuelve400YNoBorraNada()
        {
            // Arrange
            var id = InsertarFunko("Funko protegido");

            // Act
            var respuesta = await EnviarFormularioAsync($"/Eliminar/{id}", [], conToken: false);

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            Consultar(c => c.Funkos.Any(f => f.Id == id)).Should().BeTrue();
        }
    }
}
