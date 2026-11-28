using Microsoft.AspNetCore.Http;

namespace FunkoApp.Shared.Services.Storage;

/// <summary>
/// Almacenamiento de los ficheros que suben los formularios. Las páginas llaman a esta interfaz
/// y no saben dónde se guarda nada: el guardado, el nombre del fichero y sus límites
/// son responsabilidad del servicio.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Valida un fichero subido y lo escribe en disco con un nombre que decide el servidor.
    /// </summary>
    /// <param name="file">Fichero recibido en el formulario; null si el usuario no eligió ninguno.</param>
    /// <param name="folder">Subcarpeta del almacén, por ejemplo <see cref="StorageFolders.Funkos"/>.</param>
    /// <returns>Resultado con la ruta pública si se guardó, o el motivo del rechazo.</returns>
    Task<StorageResult> SaveFileAsync(IFormFile? file, string folder);

    /// <summary>
    /// Borra un fichero del almacén. Solo toca ficheros que están dentro de la carpeta de subidas:
    /// una ruta que escapa con <c>..</c>, una ruta absoluta o la imagen por defecto de
    /// <c>wwwroot/images</c> no se borran nunca.
    /// </summary>
    /// <param name="publicPath">Ruta pública, por ejemplo /uploads/funkos/foto.jpg.</param>
    /// <returns>true si el fichero existía y se borró; false en cualquier otro caso.</returns>
    bool DeleteFile(string? publicPath);
}
