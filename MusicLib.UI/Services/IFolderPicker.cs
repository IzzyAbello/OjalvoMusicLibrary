namespace MusicLib.UI.Services;

/// <summary>
/// Pide al usuario un directorio. Separa al ViewModel de la ventana.
/// </summary>
public interface IFolderPicker
{
    /// <summary>
    /// Muestra el selector de carpetas.
    /// </summary>
    /// <param name="title">
    /// Título del diálogo.
    /// </param>
    /// <returns>
    /// Ruta local elegida, o null si el usuario canceló.
    /// </returns>
    Task<string?> PickAsync(string title);
}
