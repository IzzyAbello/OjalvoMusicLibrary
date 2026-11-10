using MusicLib.Data;
using MusicLib.Models;
using MusicLib.Tests.Support;
using Xunit;

namespace MusicLib.Tests.Data;

/// <summary>
/// Pruebas de AlbumDao.Update contra una DB real.
/// </summary>
public sealed class AlbumDaoTests : IDisposable
{
    private readonly TempDir _tmp = new();
    private readonly string _db;
    private readonly long _filosofem;
    private readonly long _otro;

    /// <summary>
    /// Crea una DB con dos álbumes.
    /// </summary>
    public AlbumDaoTests()
    {
        _db = _tmp.Combine("music.db");
        DatabaseInitializer.EnsureCreated(_db);

        using var uow = new UnitOfWork(_db);
        uow.Begin();

        var burzum = uow.Performers.Insert(new Performer { Name = "Burzum" });
        _filosofem = uow.Albums.Insert
        (
            new Album { Name = "Filosofem", Path = "/m/f", Year = 1996 }
        );
        _otro = uow.Albums.Insert
        (
            new Album { Name = "Otro", Path = "/m/o", Year = 2000 }
        );

        foreach (var title in new[] { "Dunkelheit", "Jesu Død" })
            uow.Rolas.Insert
            (
                new Rola
                {
                    IdPerformer = burzum,
                    IdAlbum = _filosofem,
                    Path = $"/m/f/{title}.mp3",
                    Title = title
                }
            );

        uow.Commit();
    }

    /// <summary>
    /// Borra el directorio temporal.
    /// </summary>
    public void Dispose() => _tmp.Dispose();

    private bool Update(long id, string name, int year)
    {
        using var uow = new UnitOfWork(_db);
        return uow.Albums.Update
        (
            // La ruta se ignora a propósito.
            new Album { Id = id, Name = name, Year = year, Path = "/x" }
        );
    }

    /// <summary>
    /// Cambia nombre y año, conserva la ruta.
    /// </summary>
    [Fact]
    public void Update_ChangesNameAndYear_KeepsPath()
    {
        Assert.True(Update(_filosofem, "Filosofem (remaster)", 2010));

        var row = Db.Row
        (
            _db,
            "SELECT name, year, path FROM albums WHERE id_album = $p0",
            _filosofem
        );
        Assert.Equal("Filosofem (remaster)", row[0]);
        Assert.Equal(2010L, row[1]);
        Assert.Equal("/m/f", row[2]);
    }

    /// <summary>
    /// Todas las rolas del álbum ven el cambio.
    /// </summary>
    [Fact]
    public void Update_IsSeenByAllItsRolas()
    {
        Update(_filosofem, "Nuevo", 2010);

        using var uow = new UnitOfWork(_db);
        var rolas = uow.Rolas.Search(new RolaSearch(Album: "Nuevo"));

        Assert.Equal(2, rolas.Count);
        Assert.All(rolas, r => Assert.Equal(2010, r.Album!.Year));
    }

    /// <summary>
    /// Solo cambia el álbum indicado.
    /// </summary>
    [Fact]
    public void Update_OnlyChangesThatAlbum()
    {
        Update(_filosofem, "Nuevo", 2010);

        var row = Db.Row
        (
            _db,
            "SELECT name, year FROM albums WHERE id_album = $p0",
            _otro
        );
        Assert.Equal("Otro", row[0]);
        Assert.Equal(2000L, row[1]);
    }

    /// <summary>
    /// Un id inexistente no cambia nada y devuelve false.
    /// </summary>
    [Fact]
    public void Update_UnknownId_ReturnsFalse()
    {
        Assert.False(Update(999, "X", 1));
        Assert.Equal(0, Db.Scalar<int>(_db, "SELECT COUNT(*) FROM albums WHERE name = 'X'"));
    }

    /// <summary>
    /// FindById trae el álbum completo.
    /// </summary>
    [Fact]
    public void FindById_Existing_ReturnsAlbum()
    {
        using var uow = new UnitOfWork(_db);
        var album = uow.Albums.FindById(_filosofem);

        Assert.NotNull(album);
        Assert.Equal("Filosofem", album.Name);
        Assert.Equal("/m/f", album.Path);
        Assert.Equal(1996, album.Year);
    }

    /// <summary>
    /// FindById con un id inexistente devuelve null.
    /// </summary>
    [Fact]
    public void FindById_Unknown_ReturnsNull()
    {
        using var uow = new UnitOfWork(_db);
        Assert.Null(uow.Albums.FindById(999));
    }
}
