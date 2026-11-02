using System.Security.Cryptography;
using MusicLib.Mining;
using MusicLib.Tests.Support;
using Xunit;

namespace MusicLib.Tests.Mining;

/// <summary>
/// Pruebas del lector REAL (TagLib#) contra los MP3 de Dataset/Generated/Tags.
/// Los valores esperados vienen de Dataset/_generate_dataset.py: si cambias uno
/// cambia el otro.
/// </summary>
public sealed class TagReaderTests : IDisposable
{
    // Textos con ñ, ü, ¿, ¡, raya larga y kanji (fuera de Latin-1 a propósito).
    private const string Utf8Title = "Mañana será otro día: ¿qué tal? — 日本語";
    private const string Utf8Performer = "Niño Pingüino";
    private const string Utf8Album = "Año de las Cigüeñas";
    private const string Utf8Genre = "Música Tradicional";

    private readonly TempDir _tmp = new();
    private readonly TagReader _reader = new();

    /// <summary>
    /// Cumplir en contrato de IDisposable eliminando los datos temporales.
    /// </summary>
    public void Dispose() => _tmp.Dispose();

    private static string Tags(string file) => Dataset.At("Generated", "Tags", file);

    private static IEnumerable<string> AllTagFixtures() =>
        Directory.GetFiles(Dataset.At("Generated", "Tags"), "*.mp3");

    private sealed record Snapshot(string Hash, long Length, DateTime LastWriteUtc);

