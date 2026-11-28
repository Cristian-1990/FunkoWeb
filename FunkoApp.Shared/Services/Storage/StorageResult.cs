namespace FunkoApp.Shared.Services.Storage;

/// <summary>
/// Resultado de guardar un fichero. Un fichero puede fallar por varios motivos (vacío, tamaño,
/// extensión, contenido) y el formulario necesita el motivo para pintarlo: el servicio no lanza
/// excepciones, devuelve el resultado.
/// </summary>
/// <param name="Ok">Si el fichero se guardó.</param>
/// <param name="Mensaje">Motivo del fallo; vacío cuando todo ha ido bien.</param>
/// <param name="Ruta">Ruta pública del fichero guardado, por ejemplo /uploads/funkos/foto.jpg.</param>
public record StorageResult(bool Ok, string Mensaje, string Ruta)
{
    /// <summary>Resultado de un guardado correcto.</summary>
    /// <param name="ruta">Ruta pública del fichero recién escrito.</param>
    /// <returns>Resultado marcado como correcto.</returns>
    public static StorageResult Exito(string ruta) => new(true, string.Empty, ruta);

    /// <summary>Resultado de un guardado rechazado.</summary>
    /// <param name="mensaje">Motivo legible para quien usa el formulario.</param>
    /// <returns>Resultado marcado como fallido, sin ruta.</returns>
    public static StorageResult Fallo(string mensaje) => new(false, mensaje, string.Empty);
}
