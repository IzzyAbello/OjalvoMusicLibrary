using MusicLib.Data;
using MusicLib.Models;
using MusicLib.Tests.Support;
using Xunit;

namespace MusicLib.Tests.Data;

/// <summary>
/// Pruebas de LibraryEditor contra una BD real.
/// </summary>
public sealed class LibraryEditorTests : IDisposable
{
    private readonly TempDir _tmp = new();
    private readonly string _db;
    private readonly LibraryEditor _editor;
    private readonly long _dunkelheit;
    private readonly long _jesu;

    /// <summary>
    /// BD con dos rolas de Burzum en el álbum Filosofem. 
    /// Aunque Varg Vikernes odie a mi pueblo a mi me gusta
    /// su música XD.
    /// </summary>
    public LibraryEditorTests()
    {
        _db = _tmp.Combine("music.db");
        DatabaseInitializer.EnsureCreated(_db);
        _editor = new LibraryEditor(_db);

        using var uow = new UnitOfWork(_db);
        uow.Begin();

        var burzum = uow.Performers.Insert(new Performer { Name = "Burzum" });
        var filosofem = uow.Albums.Insert
        (
            new Album { Name = "Filosofem", Path = "/m/f", Year = 1996 }
        );
        _dunkelheit = AddRola(uow, burzum, filosofem, "Dunkelheit");
        _jesu = AddRola(uow, burzum, filosofem, "Jesu Død");

        uow.Commit();
    }

    /// <summary>
    /// Borra el directorio temporal.
    /// </summary>
    public void Dispose() => _tmp.Dispose();

    private static long AddRola
    (
        UnitOfWork uow,
        long idPerformer,
        long idAlbum,
        string title
    ) => uow.Rolas.Insert
    (
        new Rola
        {
            IdPerformer = idPerformer,
            IdAlbum = idAlbum,
            Path = $"/m/f/{title}.mp3",
            Title = title,
            Track = 1,
            Year = 1996,
            Genre = "Black Metal"
        }
    );

    private Rola Get(long id)
    {
        using var uow = new UnitOfWork(_db);
        return uow.Rolas.FindFirst().Single(r => r.Id == id);
    }

    private int Performers() => Db.Count(_db, "performers");

    /// <summary>
    /// Guarda los campos (recortados) de la rola.
    /// </summary>
    [Fact]
    public void UpdateRola_SavesTrimmedFields()
    {
        Assert.True
        (
            _editor.UpdateRola(_dunkelheit, " Nueva ", "Burzum", "Filosofem", 3, 2001, " Ambient ")
        );

        var rola = Get(_dunkelheit);
        Assert.Equal("Nueva", rola.Title);
        Assert.Equal(3, rola.Track);
        Assert.Equal(2001, rola.Year);
        Assert.Equal("Ambient", rola.Genre);
    }

    /// <summary>
    /// Un intérprete que ya existe se reutiliza (no se duplica).
    /// </summary>
    [Fact]
    public void UpdateRola_ExistingPerformer_IsReused()
    {
        var before = Performers();

        _editor.UpdateRola(_dunkelheit, "Dunkelheit", " Burzum ", "Filosofem", 1, 1996, "X");

        Assert.Equal(before, Performers());
        Assert.Equal(Get(_jesu).IdPerformer, Get(_dunkelheit).IdPerformer);
    }

    /// <summary>
    /// Un intérprete nuevo se crea y solo esa rola cambia.
    /// </summary>
    [Fact]
    public void UpdateRola_NewPerformer_IsCreatedOnlyForThatRola()
    {
        var before = Performers();

        _editor.UpdateRola(_dunkelheit, "Dunkelheit", "Darkthrone", "Filosofem", 1, 1996, "X");

        Assert.Equal(before + 1, Performers());
        var rola = Get(_dunkelheit);
        Assert.Equal("Darkthrone", rola.Performer!.Name);
        Assert.Equal(PerformerType.Unknown, rola.Performer.Type);
        Assert.Equal("Burzum", Get(_jesu).Performer!.Name);
    }

