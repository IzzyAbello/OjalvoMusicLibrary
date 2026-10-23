using System.Security.Cryptography;
using MusicLib.Data;
using MusicLib.Mining;
using MusicLib.Models;
using MusicLib.Tests.Support;
using Xunit;

namespace MusicLib.Tests.Mining;

/// <summary>
/// Miner + FileScanner + TagReader REALES + SQLite real, sobre copias de los MP3
/// de Dataset/Generated. Se trabaja sobre copias para que las rutas guardadas en
/// la BD sean las del directorio temporal.
/// </summary>
public sealed class MinerIntegrationTests : IDisposable
{
    private readonly TempDir _tmp = new();
    private readonly string _db;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MinerIntegrationTests()
    {
        _db = _tmp.Combine("music.db");
        DatabaseInitializer.EnsureCreated(_db);
    }

    /// <summary>
    /// Cumplir el contrato con IDisposable para eliminar los datos temporales.
    /// </summary>
    public void Dispose() => _tmp.Dispose();

    private Miner NewMiner() => new(_db, new FileScanner(), new TagReader());

    private static Dictionary<string, string> Hashes(string dir) => Directory
        .EnumerateFiles(dir, "*", SearchOption.AllDirectories)
        .ToDictionary(f => f, f => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f))));

    private const string RolaJoin =
        "SELECT r.title, p.name, a.name, r.id_album, r.track, r.year, r.genre " +
        "FROM rolas r " +
        "JOIN performers p ON p.id_performer = r.id_performer " +
        "JOIN albums a ON a.id_album = r.id_album " +
        "WHERE r.path = $p0";

    // =====================================================================

    /// <summary>
    /// Comportamiento normal.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Mine_RealAlbumsDataset_InsertsExpectedRowsAndDoesNotTouchTheMp3()
    {
        var root = _tmp.CopyDirectory(Dataset.At("Generated", "Albums"), "Albums");
        var before = Hashes(root);

        var result = await NewMiner().MineAsync(root);

        Assert.Equal(6, result.Found);
        Assert.Equal(6, result.Inserted);
        Assert.Equal(0, result.Failed);
        Assert.Empty(result.Errors);

        Assert.Equal(6, Db.Count(_db, "rolas"));
        // Artista Uno, Artista Dos y el desconocido (sin_tags.mp3).
        Assert.Equal(3, Db.Count(_db, "performers"));
        // Unknown + "Disco Uno" en dos directorios distintos.
        Assert.Equal(3, Db.Count(_db, "albums"));
        Assert.Equal(2, Db.Scalar<int>(_db, "SELECT COUNT(*) FROM albums WHERE name = 'Disco Uno'"));
        Assert.Equal(2, Db.Scalar<int>(_db, "SELECT COUNT(DISTINCT path) FROM albums WHERE name = 'Disco Uno'"));

        // Las 3 rolas de "Disco Uno" comparten álbum; la de "(copia)" tiene otro.
        var ids = new[] { "01 Primera.mp3", "02 Segunda.mp3", "03 Tercera.mp3" }
            .Select(f => Db.Scalar<long>(_db, "SELECT id_album FROM rolas WHERE path = $p0",
                Path.Combine(root, "Disco Uno", f)))
            .ToList();
        var copy = Db.Scalar<long>(_db, "SELECT id_album FROM rolas WHERE path = $p0",
            Path.Combine(root, "Disco Uno (copia)", "01 Primera.mp3"));
        Assert.Single(ids.Distinct());
        Assert.NotEqual(ids[0], copy);

        var full = Db.Row(_db, RolaJoin, Path.Combine(root, "Disco Uno", "02 Segunda.mp3"));
        Assert.Equal("Segunda", full[0]);
        Assert.Equal("Artista Uno", full[1]);
        Assert.Equal("Disco Uno", full[2]);
        Assert.Equal(2L, full[4]);
        Assert.Equal(1999L, full[5]);
        Assert.Equal("Rock", full[6]);

        var noAlbum = Db.Row(_db, RolaJoin, Path.Combine(root, "Sueltas", "sin_album.mp3"));
        Assert.Equal("Artista Dos", noAlbum[1]);
        Assert.Equal(Album.UnknownId, noAlbum[3]);

        var noTags = Db.Row(_db, RolaJoin, Path.Combine(root, "Sueltas", "sin_tags.mp3"));
        Assert.Equal("sin_tags", noTags[0]);
        Assert.Equal(Performer.UnknownName, noTags[1]);
        Assert.Equal(Album.UnknownId, noTags[3]);
        Assert.Equal("Unknown", noTags[6]);

        Assert.Equal(before, Hashes(root));   // ningún MP3 cambió
    }

    /// <summary>
    /// Segunda ronda no inserta nada nuevo.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Mine_RealAlbumsDatasetTwice_SecondRunInsertsNothing()
    {
        var root = _tmp.CopyDirectory(Dataset.At("Generated", "Albums"), "Albums");
        var miner = NewMiner();
        await miner.MineAsync(root);
        var before = Hashes(root);

        var second = await miner.MineAsync(root);

        Assert.Equal(6, second.Found);
        Assert.Equal(0, second.Inserted);
        Assert.Equal(6, second.Skipped);
        Assert.Equal(6, Db.Count(_db, "rolas"));
        Assert.Equal(3, Db.Count(_db, "performers"));
        Assert.Equal(3, Db.Count(_db, "albums"));
        Assert.Equal(before, Hashes(root));
    }

    /// <summary>
    /// Manejo de fallos.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Mine_RealCorruptFile_IsCountedAsFailedAndTheRestIsInserted()
    {
        var root = _tmp.CopyDirectory(Dataset.At("Generated", "Albums"), "Albums");
        var corrupt = Path.Combine(root, "corrupt.mp3");
        File.Copy(Dataset.At("Generated", "Corrupt", "corrupt.mp3"), corrupt);

        var result = await NewMiner().MineAsync(root);

        Assert.Equal(7, result.Found);
        Assert.Equal(6, result.Inserted);
        Assert.Equal(1, result.Failed);
        var error = Assert.Single(result.Errors);
        Assert.Contains("corrupt.mp3", error);
        Assert.Equal(0, Db.Scalar<int>(_db, "SELECT COUNT(*) FROM rolas WHERE path = $p0", corrupt));
    }

    /// <summary>
    /// Round-trip y caracteres especiales en la integración.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Mine_RealUnicodeTags_RoundTripThroughTheDatabase()
    {
        var root = _tmp.CopyDirectory(Dataset.At("Generated", "Tags"), "Tags");

        var result = await NewMiner().MineAsync(root);

        Assert.Equal(9, result.Found);
        Assert.Equal(9, result.Inserted);
        Assert.Equal(0, result.Failed);

        // UTF-8 (v2.4) y UTF-16 (v2.3) con el mismo texto, mismo intérprete y álbum.
        foreach (var file in new[] { "utf8_v24.mp3", "utf16_v23.mp3" })
        {
            var row = Db.Row(_db, RolaJoin, Path.Combine(root, file));
            Assert.Equal("Mañana será otro día: ¿qué tal? — 日本語", row[0]);
            Assert.Equal("Niño Pingüino", row[1]);
            Assert.Equal("Año de las Cigüeñas", row[2]);
            Assert.Equal("Música Tradicional", row[6]);
        }
        Assert.Equal(1, Db.Scalar<int>(_db, "SELECT COUNT(*) FROM performers WHERE name = 'Niño Pingüino'"));

        var latin1 = Db.Row(_db, RolaJoin, Path.Combine(root, "latin1_v23.mp3"));
        Assert.Equal("Cañón ñandú ¿Qué tal? ¡Sí!", latin1[0]);
        Assert.Equal("Los Niños Ñoños", latin1[1]);

        var multi = Db.Row(_db, RolaJoin, Path.Combine(root, "multi_performers_v24.mp3"));
        Assert.Equal("Ximena Peña, Joaquín Núñez, Lola Ibáñez", multi[1]);

        var albumArtist = Db.Row(_db, RolaJoin, Path.Combine(root, "album_artist_only_v24.mp3"));
        Assert.Equal("Orquesta Ñ", albumArtist[1]); 

        foreach (var file in new[] { "no_tags.mp3", "empty_metadata_v24.mp3" })
        {
            var row = Db.Row(_db, RolaJoin, Path.Combine(root, file));
            Assert.Equal(Performer.UnknownName, row[1]);
            Assert.Equal(Album.UnknownId, row[3]);
            Assert.Equal(Path.GetFileNameWithoutExtension(file), row[0]);
        }
    }
}
