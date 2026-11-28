using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FunkoApp.Shared.Binders;

/// <summary>
/// Binder propio para decimales.
/// El binder por defecto lee el número con la cultura del hilo: en un servidor en español
/// "15.50" se interpretaría como 1550. Este acepta "15,50" y "15.50" y siempre lo convierte
/// con la cultura invariable. Cualquier otra forma (por ejemplo "1.299,00") se rechaza
/// con un error en el ModelState en lugar de adivinar qué separador es el decimal.
/// </summary>
public class DecimalModelBinder : IModelBinder
{
    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var resultado = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        if (resultado == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        // Se guarda el texto tal y como llegó, para poder repintar el formulario si hay error.
        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, resultado);

        var texto = resultado.FirstValue;

        if (string.IsNullOrWhiteSpace(texto))
        {
            return Task.CompletedTask;
        }

        var normalizado = texto.Replace(',', '.');

        if (decimal.TryParse(normalizado, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor))
        {
            bindingContext.Result = ModelBindingResult.Success(valor);
        }
        else
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"«{texto}» no es un número válido");
        }

        return Task.CompletedTask;
    }
}
