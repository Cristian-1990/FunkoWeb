using System.ComponentModel.DataAnnotations;

namespace FunkoApp.Shared.ViewModels;

/// <summary>
/// Formulario de alta y edición de un Funko.
/// La vista lo usa con asp-for, que escribe el nombre de cada campo como atributo name.
/// Las reglas de validación viven aquí, en atributos; la entidad se mantiene limpia.
/// </summary>
public class FunkoFormViewModel
{
    /// <summary>0 en el alta; el Id real al editar. No aparece en el formulario.</summary>
    public long Id { get; set; }

    /// <summary>Nombre del Funko (obligatorio, hasta 200 caracteres).</summary>
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Año de lanzamiento de la figura.</summary>
    [Range(1998, 2100, ErrorMessage = "El año debe estar entre 1998 y 2100")]
    [Display(Name = "Año")]
    public int Anio { get; set; } = DateTime.Now.Year;

    /// <summary>Precio de referencia en euros, sin IVA; el binder admite coma o punto.</summary>
    [Range(0.01, 100000, ErrorMessage = "El precio debe estar entre 0,01 y 100.000")]
    [Display(Name = "Precio de referencia (euros)")]
    public decimal PrecioReferencia { get; set; }

    /// <summary>Categoría a la que pertenece; el formulario envía su Id.</summary>
    [Range(1, long.MaxValue, ErrorMessage = "Elige una categoría")]
    [Display(Name = "Categoría")]
    public long CategoriaId { get; set; }

    /// <summary>Indica si el Funko se muestra como novedad.</summary>
    [Display(Name = "Novedad")]
    public bool EsNovedad { get; set; }
}
