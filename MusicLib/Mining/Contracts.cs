namespace MusicLib.Mining;

/// <summary>
/// Metadatos crudos de un archivo MP3.
/// Inmutable: lo produce el lector, lo consume el minero.
/// </summary>
public record TagInfo
(
    string? Title,
    string? Performer,      // intérpretes, unidos con ", "
    string? AlbumArtist,    // respaldo si no hay Performer
    string? Album,
    int Track,
    int Year,
    string? Genre
);

/// <summary>
/// Lee metadatos de un archivo.
/// </summary>
public interface ITagReader
{
    /// <summary>
    /// Método de lectura.
    /// </summary>
    /// <param name="path">
    /// Ruta del archivo a leer.
    /// </param>
    /// <returns>
    /// Record de metadatos del archivo MP3
    /// </returns>
    TagInfo Read(string path);
}

/// <summary>
/// Enumera las rutas de los MP3 bajo un directorio.
/// </summary>
public interface IFileScanner
{
    /// <summary>
    /// Genera una estructura iterable de los archivos MP3 encontrados
    /// recursivamente con raíz en la ruta indicada.
    /// </summary>
    /// <param name="root">
    /// Ruta indicada.
    /// </param>
    /// <returns>
    /// Estructura iterable de los archivos MP3 encontrados.
    /// </returns>
    IEnumerable<string> EnumerateMp3(string root);
}

/// <summary>
/// Fase en la que se encuentra el minero.
/// </summary>
public enum MiningPhase
{
    /// <summary>
    /// Leyendo las etiquetas de los MP3.
    /// </summary>
    Reading,

    /// <summary>
    /// Guardando en la BD lo que no existe todavía.
    /// </summary>
    Saving
}

/// <summary>
/// Progreso del proceso de mining.
/// </summary>
/// <param name="Phase">
/// Fase actual.
/// </param>
/// <param name="Processed">
/// Archivos procesados en la fase actual.
/// </param>
/// <param name="Total">
/// Archivos que se procesarán en la fase actual.
/// </param>
/// <param name="CurrentFile">
/// Archivo que se procesa en este momento.
/// </param>
public record MiningProgress
(
    MiningPhase Phase,
    int Processed,
    int Total,
    string CurrentFile
);

/// <summary>
/// Resumen final del proceso de mining.
/// </summary>
/// <param name="Found">
/// Cantidad de MP3 encontrados en el directorio.
/// </param>
/// <param name="Inserted">
/// Cantidad de rolas nuevas insertadas en la BD.
/// </param>
/// <param name="Skipped">
/// Cantidad de rolas descartadas por ya estar en la BD.
/// </param>
/// <param name="Failed">
/// Cantidad de archivos cuyas etiquetas no se pudieron leer.
/// </param>
/// <param name="Cancelled">
/// Si el proceso fue cancelado antes de terminar.
/// </param>
/// <param name="Errors">
/// Mensajes de error de lectura, uno por archivo fallido.
/// </param>
public record MiningResult
(
    int Found,
    int Inserted,
    int Skipped,
    int Failed,
    bool Cancelled,
    IReadOnlyList<string> Errors
);