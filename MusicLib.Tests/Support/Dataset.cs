namespace MusicLib.Tests.Support;

/// <summary>
/// Acceso a los archivos del OjalvoMusicLibrary/Dataset.
/// </summary>
public static class Dataset
{
    /// <summary>
    /// Directorio Dataset dentro del directorio de salida.
    /// </summary>
    public static string Root => Path.Combine
    (
        AppContext.BaseDirectory,
        "Dataset"
    );

    /// <summary>
    /// Ruta absoluta de un archivo o carpeta del Dataset.
    /// Falla con un mensaje si no existe.
    /// </summary>
    public static string At(params string[] parts)
    {
        var full = Path.Combine(new[] { Root }.Concat(parts).ToArray());

        if (!File.Exists(full) && !Directory.Exists(full))
            throw new FileNotFoundException($"No existe '{full}'.");

        return full;
    }
}
