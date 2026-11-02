using Microsoft.Data.Sqlite;
using MusicLib.Data;
using MusicLib.Models;
using MusicLib.Tests.Support;
using Xunit;

namespace MusicLib.Tests.Data;

/// <summary>
/// Prueba que la DB se crea y se mantiene es estado adecuado.
/// </summary>
public sealed class DatabaseInitializerTests : IDisposable
{
    private static readonly string[] ExpectedTables =
    {
        "albums", "groups", "in_group", "performers", "persons", "rolas", "types"
    };

    private static readonly string[] ExpectedIndexes =
    {
        "idx_albums_name_path", "idx_performers_name", "idx_rolas_path"
    };

    private const string InsertRola =
        "INSERT INTO rolas (id_performer, id_album, path, title, track, year, genre) " +
        "VALUES (999, 0, 'x.mp3', 't', 1, 2000, 'g')";

    private readonly TempDir _tmp = new();

    /// <summary>
    /// Cumplir con el contrato de IDisposable de eliminar el directorio
    /// temporal y sus archivos asociados.
    /// </summary>
    public void Dispose() => _tmp.Dispose();

    private string DbPath(string name = "music.db") => _tmp.Combine(name);

    // =====================================================================
    // 1) Crea bien una DB nueva
    // =====================================================================

    /// <summary>
    /// Crea correctamente la DB.
    /// </summary>
    [Fact]
    public void EnsureCreated_NewDb_CreatesFileAndAllTables()
    {
        var db = DbPath();
        Assert.False(File.Exists(db));

        DatabaseInitializer.EnsureCreated(db);

        Assert.True(File.Exists(db));
        Assert.Equal(ExpectedTables, Db.Tables(db));
    }

    /// <summary>
    /// Crea todos los índices.
    /// </summary>
    [Fact]
    public void EnsureCreated_NewDb_CreatesAllIndexes()
    {
        var db = DbPath();

        DatabaseInitializer.EnsureCreated(db);

        Assert.Equal(ExpectedIndexes, Db.Indexes(db));
    }

    /// <summary>
    /// Comprueba que la tabla types se incializa con los valores que corresponen
    /// a la enumeración de PerformerType.
    /// </summary>
    [Fact]
    public void EnsureCreated_NewDb_SeedsTypesMatchingThePerformerTypeEnum()
    {
        var db = DbPath();

        DatabaseInitializer.EnsureCreated(db);

        var rows = Db.Rows(db, "SELECT id_type, description FROM types ORDER BY id_type");
        var expected = Enum.GetValues<PerformerType>()
            .OrderBy(t => (int)t)
            .Select(t => ((long)(int)t, t.ToString()))
            .ToList();

        Assert.Equal(expected, rows.Select(r => ((long)r[0]!, (string)r[1]!)).ToList());
    }

    /// <summary>
    /// Chequea que el álbum desconocido está.
    /// </summary>
    [Fact]
    public void EnsureCreated_NewDb_SeedsOnlyTheUnknownAlbum()
    {
        var db = DbPath();

        DatabaseInitializer.EnsureCreated(db);

        var row = Db.Row(db, "SELECT id_album, path, name, year FROM albums");
        Assert.Equal(Album.UnknownId, row[0]);
        Assert.Equal("", row[1]);
        Assert.Equal("Unknown", row[2]);
        Assert.Equal(0L, row[3]);
    }


    /// <summary>
    /// Prueba que se crea el archivo aunque no existan los directorios intermedios.
    /// </summary>
    [Fact]
    public void EnsureCreated_NewDb_CreatesDirectoryIfMissing()
    {
        var db = _tmp.Combine("no", "existe", "todavia", "music.db");

        DatabaseInitializer.EnsureCreated(db);

        Assert.True(File.Exists(db));
        Assert.Equal(ExpectedTables, Db.Tables(db));
    }


    /// <summary>
    /// Verifica la integriad referencial de la DB.
    /// La operación debe ser rechazada si las claves foráneas están activas.
    /// </summary>
    [Fact]
    public void EnsureCreated_NewDb_EnforcesForeignKeys()
    {
        var db = DbPath();
        DatabaseInitializer.EnsureCreated(db);

        // El performer 999 no existe (a.k.a no tiene referencia XD).
        Assert.Throws<SqliteException>(() => Db.Exec(db, InsertRola, foreignKeys: true));
    }

