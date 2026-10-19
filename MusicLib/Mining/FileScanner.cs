namespace MusicLib.Mining;

/// <summary>
/// Implementación del lector de archivos.
/// </summary>
public sealed class FileScanner : IFileScanner
{
    /// <summary>
    /// Genera un enumerable que puede ser recorrido a través de un foreach
    /// donde se encuentran todos los archivos mp3 encontrados recursivamente.
    /// </summary>
    /// <param name="root">
    /// Ruta de inicio.
    /// </param>
    /// <returns>
    /// Enumerable de archivos.
    /// </returns>
    public IEnumerable<string> EnumerateMp3(string root)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            MatchCasing = MatchCasing.CaseInsensitive
        };

        return Directory.EnumerateFiles(Path.GetFullPath(root), "*.mp3", options);
    }
}