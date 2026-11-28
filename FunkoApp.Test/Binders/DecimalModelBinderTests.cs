using FluentAssertions;
using FunkoApp.Shared.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NUnit.Framework;

namespace FunkoApp.Test.Binders;

/// <summary>
/// Pruebas del binder propio de decimales.
/// Se monta a mano el contexto que el binder recibe en ejecución, con un proveedor de valores
/// de prueba: así se comprueba el comportamiento sin arrancar el servidor.
/// </summary>
[TestFixture]
public class DecimalModelBinderTests
{
    /// <summary>
    /// Proveedor de valores mínimo: devuelve el texto fijado en la prueba o None.
    /// </summary>
    /// <param name="valores">Pares nombre de campo y texto enviado por el formulario.</param>
    private sealed class ValueProviderDePrueba(Dictionary<string, string> valores) : IValueProvider
    {
        public bool ContainsPrefix(string prefix) => valores.ContainsKey(prefix);

        public ValueProviderResult GetValue(string key) =>
            valores.TryGetValue(key, out var valor)
                ? new ValueProviderResult(valor)
                : ValueProviderResult.None;
    }

    private static DefaultModelBindingContext CrearContexto(string? valor)
    {
        var metadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(decimal));

        var valores = new Dictionary<string, string>();
        if (valor is not null)
        {
            valores["precio"] = valor;
        }

        return new DefaultModelBindingContext
        {
            ModelMetadata = metadata,
            ModelName = "precio",
            ModelState = new ModelStateDictionary(),
            ValueProvider = new ValueProviderDePrueba(valores)
        };
    }

    [TestFixture]
    public class CasosPositivos
    {
        [TestCase("15,50", 15.50)]
        [TestCase("15.50", 15.50)]
        [TestCase("1999,99", 1999.99)]
        [TestCase("1999.99", 1999.99)]
        [TestCase("0,01", 0.01)]
        [TestCase("20", 20)]
        public async Task BindModelAsync_ConComaOPunto_DevuelveElMismoDecimal(string texto, decimal esperado)
        {
            // Arrange
            var contexto = CrearContexto(texto);
            var binder = new DecimalModelBinder();

            // Act
            await binder.BindModelAsync(contexto);

            // Assert
            contexto.Result.IsModelSet.Should().BeTrue();
            contexto.Result.Model.Should().Be(esperado);
            contexto.ModelState.ErrorCount.Should().Be(0);
        }
    }

    [TestFixture]
    public class CasosNegativos
    {
        [Test]
        public async Task BindModelAsync_SinValor_NoMarcaElModelo()
        {
            // Arrange
            var contexto = CrearContexto(null);
            var binder = new DecimalModelBinder();

            // Act
            await binder.BindModelAsync(contexto);

            // Assert
            contexto.Result.IsModelSet.Should().BeFalse();
            contexto.ModelState.IsValid.Should().BeTrue();
        }

        [Test]
        public async Task BindModelAsync_ValorEnBlanco_NoMarcaElModelo()
        {
            // Arrange
            var contexto = CrearContexto("   ");
            var binder = new DecimalModelBinder();

            // Act
            await binder.BindModelAsync(contexto);

            // Assert
            contexto.Result.IsModelSet.Should().BeFalse();
        }

        [TestCase("barato")]
        [TestCase("1.299,00")]
        [TestCase("1,299.00")]
        [TestCase("12,5,0")]
        public async Task BindModelAsync_ValorNoValido_AnadeErrorYConservaElTexto(string texto)
        {
            // Arrange
            var contexto = CrearContexto(texto);
            var binder = new DecimalModelBinder();

            // Act
            await binder.BindModelAsync(contexto);

            // Assert
            contexto.Result.IsModelSet.Should().BeFalse();
            contexto.ModelState.IsValid.Should().BeFalse();
            contexto.ModelState["precio"]!.Errors.Should().ContainSingle();
            contexto.ModelState["precio"]!.RawValue.Should().Be(texto);
        }
    }
}
