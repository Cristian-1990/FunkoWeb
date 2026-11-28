using System.Net;
using System.Text.RegularExpressions;
using FunkoApp.Shared.Data;
using FunkoApp.Shared.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace FunkoApp.Test.Integration;

/// <summary>
/// Base de las pruebas de integración. Levanta la aplicación real una vez por clase de pruebas,
/// con su base de datos y su wwwroot temporales, siembra dos categorías y ofrece un cliente HTTP
/// que se comporta como un navegador: guarda cookies (antifalsificación y TempData) y no sigue
/// las redirecciones solo, para poder comprobarlas.
/// </summary>
public abstract partial class IntegracionBase
{
    /// <summary>PNG mínimo: lo único que se valida es su firma de 8 bytes.</summary>
    protected static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01];

    protected FunkoWebFactory Fabrica = null!;
    protected HttpClient Cliente = null!;
    protected long TenistasId;
    protected long MarvelId;

    [OneTimeSetUp]
    public void ArrancarLaAplicacion()
    {
        Fabrica = new FunkoWebFactory();
        Cliente = Fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        using var alcance = Fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<FunkoDbContext>();

        var tenistas = new Category { Nombre = "Tenistas" };
        var marvel = new Category { Nombre = "Marvel" };
        contexto.Categories.AddRange(tenistas, marvel);
        contexto.Funkos.AddRange(
            new Funko { Nombre = "Rafael Nadal", Anio = 2021, PrecioReferencia = 17.50m, Categoria = tenistas },
            new Funko { Nombre = "Iron Man", Anio = 2019, PrecioReferencia = 12.50m, Categoria = marvel });
        contexto.SaveChanges();

        TenistasId = tenistas.Id;
        MarvelId = marvel.Id;
    }

    [OneTimeTearDown]
    public void ApagarLaAplicacion()
    {
        Cliente.Dispose();
        Fabrica.Dispose();
    }

    /// <summary>Descarga una página y devuelve su HTML con las entidades ya decodificadas.</summary>
    protected async Task<string> HtmlDeAsync(string url)
    {
        var respuesta = await Cliente.GetAsync(url);
        return WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
    }

    /// <summary>Lee el HTML de una respuesta con las entidades ya decodificadas.</summary>
    protected static async Task<string> HtmlDeAsync(HttpResponseMessage respuesta) =>
        WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

    /// <summary>Saca el token antifalsificación del formulario de una página.</summary>
    protected async Task<string> TokenDeAsync(string urlDelFormulario)
    {
        var html = await Cliente.GetStringAsync(urlDelFormulario);
        var coincidencia = TokenRegex().Match(html);
        Assert.That(coincidencia.Success, $"La página {urlDelFormulario} no trae token antifalsificación");
        return coincidencia.Groups[1].Value;
    }

    /// <summary>
    /// Envía un formulario como lo haría el navegador: pide antes la página para obtener su token
    /// y su cookie, y después hace el POST. Con imagen lo manda como multipart.
    /// </summary>
    protected async Task<HttpResponseMessage> EnviarFormularioAsync(
        string urlDelFormulario,
        Dictionary<string, string> campos,
        (string NombreFichero, byte[] Contenido)? imagen = null,
        bool conToken = true)
    {
        var datos = new Dictionary<string, string>(campos);
        if (conToken)
        {
            datos["__RequestVerificationToken"] = await TokenDeAsync(urlDelFormulario);
        }

        if (imagen is null)
        {
            return await Cliente.PostAsync(urlDelFormulario, new FormUrlEncodedContent(datos));
        }

        var multipart = new MultipartFormDataContent();
        foreach (var (clave, valor) in datos)
        {
            multipart.Add(new StringContent(valor), clave);
        }

        multipart.Add(new ByteArrayContent(imagen.Value.Contenido), "Formulario.ImagenFile", imagen.Value.NombreFichero);
        return await Cliente.PostAsync(urlDelFormulario, multipart);
    }

    /// <summary>Campos de un formulario de Funko válido; cada prueba cambia solo los que le importan.</summary>
    protected Dictionary<string, string> Campos(
        string nombre,
        string precio = "15,50",
        long? categoriaId = null) =>
        new()
        {
            ["Formulario.Nombre"] = nombre,
            ["Formulario.Anio"] = "2024",
            ["Formulario.PrecioReferencia"] = precio,
            ["Formulario.CategoriaId"] = (categoriaId ?? TenistasId).ToString()
        };

    /// <summary>Ejecuta una consulta contra la base de datos real de la prueba.</summary>
    protected T Consultar<T>(Func<FunkoDbContext, T> consulta)
    {
        using var alcance = Fabrica.Services.CreateScope();
        return consulta(alcance.ServiceProvider.GetRequiredService<FunkoDbContext>());
    }

    /// <summary>Inserta un Funko directamente en la base de datos y devuelve su Id.</summary>
    protected long InsertarFunko(string nombre, string? imagen = null)
    {
        using var alcance = Fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<FunkoDbContext>();
        var funko = new Funko { Nombre = nombre, Anio = 2020, PrecioReferencia = 10m, CategoriaId = MarvelId, Imagen = imagen };
        contexto.Funkos.Add(funko);
        contexto.SaveChanges();
        return funko.Id;
    }

    /// <summary>Ficheros de la carpeta de subidas cuyo nombre contiene el texto dado (vacío si la carpeta aún no existe).</summary>
    protected string[] FicherosSubidosQueContienen(string texto)
    {
        var carpeta = Path.Combine(Fabrica.WebRoot, "uploads", "funkos");
        return Directory.Exists(carpeta) ? Directory.GetFiles(carpeta, $"*{texto}*") : [];
    }

    /// <summary>Convierte una ruta pública (/uploads/...) en la ruta del fichero dentro del wwwroot de la prueba.</summary>
    protected string RutaFisica(string rutaPublica) =>
        Path.Combine(Fabrica.WebRoot, rutaPublica.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

    [GeneratedRegex(@"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""")]
    private static partial Regex TokenRegex();
}
