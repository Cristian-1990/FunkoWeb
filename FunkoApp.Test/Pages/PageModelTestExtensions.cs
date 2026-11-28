using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace FunkoApp.Test.Pages;

/// <summary>
/// Ayuda para probar un PageModel sin arrancar el servidor: le da el contexto, el ModelState,
/// el ViewData y el TempData que en ejecución le pone el framework.
/// </summary>
internal static class PageModelTestExtensions
{
    /// <summary>
    /// Monta el contexto mínimo que necesita un PageModel para ejecutar sus handlers.
    /// </summary>
    /// <typeparam name="T">Tipo del PageModel.</typeparam>
    /// <param name="pagina">PageModel a preparar.</param>
    /// <returns>El mismo PageModel, para encadenar.</returns>
    public static T ConContexto<T>(this T pagina) where T : PageModel
    {
        var http = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(http, new RouteData(), new PageActionDescriptor(), modelState);

        pagina.PageContext = new PageContext(actionContext)
        {
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), modelState)
        };
        pagina.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        return pagina;
    }
}