    /// <summary>
    /// Género vacío se guarda como Rola.UnknownGenre.
    /// </summary>
    [Fact]
    public void UpdateRola_BlankGenre_IsSavedAsUnknown()
    {
        _editor.UpdateRola(_dunkelheit, "Dunkelheit", "Burzum", "Filosofem", 1, 1996, "  ");
        Assert.Equal(Rola.UnknownGenre, Get(_dunkelheit).Genre);
    }

    /// <summary>
    /// Datos inválidos se rechazan sin tocar la DB.
    /// </summary>
    [Theory]
    [InlineData("  ", "Burzum", 1, 1996)]
    [InlineData("Título", "", 1, 1996)]
    [InlineData("Título", "Nuevo intérprete", -1, 1996)]
    [InlineData("Título", "Nuevo intérprete", 1, -5)]
    [InlineData("Título", "Burzum", 1, 1996, " ")]
    public void UpdateRola_InvalidData_ThrowsAndChangesNothing
    (
        string title,
        string performer,
        int track,
        int year,
        string album = "Filosofem"
    )
    {
        var before = Performers();

        Assert.Throws<ArgumentException>
        (
            () => _editor.UpdateRola(_dunkelheit, title, performer, album, track, year, "X")
        );

        Assert.Equal(before, Performers());
        Assert.Equal("Dunkelheit", Get(_dunkelheit).Title);
    }

    /// <summary>
    /// Rola inexistente: false y no queda creado el intérprete nuevo
    /// (la transacción se deshace).
    /// </summary>
    [Fact]
    public void UpdateRola_UnknownId_ReturnsFalseAndCreatesNoPerformer()
    {
        var before = Performers();

        Assert.False(_editor.UpdateRola(999, "X", "Darkthrone", "Filosofem", 1, 1996, "X"));
        Assert.Equal(before, Performers());
    }

    private int AlbumsCount() => Db.Count(_db, "albums");

    /// <summary>
    /// Un álbum nuevo se crea en el directorio de la rola, con su año, y
    /// solo esa rola se mueve.
    /// </summary>
    [Fact]
    public void UpdateRola_NewAlbum_IsCreatedOnlyForThatRola()
    {
        var before = AlbumsCount();

        _editor.UpdateRola(_dunkelheit, "Dunkelheit", "Burzum", "Hvis", 1, 1994, "X");

        Assert.Equal(before + 1, AlbumsCount());
        var album = Get(_dunkelheit).Album!;
        Assert.Equal("Hvis", album.Name);
        Assert.Equal("/m/f", album.Path);
        Assert.Equal(1994, album.Year);
        Assert.Equal("Filosofem", Get(_jesu).Album!.Name);
    }

    /// <summary>
    /// Si ya hay un álbum con ese nombre en el directorio, se reutiliza.
    /// </summary>
    [Fact]
    public void UpdateRola_ExistingAlbumInSameFolder_IsReused()
    {
        var hvis = AddAlbum("Hvis", "/m/f");
        var before = AlbumsCount();

        _editor.UpdateRola(_dunkelheit, "Dunkelheit", "Burzum", " Hvis ", 1, 1994, "X");

        Assert.Equal(before, AlbumsCount());
        Assert.Equal(hvis, Get(_dunkelheit).IdAlbum);
    }

    /// <summary>
    /// Un álbum con ese nombre en otro directorio no cuenta: se crea uno
    /// en el directorio de la rola.
    /// </summary>
    [Fact]
    public void UpdateRola_AlbumInAnotherFolder_IsNotReused()
    {
        var otro = AddAlbum("Hvis", "/m/otro");

        _editor.UpdateRola(_dunkelheit, "Dunkelheit", "Burzum", "Hvis", 1, 1994, "X");

        Assert.NotEqual(otro, Get(_dunkelheit).IdAlbum);
        Assert.Equal("/m/f", Get(_dunkelheit).Album!.Path);
    }