    private static Snapshot Snap(string path) => new
    (
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),
        new FileInfo(path).Length,
        File.GetLastWriteTimeUtc(path)
    );

    // =====================================================================
    // Archivo normal con todos los metadatos
    // =====================================================================

    /// <summary>
    /// Lee correctamente metadatos.
    /// </summary>
    [Fact]
    public void Read_FullTags_ReturnsAllMetadata()
    {
        var info = _reader.Read(Tags("full_v24.mp3"));

        Assert.Equal("Canción de Prueba", info.Title);
        Assert.Equal("Artista Uno", info.Performer);
        Assert.Equal("Artista Uno", info.AlbumArtist);
        Assert.Equal("Álbum de Prueba", info.Album);
        Assert.Equal(3, info.Track);    // "3/12" → 3
        Assert.Equal(1993, info.Year);
        Assert.Equal("Cumbia", info.Genre);
    }

    // =====================================================================
    // Varios intérpretes
    // =====================================================================

    /// <summary>
    /// Lee correctamente intérpretes unidos por comas.
    /// </summary>
    [Fact]
    public void Read_MultiplePerformers_AreJoinedWithCommaAndSpace()
    {
        var info = _reader.Read(Tags("multi_performers_v24.mp3"));

        Assert.Equal("Ximena Peña, Joaquín Núñez, Lola Ibáñez", info.Performer);
        Assert.Equal(3, info.Performer!.Split(", ").Length);
        Assert.Equal("Trío de Colaboraciones", info.Title);
        Assert.Equal(2, info.Track);    // "2/9" → 2
        Assert.Equal(2015, info.Year);
    }

    /// <summary>
    /// Deja vacío los metadatos que no conoce.
    /// </summary>
    [Fact]
    public void Read_OnlyAlbumArtist_LeavesPerformerEmptyAndFillsAlbumArtist()
    {
        var info = _reader.Read(Tags("album_artist_only_v24.mp3"));

        Assert.True(string.IsNullOrWhiteSpace(info.Performer));
        Assert.Equal("Orquesta Ñ", info.AlbumArtist);
        Assert.Equal("Sin intérprete", info.Title);
    }

    // =====================================================================
    // UTF-8 y demás codificaciones
    // =====================================================================

    /// <summary>
    /// Decodificado en UTF-8
    /// </summary>
    /// <param name="file"></param>
    [Theory]
    [InlineData("utf8_v24.mp3")]    // ID3v2.4, texto en UTF-8
    [InlineData("utf16_v23.mp3")]   // ID3v2.3, texto en UTF-16 con BOM
    public void Read_UnicodeTags_AreDecodedExactly(string file)
    {
        var info = _reader.Read(Tags(file));

        Assert.Equal(Utf8Title, info.Title);
        Assert.Equal(Utf8Performer, info.Performer);
        Assert.Equal(Utf8Album, info.Album);
        Assert.Equal(Utf8Genre, info.Genre);
        Assert.Equal(1, info.Track);
        Assert.Equal(2001, info.Year);

        foreach (var text in new[] { info.Title!, info.Performer!, info.Album!, info.Genre! })
        {
            Assert.DoesNotContain("\uFFFD", text); 
            Assert.DoesNotContain("Ã", text);
        }
    }

    /// <summary>
    /// Español.
    /// </summary>
    [Fact]
    public void Read_Latin1Tags_DecodeAccentsAndEnye()
    {
        var info = _reader.Read(Tags("latin1_v23.mp3"));

        Assert.Equal("Cañón ñandú ¿Qué tal? ¡Sí!", info.Title);
        Assert.Equal("Los Niños Ñoños", info.Performer);
        Assert.Equal("Último año", info.Album);
        Assert.Equal("Música", info.Genre);
        Assert.Equal(7, info.Track);
        Assert.Equal(1987, info.Year);
    }

    // =====================================================================
    // Formatos correctos y esperados
    // =====================================================================

    /// <summary>
    /// Se guarda como nombre literal.
    /// </summary>
    [Fact]
    public void Read_NumericGenre_IsResolvedToItsName()
    {
        var info = _reader.Read(Tags("genre_numeric_v24.mp3"));

        Assert.Equal("Rock", info.Genre);
        Assert.Equal(4, info.Track);
    }

    /// <summary>
    /// Valores bien formados.
    /// </summary>
    [Fact]
    public void Read_EveryFixture_ProducesWellFormedValues()
    {
        var files = AllTagFixtures().ToList();
        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            var info = _reader.Read(file);
            var name = Path.GetFileName(file);

            foreach 
            (
                var text in new[] 
                {
                    info.Title,
                    info.Performer,
                    info.AlbumArtist,
                    info.Album,
                    info.Genre 
                }
            )
            {
                if (string.IsNullOrEmpty(text)) continue;
                Assert.False(text.Contains('\0'), $"{name}: texto con caracteres NUL");
                Assert.False(text.Contains('\uFFFD'), $"{name}: texto con caracteres de reemplazo");
            }

            Assert.True(info.Track >= 0, $"{name}: track negativo");
            Assert.True(info.Year >= 0, $"{name}: año negativo");

            if (!string.IsNullOrEmpty(info.Performer))
            {
                Assert.DoesNotContain(";", info.Performer, StringComparison.Ordinal);
                Assert.DoesNotContain("\0", info.Performer, StringComparison.Ordinal);
            }
        }
    }

    // =====================================================================
    // Sin tags o con metadatos vacíos
    // =====================================================================

    /// <summary>
    /// Sin tags.
    /// </summary>
    [Fact]
    public void Read_FileWithoutTags_ReturnsEmptyInfo()
    {
        var info = _reader.Read(Tags("no_tags.mp3"));

        Assert.True(string.IsNullOrWhiteSpace(info.Title));
        Assert.True(string.IsNullOrWhiteSpace(info.Performer));
        Assert.True(string.IsNullOrWhiteSpace(info.AlbumArtist));
        Assert.True(string.IsNullOrWhiteSpace(info.Album));
        Assert.True(string.IsNullOrWhiteSpace(info.Genre));
        Assert.Equal(0, info.Track);
        Assert.Equal(0, info.Year);
    }

    /// <summary>
    /// Mas sin tags xd.
    /// </summary>
    [Fact]
    public void Read_FileWithEmptyMetadata_ReturnsBlankValuesAndZeros()
    {
        var info = _reader.Read(Tags("empty_metadata_v24.mp3"));

        Assert.True(string.IsNullOrWhiteSpace(info.Title));
        Assert.True(string.IsNullOrWhiteSpace(info.Performer));
        Assert.True(string.IsNullOrWhiteSpace(info.AlbumArtist));
        Assert.True(string.IsNullOrWhiteSpace(info.Album));
        Assert.True(string.IsNullOrWhiteSpace(info.Genre));
        Assert.Equal(0, info.Track);
        Assert.Equal(0, info.Year);
    }

    // =====================================================================
    // Fallos esperados
    // =====================================================================

    /// <summary>
    /// No es un archivo MP3.
    /// </summary>
    [Fact]
    public void Read_FileThatIsNotAnMp3_Throws()
    {
        Assert.ThrowsAny<Exception>
        (
            () => _reader.Read(Dataset.At("Generated", "Corrupt", "corrupt.mp3"))
        );
    }

    /// <summary>
    /// Archivo MP3 vacío.
    /// </summary>
    [Fact]
    public void Read_ZeroByteFile_Throws()
    {
        Assert.ThrowsAny<Exception>
        (
            () => _reader.Read(Dataset.At("Generated", "Corrupt", "empty.mp3"))
        );
    }

    /// <summary>
    /// No existe.
    /// </summary>
    [Fact]
    public void Read_MissingFile_ThrowsIOException()
    {
        var missing = _tmp.Combine("no_existe.mp3");

        Assert.ThrowsAny<IOException>(() => _reader.Read(missing));
    }

    // =====================================================================
    // Los MP3 no se modifican
    // =====================================================================

    /// <summary>
    /// No se modifican los MP3.
    /// </summary>
    [Fact]
    public void Read_NeverModifiesTheFiles()
    {
        var dir = _tmp.CopyDirectory(Dataset.At("Generated", "Tags"), "copia");
        var files = Directory.GetFiles(dir, "*.mp3");
        var before = files.ToDictionary(f => f, Snap);

        foreach (var file in files) _reader.Read(file);
        foreach (var file in files) _reader.Read(file);   // y otra vez xd

        foreach (var file in files)
            Assert.Equal(before[file], Snap(file));
        Assert.Equal
        (
            before.Keys.OrderBy(k => k),
            Directory.GetFiles(dir).OrderBy(k => k)
        );
    }

    /// <summary>
    /// Archivo read-only se puede leer perfectamente.
    /// </summary>
    [Fact]
    public void Read_ReadOnlyFile_StillWorksAndStaysIntact()
    {
        var dir = _tmp.CopyDirectory(Dataset.At("Generated", "Tags"), "solo_lectura");
        var file = Path.Combine(dir, "full_v24.mp3");
        var before = Snap(file);
        File.SetAttributes(file, FileAttributes.ReadOnly);

        try
        {
            var info = _reader.Read(file);

            Assert.Equal("Canción de Prueba", info.Title);
            Assert.Equal(before, Snap(file));
        }
        finally
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
    }

    /// <summary>
    /// No bloquea el archivo.
    /// </summary>
    [Fact]
    public void Read_DoesNotKeepTheFileLocked()
    {
        var dir = _tmp.CopyDirectory(Dataset.At("Generated", "Tags"), "bloqueo");
        var file = Path.Combine(dir, "full_v24.mp3");

        _reader.Read(file);

        File.Delete(file);
        Assert.False(File.Exists(file));
    }

    // =====================================================================
    // Cualquier otro MP3 real que pongas en Dataset/
    // =====================================================================

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public void Read_AnyOtherMp3InDataset_ReadsWithoutErrorsAndWithoutModifyingIt()
    {
        var generated = Path.Combine(Dataset.Root, "Generated") + Path.DirectorySeparatorChar;
        var others = Directory
            .EnumerateFiles(Dataset.Root, "*.mp3", SearchOption.AllDirectories)
            .Where(f => !f.StartsWith(generated, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var problems = new List<string>();
        foreach (var file in others)
        {
            var before = Snap(file);
            try
            {
                var info = _reader.Read(file);
                if (info.Track < 0 || info.Year < 0) problems.Add($"{file}: número negativo");
            }
            catch (Exception ex)
            {
                problems.Add($"{file}: {ex.GetType().Name}: {ex.Message}");
            }

            if (before != Snap(file)) problems.Add($"{file}: ¡fue modificado!");
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }
}