    // =====================================================================
    // 2) Una 2da llamada con la BD ya existente va bien
    // =====================================================================


    /// <summary>
    /// Crear la DB dos veces no elimina información ni la repite.
    /// </summary>
    [Fact]
    public void EnsureCreated_SecondCall_DoesNotThrowNorDuplicateAnything()
    {
        var db = DbPath();
        DatabaseInitializer.EnsureCreated(db);

        DatabaseInitializer.EnsureCreated(db);

        Assert.Equal(ExpectedTables, Db.Tables(db));
        Assert.Equal(ExpectedIndexes, Db.Indexes(db));
        Assert.Equal(3, Db.Count(db, "types"));
        Assert.Equal(1, Db.Count(db, "albums"));
    }


    /// <summary>
    /// Se conservan los datos previamente introducidos a la DB.
    /// </summary>
    [Fact]
    public void EnsureCreated_SecondCall_PreservesExistingData()
    {
        var db = DbPath();
        DatabaseInitializer.EnsureCreated(db);
        Db.Exec(db,
            "INSERT INTO performers (id_type, name) VALUES (1, 'Banda Uno');" +
            "INSERT INTO albums (path, name, year) VALUES ('/m/a', 'Disco A', 1990);" +
            "INSERT INTO rolas (id_performer, id_album, path, title, track, year, genre) " +
            "VALUES (1, 1, '/m/a/01.mp3', 'Uno', 1, 1990, 'Rock');");

        DatabaseInitializer.EnsureCreated(db);
        DatabaseInitializer.EnsureCreated(db);   // y una tercera, por si acaso XD

        Assert.Equal(1, Db.Count(db, "performers"));
        Assert.Equal(2, Db.Count(db, "albums"));
        Assert.Equal("Uno", Db.Scalar<string>(db, "SELECT title FROM rolas"));
    }


    /// <summary>
    /// Se vuelven a crear índices eliminados.
    /// </summary>
    [Fact]
    public void EnsureCreated_SecondCall_RecreatesMissingIndexes()
    {
        var db = DbPath();
        DatabaseInitializer.EnsureCreated(db);
        Db.Exec(db, "DROP INDEX idx_rolas_path");
        Assert.DoesNotContain("idx_rolas_path", Db.Indexes(db));

        DatabaseInitializer.EnsureCreated(db);

        Assert.Equal(ExpectedIndexes, Db.Indexes(db));
    }

    // =====================================================================
    // 3) Inicializa una BD vacía
    // =====================================================================


    /// <summary>
    /// Se crea correctamente aunque ya exista un archivo vacío en la
    /// misma ruta.
    /// </summary>
    [Fact]
    public void EnsureCreated_ZeroByteFile_CreatesSchema()
    {
        var db = DbPath();
        File.WriteAllBytes(db, Array.Empty<byte>());

        DatabaseInitializer.EnsureCreated(db);

        Assert.Equal(ExpectedTables, Db.Tables(db));
        Assert.Equal(ExpectedIndexes, Db.Indexes(db));
    }

    /// <summary>
    /// Se crea correctamente aunque ya exista una BD vacía.
    /// </summary>
    [Fact]
    public void EnsureCreated_SqliteDbWithoutTables_CreatesSchema()
    {
        var db = DbPath();
        Db.Exec(db, "PRAGMA user_version = 1");
        Assert.Empty(Db.Tables(db));

        DatabaseInitializer.EnsureCreated(db);

        Assert.Equal(ExpectedTables, Db.Tables(db));
    }


    /// <summary>
    /// La DB se crea vacía si no existía una antes.
    /// </summary>
    [Fact]
    public void EnsureCreated_FreshSchema_HasNoUserData()
    {
        var db = DbPath();

        DatabaseInitializer.EnsureCreated(db);

        foreach (var table in new[] { "rolas", "performers", "persons", "groups", "in_group" })
            Assert.Equal(0, Db.Count(db, table));
    }