    /// <summary>
    /// Una rola del álbum Unknown que se guarda sin cambiar el álbum se
    /// queda en Unknown (no se crea un "Unknown" en su directorio).
    /// </summary>
    [Fact]
    public void UpdateRola_SameAlbumName_KeepsTheAlbum()
    {
        using (var uow = new UnitOfWork(_db))
        {
            var rola = Get(_jesu);
            rola.IdAlbum = Album.UnknownId;
            uow.Rolas.Update(rola);
        }
        var before = AlbumsCount();

        _editor.UpdateRola(_jesu, "Jesu Død", "Burzum", "Unknown", 1, 1996, "X");

        Assert.Equal(before, AlbumsCount());
        Assert.Equal(Album.UnknownId, Get(_jesu).IdAlbum);
    }

    private long AlbumId => Get(_dunkelheit).IdAlbum;

    private long AddAlbum(string name, string path)
    {
        using var uow = new UnitOfWork(_db);
        return uow.Albums.Insert(new Album { Name = name, Path = path, Year = 1 });
    }

    /// <summary>
    /// Guarda nombre (recortado) y año, y lo ven todas sus rolas.
    /// </summary>
    [Fact]
    public void UpdateAlbum_SavesAndAllRolasSeeIt()
    {
        Assert.True(_editor.UpdateAlbum(AlbumId, " Filosofem II ", 2010));

        foreach (var id in new[] { _dunkelheit, _jesu })
        {
            var album = Get(id).Album!;
            Assert.Equal("Filosofem II", album.Name);
            Assert.Equal(2010, album.Year);
        }
    }

    /// <summary>
    /// Conservar el propio nombre (solo cambiar el año) no es duplicado.
    /// </summary>
    [Fact]
    public void UpdateAlbum_SameName_IsAllowed()
    {
        Assert.True(_editor.UpdateAlbum(AlbumId, "Filosofem", 2010));
        Assert.Equal(2010, Get(_dunkelheit).Album!.Year);
    }

    /// <summary>
    /// El mismo nombre en OTRO directorio es otro álbum: se permite.
    /// </summary>
    [Fact]
    public void UpdateAlbum_NameUsedInAnotherFolder_IsAllowed()
    {
        AddAlbum("Otro", "/m/otro-directorio");
        Assert.True(_editor.UpdateAlbum(AlbumId, "Otro", 1996));
    }

    /// <summary>
    /// El mismo nombre en el MISMO directorio duplicaría el álbum: se rechaza
    /// sin cambiar nada.
    /// </summary>
    [Fact]
    public void UpdateAlbum_NameUsedInSameFolder_ThrowsAndChangesNothing()
    {
        AddAlbum("Otro", "/m/f");

        Assert.Throws<InvalidOperationException>
        (
            () => _editor.UpdateAlbum(AlbumId, "Otro", 2010)
        );

        var album = Get(_dunkelheit).Album!;
        Assert.Equal("Filosofem", album.Name);
        Assert.Equal(1996, album.Year);
    }

    /// <summary>
    /// El álbum comodín "Unknown" no se puede editar.
    /// </summary>
    [Fact]
    public void UpdateAlbum_UnknownAlbum_Throws() =>
        Assert.Throws<InvalidOperationException>
        (
            () => _editor.UpdateAlbum(Album.UnknownId, "X", 2000)
        );

    /// <summary>
    /// Nombre vacío o año negativo se rechazan.
    /// </summary>
    [Theory]
    [InlineData("  ", 2000)]
    [InlineData("X", -1)]
    public void UpdateAlbum_InvalidData_Throws(string name, int year) =>
        Assert.Throws<ArgumentException>
        (
            () => _editor.UpdateAlbum(AlbumId, name, year)
        );

    /// <summary>
    /// Álbum inexistente: false.
    /// </summary>
    [Fact]
    public void UpdateAlbum_UnknownId_ReturnsFalse() =>
        Assert.False(_editor.UpdateAlbum(999, "X", 2000));
}
