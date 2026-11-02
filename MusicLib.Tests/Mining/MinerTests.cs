using Microsoft.Data.Sqlite;
using MusicLib.Data;
using MusicLib.Mining;
using MusicLib.Models;
using MusicLib.Tests.Support;
using Xunit;

namespace MusicLib.Tests.Mining;

/// <summary>
/// Pruebas del Miner con IFileScanner e ITagReader falsos y una BD SQLite real
/// en un directorio temporal. Los archivos "existen" solo en el escáner falso.
/// </summary>
public sealed class MinerTests : IDisposable
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    private const string RolaJoin =
        "SELECT r.title, p.name, a.name, r.track, r.year, r.genre, r.id_album, a.path " +
        "FROM rolas r " +
        "JOIN performers p ON p.id_performer = r.id_performer " +
        "JOIN albums a ON a.id_album = r.id_album " +
        "WHERE r.path = $p0";

    private readonly TempDir _tmp = new();
    private readonly string _db;
    private readonly string _root;
    private readonly FakeFileScanner _scanner = new();
    private readonly FakeTagReader _reader = new();
    private readonly Miner _miner;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MinerTests()
    {
        _db = _tmp.Combine("music.db");
        _root = _tmp.Combine("Music");
        DatabaseInitializer.EnsureCreated(_db);
        _miner = new Miner(_db, _scanner, _reader);
    }

    /// <summary>
    /// Cumplir con el contrato para eliminar los datos temporales.
    /// </summary>
    public void Dispose() => _tmp.Dispose();

    // ------------------------------------------------------------ helpers

    private string P(params string[] parts) =>
        Path.Combine(new[] { _root }.Concat(parts).ToArray());

    private static TagInfo Tag
    (
        string? title, string? performer, string? album,
        int track = 1, int year = 2000, string? genre = "Rock",
        string? albumArtist = null
    ) => new(title, performer, albumArtist, album, track, year, genre);

    /// <summary>
    /// Registra un archivo falso (ruta relativa con '/') y devuelve su ruta.
    /// </summary>
    private string Add(string relative, TagInfo tag)
    {
        var path = P(relative.Split('/'));
        _scanner.Files.Add(path);
        _reader.Set(path, tag);
        return path;
    }

    /// <summary>
    /// 6 archivos: 3 intérpretes distintos, 2 álbumes y una rola suelta sin álbum.
    /// </summary>
    private void SeedLibrary()
    {
        Add("Rock/Disco A/01.mp3", Tag("Uno", "Banda Uno", "Disco A", 1, 1990, "Rock"));
        Add("Rock/Disco A/02.mp3", Tag("Dos", "Banda Uno", "Disco A", 2, 1990, "Rock"));
        Add("Rock/Disco A/03.mp3", Tag("Tres", "Banda Uno, Invitada", "Disco A", 3, 1990, "Rock"));
        Add("Pop/Disco B/01.mp3", Tag("Cuatro", "Cantante Dos", "Disco B", 1, 2005, "Pop"));
        Add("Pop/Disco B/02.mp3", Tag("Cinco", "Cantante Dos", "Disco B", 2, 2005, "Pop"));
        Add("Sueltas/suelta.mp3", Tag("Seis", "Banda Uno", null, 0, 0, null));
    }

    private int Rolas => Db.Count(_db, "rolas");
    private int Performers => Db.Count(_db, "performers");
    private int Albums => Db.Count(_db, "albums");

    // =====================================================================
    // Mina bien un set de datos
    // =====================================================================

    /// <summary>
    /// Inserta correctamente.
    /// </summary>
    /// <returns>
    /// null
    /// </returns>
    [Fact]
    public async Task MineAsync_FakeLibrary_InsertsEverythingCorrectly()
    {
        SeedLibrary();

        var result = await _miner.MineAsync(_root);

        Assert.Equal(6, result.Found);
        Assert.Equal(6, result.Inserted);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.Failed);
        Assert.False(result.Cancelled);
        Assert.Empty(result.Errors);

        Assert.Equal(6, Rolas);
        Assert.Equal(3, Performers);   // Banda Uno, Banda Uno, Invitada, Cantante Dos
        Assert.Equal(3, Albums);       // Unknown, Disco A, Disco B

        // Una rola con todos los datos, tal cual venían en las etiquetas.
        var row = Db.Row(_db, RolaJoin, P("Rock", "Disco A", "03.mp3"));
        Assert.Equal("Tres", row[0]);
        Assert.Equal("Banda Uno, Invitada", row[1]);
        Assert.Equal("Disco A", row[2]);
        Assert.Equal(3L, row[3]);
        Assert.Equal(1990L, row[4]);
        Assert.Equal("Rock", row[5]);
        Assert.Equal(P("Rock", "Disco A"), row[7]);   // el álbum guarda su directorio
    }

    /// <summary>
    /// Registra a álbum desconocido.
    /// </summary>
    /// <returns>
    /// null
    /// </returns>
    [Fact]
    public async Task MineAsync_SongWithoutAlbum_GoesToTheUnknownAlbum()
    {
        SeedLibrary();

        await _miner.MineAsync(_root);

        var row = Db.Row(_db, RolaJoin, P("Sueltas", "suelta.mp3"));
        Assert.Equal(Album.UnknownId, row[6]);
        Assert.Equal("Unknown", row[2]);
        Assert.Equal("Unknown", row[5]);
    }

    /// <summary>
    /// No mina nada.
    /// </summary>
    /// <returns>
    /// null
    /// </returns>
    [Fact]
    public async Task MineAsync_EmptyDirectory_ReturnsEmptyResult()
    {
        var result = await _miner.MineAsync(_root);

        Assert.Equal(0, result.Found);
        Assert.Equal(0, result.Inserted);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.Failed);
        Assert.False(result.Cancelled);
        Assert.Equal(0, Rolas);
    }

    /// <summary>
    /// Reporta el progreso correctamente.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_ReportsProgressForBothPhases()
    {
        SeedLibrary();
        var progress = new SyncProgress<MiningProgress>();

        await _miner.MineAsync(_root, progress);

        var all = progress.Reports;
        var reading = all.Where(p => p.Phase == MiningPhase.Reading).ToList();
        var saving = all.Where(p => p.Phase == MiningPhase.Saving).ToList();

        // Primero toda la lectura y luego todo el guardado.
        Assert.Equal
        (
            Enumerable.Repeat(MiningPhase.Reading, 6)
                .Concat(Enumerable.Repeat(MiningPhase.Saving, 6)),
            all.Select(p => p.Phase)
        );
        Assert.Equal(Enumerable.Range(1, 6), reading.Select(p => p.Processed));
        Assert.Equal(Enumerable.Range(1, 6), saving.Select(p => p.Processed));
        Assert.All(all, p => Assert.Equal(6, p.Total));
        Assert.Equal(_scanner.Files, reading.Select(p => p.CurrentFile));
    }

    /// <summary>
    /// Comprueba si el minero no está corriendo.
    /// </summary>
    /// <returns>
    /// null
    /// </returns>
    [Fact]
    public async Task MineAsync_Idle_IsNotRunning()
    {
        Assert.False(_miner.IsRunning);

        SeedLibrary();
        await _miner.MineAsync(_root);

        Assert.False(_miner.IsRunning);
    }

    // =====================================================================
    // Minar dos veces lo mismo
    // =====================================================================

    /// <summary>
    /// Comprueba que no se leen los archivos ya leídos en
    /// una minería anterior (todos).
    /// </summary>
    /// <returns>
    /// null
    /// </returns>
    [Fact]
    public async Task MineAsync_SameDataTwice_SecondRunSkipsEverythingWithoutReading()
    {
        SeedLibrary();
        await _miner.MineAsync(_root);
        var readsAfterFirstRun = _reader.Reads.Count;
        var progress = new SyncProgress<MiningProgress>();

        var second = await _miner.MineAsync(_root, progress);

        Assert.Equal(6, second.Found);
        Assert.Equal(0, second.Inserted);
        Assert.Equal(6, second.Skipped);
        Assert.Equal(0, second.Failed);
        Assert.False(second.Cancelled);

        Assert.Equal(6, Rolas);
        Assert.Equal(3, Performers);
        Assert.Equal(3, Albums);

        // Lo ya minado se descarta sin abrir el archivo.
        Assert.Equal(readsAfterFirstRun, _reader.Reads.Count);
        Assert.DoesNotContain(progress.Reports, p => p.Phase == MiningPhase.Saving);
    }

    // =====================================================================
    // Minar dos veces con diferencias
    // =====================================================================

    /// <summary>
    /// Comprueba que no se leen los archivos ya leídos en
    /// una minería anterior. 
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_SecondRunWithChanges_InsertsOnlyTheNewFiles()
    {
        SeedLibrary();
        await _miner.MineAsync(_root);

        // Cambios entre corridas:
        //  - se quita suelta.mp3 del disco (el escáner ya no la ve)
        //  - se agrega una rola a un álbum existente y otra con intérprete y álbum nuevos
        //  - se cambian las etiquetas de un archivo ya minado CAMBIAR A FUTURO???
        var removed = P("Sueltas", "suelta.mp3");
        _scanner.Files.Remove(removed);
        Add("Rock/Disco A/04.mp3", Tag("Siete", "Banda Uno", "Disco A", 4, 1990, "Rock"));
        Add("Jazz/Disco C/01.mp3", Tag("Ocho", "Trío Tres", "Disco C", 1, 2020, "Jazz"));
        var changed = P("Rock", "Disco A", "01.mp3");
        _reader.Set(changed, Tag("Título cambiado", "Otro Intérprete", "Otro Álbum", 9, 1999, "Pop"));

        var second = await _miner.MineAsync(_root);

        Assert.Equal(7, second.Found);      // 5 viejas + 2 nuevas
        Assert.Equal(2, second.Inserted);
        Assert.Equal(5, second.Skipped);
        Assert.Equal(0, second.Failed);

        Assert.Equal(8, Rolas);             // 6 + 2
        Assert.Equal(4, Performers);        // + Trío Tres (Banda Uno se reutiliza)
        Assert.Equal(4, Albums);            // Unknown + A + B + C
        Assert.Equal
        (
            4,
            Db.Scalar<int>
            (
                _db,
                "SELECT COUNT(*) FROM rolas r JOIN albums a ON a.id_album = r.id_album " +
                "WHERE a.name = 'Disco A'"
            )
        );

        // Lo ya minado NO se actualiza aunque cambien sus etiquetas... CAMBIAR
        Assert.Equal("Uno", Db.Scalar<string>(_db, "SELECT title FROM rolas WHERE path = $p0", changed));
        // ...y lo que desapareció del disco NO se borra de la BD.
        Assert.Equal(1, Db.Scalar<int>(_db, "SELECT COUNT(*) FROM rolas WHERE path = $p0", removed));
    }

    // =====================================================================
    // Varios archivos de los mismos intérpretes y álbumes
    // =====================================================================

    /// <summary>
    /// Se registra un solo intérprete.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_SamePerformerAcrossAlbums_CreatesSinglePerformer()
    {
        Add("A/01.mp3", Tag("a1", "Banda Uno", "Álbum A"));
        Add("B/01.mp3", Tag("b1", "Banda Uno", "Álbum B"));
        Add("C/01.mp3", Tag("c1", "Banda Uno", null));

        await _miner.MineAsync(_root);

        Assert.Equal(1, Performers);
        Assert.Equal(3, Rolas);
        Assert.Equal(1, Db.Scalar<int>(_db, "SELECT COUNT(DISTINCT id_performer) FROM rolas"));
    }

    /// <summary>
    /// Mismo nombre de álbum y mismo directorio llevan al mismo álbum en la DB.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_SameAlbumNameInSameDirectory_SharesOneAlbum()
    {
        Add("Disco/01.mp3", Tag("t1", "Banda Uno", "Éxitos", 1));
        Add("Disco/02.mp3", Tag("t2", "Banda Uno", "Éxitos", 2));
        Add("Disco/03.mp3", Tag("t3", "Cantante Dos", "Éxitos", 3));

        await _miner.MineAsync(_root);

        Assert.Equal(2, Albums);   // Unknown + Éxitos
        Assert.Equal(1, Db.Scalar<int>(_db, "SELECT COUNT(DISTINCT id_album) FROM rolas"));
    }

    /// <summary>
    /// Chequea que dos directorios distintos con el mismo álbum produce dos álbumes
    /// distintos en la BD, y que sin importar el autor si se tiene el mismo nombre
    /// y directorio, se genera un solo álbum.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_SameAlbumNameInDifferentDirectories_CreatesOneAlbumPerDirectory()
    {
        Add("Carpeta1/01.mp3", Tag("t1", "Banda Uno", "Éxitos", 1));
        Add("Carpeta1/02.mp3", Tag("t2", "Banda Uno", "Éxitos", 2));
        Add("Carpeta2/01.mp3", Tag("t1", "Banda Uno", "Éxitos", 1));

        await _miner.MineAsync(_root);

        Assert.Equal(3, Albums);   // Unknown + Éxitos@Carpeta1 + Éxitos@Carpeta2
        Assert.Equal(2, Db.Scalar<int>(_db, "SELECT COUNT(*) FROM albums WHERE name = 'Éxitos'"));
        Assert.Equal(1, Performers);

        var album1 = Db.Scalar<long>
        (
            _db,
            "SELECT id_album FROM rolas WHERE path = $p0",
            P("Carpeta1", "01.mp3")
        );
        var album1b = Db.Scalar<long>
        (
            _db,
            "SELECT id_album FROM rolas WHERE path = $p0",
            P("Carpeta1", "02.mp3")
        );
        var album2 = Db.Scalar<long>
        (
            _db,
            "SELECT id_album FROM rolas WHERE path = $p0",
            P("Carpeta2", "01.mp3")
        );
        Assert.Equal(album1, album1b);
        Assert.NotEqual(album1, album2);
    }

    /// <summary>
    /// Se hace Trim a los metadatos.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_PerformerAndAlbumDifferingOnlyByWhitespace_AreMerged()
    {
        Add("D/01.mp3", Tag("t1", "Banda Uno", "Éxitos"));
        Add("D/02.mp3", Tag("t2", "  Banda Uno  ", "  Éxitos  "));

        await _miner.MineAsync(_root);

        Assert.Equal(1, Performers);
        Assert.Equal(2, Albums);   // Unknown + Éxitos
    }

    // =====================================================================
    // Archivos sin tags o con metadatos vacíos
    // =====================================================================

    /// <summary>
    /// Rola sin metadatos.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_FileWithNullTags_UsesFallbackValues()
    {
        var path = Add("Sueltas/mi_rola.mp3", new TagInfo(null, null, null, null, 0, 0, null));

        var result = await _miner.MineAsync(_root);

        Assert.Equal(1, result.Inserted);
        Assert.Equal(0, result.Failed);

        var row = Db.Row(_db, RolaJoin, path);
        Assert.Equal("mi_rola", row[0]);                 // título = nombre de archivo sin extensión
        Assert.Equal(Performer.UnknownName, row[1]);     // intérprete desconocido
        Assert.Equal(Album.UnknownId, row[6]);           // álbum desconocido
        Assert.Equal(0L, row[3]);
        Assert.Equal(0L, row[4]);
        Assert.Equal("Unknown", row[5]);

        var type = Db.Scalar<long>(_db, "SELECT id_type FROM performers");
        Assert.Equal((long)PerformerType.Unknown, type);
        Assert.Equal(1, Performers);
        Assert.Equal(1, Albums);   // no se crea otro álbum 'Unknown'
    }

    /// <summary>
    /// Limpia carácteres raros y hace trim.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_FileWithEmptyAndWhitespaceTags_UsesFallbackValues()
    {
        var path = Add("Sueltas/vacia.mp3", new TagInfo("", "   ", " \t ", "  ", 0, 0, " "));

        await _miner.MineAsync(_root);

        var row = Db.Row(_db, RolaJoin, path);
        Assert.Equal("vacia", row[0]);
        Assert.Equal(Performer.UnknownName, row[1]);
        Assert.Equal(Album.UnknownId, row[6]);
        Assert.Equal("Unknown", row[5]);
        Assert.Equal(1, Albums);
    }

    /// <summary>
    /// Compruba que dos rolas con el mismo albumArtist pero con diferentes
    /// intérpretes aparecen en diferentes álbumes y que se sustituye el
    /// albumArtist si no hay un intérprete válido.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_AlbumArtistIsOnlyUsedWhenPerformerIsMissing()
    {
        var onlyAlbumArtist = Add("X/01.mp3", Tag("t1", null, "Orquestal", albumArtist: "Orquesta Ñ"));
        var both = Add("X/02.mp3", Tag("t2", "Solista", "Orquestal", albumArtist: "Orquesta Ñ"));

        await _miner.MineAsync(_root);

        Assert.Equal("Orquesta Ñ", Db.Row(_db, RolaJoin, onlyAlbumArtist)[1]);
        Assert.Equal("Solista", Db.Row(_db, RolaJoin, both)[1]);
    }

    /// <summary>
    /// Más trimming xd.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_TagValuesAreTrimmed()
    {
        var path = Add("T/01.mp3", Tag("  Título  ", "  Banda  ", "  Álbum  ", 1, 2000, "  Rock  "));

        await _miner.MineAsync(_root);

        var row = Db.Row(_db, RolaJoin, path);
        Assert.Equal("Título", row[0]);
        Assert.Equal("Banda", row[1]);
        Assert.Equal("Álbum", row[2]);
        Assert.Equal("Rock", row[5]);
    }

    // =====================================================================
    // Cancelar a la mitad del proceso
    // =====================================================================

    /// <summary>
    /// Cancela correctamente mientras lee.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_CancelledWhileReading_WritesNothing()
    {
        for (int i = 1; i <= 5; i++)
            Add($"Lib/0{i}.mp3", Tag($"t{i}", "Banda Uno", "Disco"));
        using var cts = new CancellationTokenSource();
        var progress = new SyncProgress<MiningProgress>
        (
            p =>
            {
                if (p.Phase == MiningPhase.Reading && p.Processed == 2) cts.Cancel();
            }
        );

        var result = await _miner.MineAsync(_root, progress, cts.Token);

        Assert.True(result.Cancelled);
        Assert.Equal(5, result.Found);
        Assert.Equal(0, result.Inserted);
        Assert.Equal(2, _reader.Reads.Count);   // paró justo después del 2º archivo
        Assert.Equal(0, Rolas);
        Assert.Equal(0, Performers);
        Assert.Equal(1, Albums);                // solo el 'Unknown' sembrado
        Assert.False(_miner.IsRunning);
    }

    /// <summary>
    /// Cancela correctamente mientras agrega en al DB.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_CancelledWhileSaving_KeepsWhatWasAlreadySaved()
    {
        for (int i = 1; i <= 5; i++)
            Add($"Lib/0{i}.mp3", Tag($"t{i}", $"Banda {i}", "Disco"));
        using var cts = new CancellationTokenSource();
        var progress = new SyncProgress<MiningProgress>
        (
            p =>
            {
                if (p.Phase == MiningPhase.Saving && p.Processed == 2) cts.Cancel();
            }
        );

        var result = await _miner.MineAsync(_root, progress, cts.Token);

        Assert.True(result.Cancelled);
        Assert.Equal(5, result.Found);
        Assert.Equal(2, result.Inserted);
        Assert.Equal(5, _reader.Reads.Count);   // la lectura sí la terminó completa
        Assert.Equal(2, Rolas);                 // lo guardado se conserva
        Assert.Equal(2, Performers);            // siempre archivos completos
        Assert.Equal(2, Albums);                // Unknown + Disco
        Assert.False(_miner.IsRunning);

        // Una corrida posterior completa lo que faltó.
        var resumed = await _miner.MineAsync(_root);
        Assert.False(resumed.Cancelled);
        Assert.Equal(3, resumed.Inserted);
        Assert.Equal(2, resumed.Skipped);
        Assert.Equal(5, Rolas);
    }

    // =====================================================================
    // Fallos esperados
    // =====================================================================

    /// <summary>
    /// Excepcion en lectura se ignora.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_UnreadableFile_IsCountedAsFailedAndTheRestContinues()
    {
        SeedLibrary();
        var bad = P("Rock", "Disco A", "02.mp3");
        _reader.Fail(bad, new InvalidDataException("etiquetas corruptas"));

        var result = await _miner.MineAsync(_root);

        Assert.Equal(6, result.Found);
        Assert.Equal(5, result.Inserted);
        Assert.Equal(1, result.Failed);
        Assert.False(result.Cancelled);

        var error = Assert.Single(result.Errors);
        Assert.Contains(bad, error);
        Assert.Contains("etiquetas corruptas", error);

        Assert.Equal(5, Rolas);
        Assert.Equal(0, Db.Scalar<int>(_db, "SELECT COUNT(*) FROM rolas WHERE path = $p0", bad));
    }

    /// <summary>
    /// No tiene problemas para minar un archivo que dió problemas anteriormente.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_FailedFile_IsRetriedOnTheNextRun()
    {
        SeedLibrary();
        var bad = P("Rock", "Disco A", "02.mp3");
        _reader.Fail(bad, new InvalidDataException("etiquetas corruptas"));
        await _miner.MineAsync(_root);

        _reader.Repair(bad);
        var second = await _miner.MineAsync(_root);

        Assert.Equal(1, second.Inserted);
        Assert.Equal(5, second.Skipped);
        Assert.Equal(0, second.Failed);
        Assert.Equal(6, Rolas);
    }

    /// <summary>
    /// Resultado de la minería refleja fallos.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_AllFilesUnreadable_ReturnsFailuresWithoutThrowing()
    {
        SeedLibrary();
        foreach (var file in _scanner.Files)
            _reader.Fail(file, new InvalidDataException("mal"));

        var result = await _miner.MineAsync(_root);

        Assert.Equal(6, result.Found);
        Assert.Equal(0, result.Inserted);
        Assert.Equal(6, result.Failed);
        Assert.Equal(6, result.Errors.Count);
        Assert.Equal(0, Rolas);
        Assert.Equal(0, Performers);
    }

    /// <summary>
    /// Manejo de fallo en la DB.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_DatabaseErrorWhileSaving_RollsBackTheWholeRunAndPropagates()
    {
        SeedLibrary();
        // La 3ª rola en guardarse hace fallar la BD.
        _reader.Set(P("Rock", "Disco A", "03.mp3"), Tag("BOOM", "Banda Uno", "Disco A", 3, 1990));
        Db.Exec
        (
            _db,
            "CREATE TRIGGER fail_on_boom BEFORE INSERT ON rolas WHEN NEW.title = 'BOOM' " +
            "BEGIN SELECT RAISE(ABORT, 'boom'); END;"
        );

        var ex = await Assert.ThrowsAsync<SqliteException>(() => _miner.MineAsync(_root));

        Assert.Contains("boom", ex.Message);
        // Nada de lo guardado antes del fallo sobrevive.
        Assert.Equal(0, Rolas);
        Assert.Equal(0, Performers);
        Assert.Equal(1, Albums);
        Assert.False(_miner.IsRunning);

        // El minero sigue usable: una vez arreglado el problema, todo entra.
        Db.Exec(_db, "DROP TRIGGER fail_on_boom");
        var retry = await _miner.MineAsync(_root);
        Assert.Equal(6, retry.Inserted);
        Assert.Equal(6, Rolas);
    }

    /// <summary>
    /// Se puede minar después de tratar de hacerlo en un directorio que no existía.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_ScannerFails_PropagatesAndReleasesTheGate()
    {
        _scanner.Failure = new DirectoryNotFoundException("la carpeta no existe");

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => _miner.MineAsync(P("noexiste")));

        Assert.False(_miner.IsRunning);
        Assert.Equal(0, Rolas);

        // Y se puede volver a minar.
        _scanner.Failure = null;
        SeedLibrary();
        var result = await _miner.MineAsync(_root);
        Assert.Equal(6, result.Inserted);
    }

    // =====================================================================
    // Evitar duplicados
    // =====================================================================

    /// <summary>
    /// Si el scanner entrega dos veces lo mismo se garantiza que no se
    /// incluye dos veces.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_SamePathReturnedTwiceByTheScanner_IsInsertedOnce()
    {
        var path = Add("Dup/01.mp3", Tag("Uno", "Banda Uno", "Disco"));
        _scanner.Files.Add(path);   // el escáner la entrega dos veces

        var result = await _miner.MineAsync(_root);

        Assert.Equal(2, result.Found);
        Assert.Equal(1, result.Inserted);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(1, Rolas);
        Assert.Equal(1, Performers);
    }

    /// <summary>
    /// No duplica elementos en la DB la minería.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_RepeatedRuns_NeverDuplicateRowsInAnyTable()
    {
        SeedLibrary();

        for (int i = 0; i < 3; i++) await _miner.MineAsync(_root);

        Assert.Equal(6, Rolas);
        Assert.Equal(3, Performers);
        Assert.Equal(3, Albums);
        Assert.Equal(6, Db.Scalar<int>(_db, "SELECT COUNT(DISTINCT path) FROM rolas"));
    }

    // =====================================================================
    // Minar dos veces al mismo tiempo: la 2da falla
    // =====================================================================

    /// <summary>
    /// Comprobar que una segunda minería no ocurre sino hasta  que termina la primera.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_WhileAnotherMiningIsRunning_SecondCallFailsAndFirstCompletes()
    {
        SeedLibrary();
        using var started = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        // La 1ª lectura se queda bloqueada hasta que la prueba la libere.
        _reader.OnRead = _ =>
        {
            started.Set();
            release.Wait(Wait);
        };

        var first = _miner.MineAsync(_root);
        Assert.True(started.Wait(Wait), "La primera minería nunca empezó a leer.");
        Assert.True(_miner.IsRunning);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _miner.MineAsync(_root));
        Assert.Contains("en curso", ex.Message);
        Assert.True(_miner.IsRunning);   // la 2ª fallida no le quitó el candado a la 1ª

        release.Set();
        var result = await first;

        Assert.Equal(6, result.Inserted);
        Assert.False(result.Cancelled);
        Assert.Equal(6, Rolas);
        Assert.False(_miner.IsRunning);
    }

    /// <summary>
    /// Comprueba que se puede minar después de fallar al intentar minar mientras
    /// ya había una un curso.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task MineAsync_AfterConcurrentAttemptFinishes_CanMineAgain()
    {
        SeedLibrary();
        using var started = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        _reader.OnRead = _ =>
        {
            started.Set();
            release.Wait(Wait);
        };
        var first = _miner.MineAsync(_root);
        Assert.True(started.Wait(Wait));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _miner.MineAsync(_root));
        release.Set();
        await first;

        var again = await _miner.MineAsync(_root);

        Assert.Equal(0, again.Inserted);
        Assert.Equal(6, again.Skipped);
        Assert.Equal(6, Rolas);
    }
}