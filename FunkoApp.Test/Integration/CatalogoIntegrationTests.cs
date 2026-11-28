using System.Net;
using FluentAssertions;
using NUnit.Framework;

namespace FunkoApp.Test.Integration;

/// <summary>
/// Pruebas de integración de la parte de consulta: portada, buscador, categorías, layout y Acerca de.
/// Arrancan la aplicación real (páginas, servicios, EF y SQLite) y la usan por HTTP.
/// </summary>
[TestFixture]
public class CatalogoIntegrationTests
{
    [TestFixture]
    public class CasosPositivos : IntegracionBase
    {
        [Test]
        public async Task Portada_ListaLosFunkosConSuCategoria()
        {
            // Arrange

            // Act
            var respuesta = await Cliente.GetAsync("/");
            var html = await HtmlDeAsync(respuesta);

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
            html.Should().Contain("Rafael Nadal").And.Contain("Iron Man").And.Contain("Tenistas");
        }

        [Test]
        public async Task Portada_BusquedaEnMinusculas_FiltraPorNombre()
        {
            // Arrange

            // Act
            var html = await HtmlDeAsync("/?busqueda=nadal");

            // Assert
            html.Should().Contain("Rafael Nadal").And.NotContain("Iron Man");
        }

        [Test]
        public async Task Categoria_MuestraSoloLosFunkosDeEsaCategoria()
        {
            // Arrange

            // Act
            var html = await HtmlDeAsync($"/Categoria/{MarvelId}");

            // Assert
            html.Should().Contain("Iron Man").And.NotContain("Rafael Nadal");
        }

        [Test]
        public async Task Layout_TieneElMenuConLasCategoriasDeLaBaseDeDatosYElPie()
        {
            // Arrange

            // Act
            var html = await HtmlDeAsync("/");

            // Assert
            html.Should().Contain($"/Categoria/{TenistasId}").And.Contain($"/Categoria/{MarvelId}");
            html.Should().Contain("Catálogo").And.Contain("Acerca de");
            html.Should().Contain("2DAW").And.Contain("2026");
            html.Should().Contain("lang=\"es\"");
        }

        [Test]
        public async Task AcercaDe_EnseñaLasFichasDeGitHubDeLosAutores()
        {
            // Arrange

            // Act
            var html = await HtmlDeAsync("/AcercaDe");

            // Assert
            html.Should().Contain("github.com/Cristian-1990").And.Contain("github.com/JesusCoboArrogante");
        }

        [Test]
        public async Task Portada_EscapaElHtmlDelNombreDeUnFunko()
        {
            // Arrange
            InsertarFunko("<script>alert('xss')</script>");

            // Act
            var crudo = await Cliente.GetStringAsync("/");

            // Assert
            crudo.Should().NotContain("<script>alert('xss')</script>");
            crudo.Should().Contain("&lt;script&gt;");
        }
    }

    [TestFixture]
    public class CasosNegativos : IntegracionBase
    {
        [Test]
        public async Task Categoria_Inexistente_Devuelve404()
        {
            // Arrange

            // Act
            var respuesta = await Cliente.GetAsync("/Categoria/9999");

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task Portada_BusquedaSinResultados_AvisaDeQueNoHayCoincidencias()
        {
            // Arrange

            // Act
            var html = await HtmlDeAsync("/?busqueda=pikachu");

            // Assert
            html.Should().Contain("No hay Funkos que coincidan").And.NotContain("Rafael Nadal");
        }

        [TestCase("/NoExiste")]
        [TestCase("/Editar/abc")]
        [TestCase("/Categoria/abc")]
        public async Task RutaInexistenteOConIdNoNumerico_Devuelve404(string ruta)
        {
            // Arrange

            // Act
            var respuesta = await Cliente.GetAsync(ruta);

            // Assert
            respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
