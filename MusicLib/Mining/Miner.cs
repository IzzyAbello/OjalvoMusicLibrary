using MusicLib.Data;
using MusicLib.Models;

namespace MusicLib.Mining;

/// <summary>
/// Una instancia vive toda la app y se reutiliza en cada pulsación de "Minar".
/// Supone que la BD ya fue preparada con DatabaseInitializer.EnsureCreated.
///
/// Trabaja en dos fases:
/// 1) Recorre los MP3 del directorio. Por cada uno CONSULTA a la BD si su ruta
///    ya existe; si existe lo descarta sin abrirlo, y si no, lee sus etiquetas.
///    No escribe nada en la BD.
/// 2) Inserta en la BD lo leído, consultando también intérprete y álbum.
/// Nunca guarda copias de datos de la BD en memoria.
///
/// Atomicidad:
/// - Un archivo se guarda completo o no se guarda.
/// - Un error de la BD durante el guardado deshace TODA la corrida.
/// - Cancelar conserva lo ya guardado (siempre archivos completos).
/// </summary>
public sealed class Miner
{
    private readonly string _dbPath;
    private readonly IFileScanner _scanner;
    private readonly ITagReader _tagReader;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Constructor del minero.
    /// </summary>
    /// <param name="dbPath">
    /// Ruta a la base de datos.
    /// </param>
    /// <param name="scanner">
    /// Lector de archivos.
    /// </param>
    /// <param name="tagReader">
    /// Lector de etiquetas.
    /// </param>
    public Miner(string dbPath, IFileScanner scanner, ITagReader tagReader)
    {
        _dbPath = dbPath;
        _scanner = scanner;
        _tagReader = tagReader;
    }

    /// <summary>
    /// Checa si hay una minería en curso.
    /// </summary>
    public bool IsRunning => _gate.CurrentCount == 0;

    /// <summary>
    /// Mina archivos MP3 de forma asíncrona (fuera del hilo que lo invoca).
    /// </summary>
    /// <param name="root">
    /// Directorio donde se comienza a minar (recursivo).
    /// </param>
    /// <param name="progress">
    /// Notificar el progreso actual.
    /// </param>
    /// <param name="ct">
    /// Token de cancelación. Si se cancela durante la lectura no se escribe
    /// nada en la BD; si se cancela durante el guardado se conserva lo ya
    /// insertado.
    /// </param>
    /// <returns>
    /// Resultado final del proceso de mining.
    /// </returns>
    /// <remarks>
    /// Un archivo cuyas etiquetas no se pueden leer se cuenta como fallido y
    /// la corrida sigue. En cambio, cualquier error de la BD durante el
    /// guardado que no es culpa de un archivo deshace toda la corrida,
    /// la BD queda como estaba y la excepción se propaga.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Si ya hay una minería en curso.
    /// </exception>
    public async Task<MiningResult> MineAsync
    (
        string root,
        IProgress<MiningProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        if (!await _gate.WaitAsync(0, ct).ConfigureAwait(false))
            throw new InvalidOperationException("Ya hay una minería en curso.");

        try
        {
            // CancellationToken.None: la cancelación se atiende dentro de Run
            // para devolver un resultado normal en lugar de una excepción.
            return await Task.Run
            (
                () => Run(root, progress, ct), CancellationToken.None
            ).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private MiningResult Run
    (
        string root,
        IProgress<MiningProgress>? progress,
        CancellationToken ct
    )
    {
        var errors = new List<string>();
        int failed = 0, skipped = 0;

        using var uow = new UnitOfWork(_dbPath);

        // ---- Fase 1: encontrar, descartar los ya existentes y leer el resto ----
        var files = _scanner.EnumerateMp3(root).ToList();
        var mined = new List<(string Path, TagInfo Tag)>();
        int visited = 0;

        foreach (var file in files)
        {
            if (ct.IsCancellationRequested)
                return new MiningResult
                    (
                        files.Count,
                        0,
                        skipped,
                        failed,
                        true,
                        errors
                    );

            if (uow.Rolas.ExistsByPath(file)) skipped++;
            else
            {
                try
                {
                    mined.Add((file, _tagReader.Read(file)));
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"{file}: {ex.Message}");
                }
            }

            visited++;
            progress?.Report
            (
                new MiningProgress
                (MiningPhase.Reading, visited, files.Count, file)
            );
        }

        // ---- Fase 2: guardar lo leído, con el candado de escritura tomado ----
        uow.Begin();

        int inserted = 0, processed = 0;
        bool cancelled = false;

        foreach (var (file, tag) in mined)
        {
            if (ct.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }

            if (uow.Rolas.ExistsByPath(file)) skipped++;
            else
            {
                Save(uow, file, tag);
                inserted++;
            }

            processed++;
            progress?.Report
            (
                new MiningProgress
                (MiningPhase.Saving, processed, mined.Count, file)
            );
        }

        uow.Commit();   // Al cancelar lo ya insertado se conserva
        return new MiningResult
        (
            files.Count, inserted, skipped, failed, cancelled, errors
        );
    }

    private static void Save(UnitOfWork uow, string file, TagInfo tag)
    {
        var performerName = Clean(tag.Performer)
            ?? Clean(tag.AlbumArtist) ?? Performer.UnknownName;

        long idPerformer = uow.Performers.FindByName(performerName)?.Id
            ?? uow.Performers.Insert
            (
                new Performer
                {
                    Name = performerName,
                    Type = PerformerType.Unknown
                }
            );

        long idAlbum = Album.UnknownId;
        var albumName = Clean(tag.Album);
        if (albumName != null)
        {
            var dir = Path.GetDirectoryName(file) ?? "";
            idAlbum = uow.Albums.FindByNameAndPath(albumName, dir)?.Id
                ?? uow.Albums.Insert
                (
                    new Album
                    {
                        Name = albumName,
                        Path = dir,
                        Year = tag.Year
                    }
                );
        }

        uow.Rolas.Insert
        (
            new Rola
            {
                IdPerformer = idPerformer,
                IdAlbum = idAlbum,
                Path = file,
                Title = Clean(tag.Title) ?? Path.GetFileNameWithoutExtension(file),
                Track = tag.Track,
                Year = tag.Year,
                Genre = Clean(tag.Genre) ?? "Unknown"
            }
        );
    }

    private static string? Clean(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}