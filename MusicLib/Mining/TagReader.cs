namespace MusicLib.Mining;

/// <summary>
/// Lector de tags basado en TagLib#. Abre los archivos SOLO para lectura:
/// la abstracción de archivo lanza excepción si alguien intenta escribir.
/// </summary>
public sealed class TagReader : ITagReader
{
    /// <summary>
    /// Método de lectura.
    /// </summary>
    /// <param name="path">
    /// Ruta del archivo MP3.
    /// </param>
    /// <returns>
    /// Metadatos del archivo MP3.
    /// </returns>
    public TagInfo Read(string path)
    {
        using var file = TagLib.File.Create(new ReadOnlyFileAbstraction(path));
        var tag = file.Tag;

        return new TagInfo
        (
            Title: tag.Title,
            Performer: Join(tag.Performers),
            AlbumArtist: Join(tag.AlbumArtists),
            Album: tag.Album,
            Track: (int)Math.Min(tag.Track, int.MaxValue),
            Year: (int)Math.Min(tag.Year, int.MaxValue),
            Genre: Join(tag.Genres)
        );
    }

    private static string? Join(string[]? values) =>
        values is { Length: > 0 } ? string.Join(", ", values) : null;

    private sealed class ReadOnlyFileAbstraction : TagLib.File.IFileAbstraction
    {
        private readonly string _path;

        public ReadOnlyFileAbstraction(string path) => _path = path;

        public string Name => _path;

        public Stream ReadStream =>
            new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);

        public Stream WriteStream =>
            throw new NotSupportedException("Los archivos MP3 son de solo lectura.");

        public void CloseStream(Stream stream) => stream.Dispose();
    }
}
