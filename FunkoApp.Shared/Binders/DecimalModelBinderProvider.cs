using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FunkoApp.Shared.Binders;

/// <summary>
/// Proveedor del binder de decimales.
/// Se registra en la primera posición de la lista para ganar al binder por defecto.
/// </summary>
public class DecimalModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(decimal) ||
            context.Metadata.ModelType == typeof(decimal?))
        {
            return new DecimalModelBinder();
        }

        return null;
    }
}