    // =====================================================================
    // 4) Errores
    // =====================================================================

    /// <summary>
    /// Para un esquema difierente en la misma ruta debe avisar con
    /// una excepción y no tocarla.
    /// </summary>
    [Fact]
    public void EnsureCreated_DbWithForeignSchema_ThrowsAndChangesNothing()
    {
        var db = DbPath();
        Db.Exec(db, "CREATE TABLE otra_cosa (x INTEGER); INSERT INTO otra_cosa VALUES (42);");

        var ex = Assert.Throws<InvalidOperationException>
        (
            () => DatabaseInitializer.EnsureCreated(db)
        );

        foreach (var table in ExpectedTables) Assert.Contains(table, ex.Message);
        Assert.Equal(new[] { "otra_cosa" }, Db.Tables(db));
        Assert.Empty(Db.Indexes(db));
        Assert.Equal(42, Db.Scalar<int>(db, "SELECT x FROM otra_cosa"));
    }

    /// <summary>
    /// Para un esquema parcialmente diferente en la misma ruta avisar con
    /// una excepción y no tocarlo.
    /// </summary>
    [Fact]
    public void EnsureCreated_PartialSchema_ReportsMissingTablesAndChangesNothing()
    {
        var db = DbPath();
        Db.Exec(db,
            "CREATE TABLE types (id_type INTEGER PRIMARY KEY, description TEXT);" +
            "CREATE TABLE performers (id_performer INTEGER PRIMARY KEY);");

        var ex = Assert.Throws<InvalidOperationException>
        (
            () => DatabaseInitializer.EnsureCreated(db)
        );

        Assert.Contains("albums", ex.Message);
        Assert.Contains("rolas", ex.Message);
        Assert.Equal(new[] { "performers", "types" }, Db.Tables(db));   // nada se agregó
        Assert.Empty(Db.Indexes(db));
    }


    /// <summary>
    /// Comprueba atomicidad de las transacciones. 
    /// </summary>
    [Fact]
    public void EnsureCreated_ScriptFailsMidway_RollsBackEverything()
    {
        // Una vista llamada "rolas" hace fallar el CREATE TABLE rolas, que es la
        // ÚLTIMA sentencia del script: todo lo creado antes debe deshacerse.
        var db = DbPath();
        Db.Exec(db, "CREATE VIEW rolas AS SELECT 1 AS x");

        Assert.Throws<SqliteException>(() => DatabaseInitializer.EnsureCreated(db));

        Assert.Empty(Db.Tables(db));
        Assert.Empty(Db.Indexes(db));
    }

    /// <summary>
    /// Falla cuando el archivo no es una DB válida.
    /// </summary>
    [Fact]
    public void EnsureCreated_FileThatIsNotADatabase_ThrowsAndLeavesFileUntouched()
    {
        var db = DbPath();
        var garbage = System.Text.Encoding.UTF8.GetBytes
        (
            string.Concat(Enumerable.Repeat("esto no es una base de datos. ", 40))
        );
        File.WriteAllBytes(db, garbage);

        Assert.ThrowsAny<SqliteException>(() => DatabaseInitializer.EnsureCreated(db));

        Assert.Equal(garbage, File.ReadAllBytes(db));
    }

    /// <summary>
    /// Falla cuando se crea un un directorio inválido.
    /// </summary>
    [Fact]
    public void EnsureCreated_ParentPathIsAFile_ThrowsIOException()
    {
        var blocker = _tmp.Combine("blocker");
        File.WriteAllText(blocker, "soy un archivo, no un directorio");
        var db = Path.Combine(blocker, "music.db");

        Assert.ThrowsAny<IOException>(() => DatabaseInitializer.EnsureCreated(db));
    }

    /// <summary>
    /// Falla cuando no se especifica la ruta del archivo.
    /// </summary>
    [Fact]
    public void EnsureCreated_EmptyPath_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => DatabaseInitializer.EnsureCreated(""));
    }

    /// <summary>
    /// Falla cuando la ruta del archivo es nula.
    /// </summary>
    [Fact]
    public void EnsureCreated_NullPath_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => DatabaseInitializer.EnsureCreated(null!));
    }
}
