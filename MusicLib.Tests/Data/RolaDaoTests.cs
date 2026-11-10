using MusicLib.Data;
using MusicLib.Models;
using MusicLib.Tests.Support;
using Xunit;

namespace MusicLib.Tests.Data;

/// <summary>
/// Pruebas de RolaDao contra una DB real.
/// </summary>
public sealed class RolaDaoTests : IDisposable
{
    private readonly TempDir _tmp = new();
    private readonly string _db;

    /// <summary>
    /// Crea una DB nueva con tres rolas de dos intérpretes y dos álbumes:
    /// <list type="bullet">
    /// <item>Burzum / Filosofem (1996): 1 "Dunkelheit", 2 "Jesu Død"</item>
    /// <item>Los Niños Ñoños / Último año (1987): 7 "Cañón ñandú"</item>
    /// </list>
    /// </summary>
    public RolaDaoTests()
    {
        _db = _tmp.Combine("music.db");
        DatabaseInitializer.EnsureCreated(_db);

        using var uow = new UnitOfWork(_db);
        uow.Begin();

        var burzum = AddPerformer(uow, "Burzum");
        var ninos = AddPerformer(uow, "Los Niños Ñoños");
        var filosofem = AddAlbum(uow, "Filosofem", "/m/filosofem", 1996);
        var ultimo = AddAlbum(uow, "Último año", "/m/ultimo", 1987);

        AddRola(uow, ninos, ultimo, 7, "Cañón ñandú", 1987, "Pop");
        AddRola(uow, burzum, filosofem, 2, "Jesu Død", 1996, "Black Metal");
        AddRola(uow, burzum, filosofem, 1, "Dunkelheit", 1996, "Black Metal");

        uow.Commit();
    }

    /// <summary>
    /// Borra el directorio temporal para cumplir con el contrato de IDisposable.
    /// </summary>
    public void Dispose() => _tmp.Dispose();

    private static long AddPerformer(UnitOfWork uow, string name) =>
        uow.Performers.Insert(new Performer { Name = name });

    private static long AddAlbum
    (
        UnitOfWork uow,
        string name,
        string path,
        int year
    ) => uow.Albums.Insert(new Album { Name = name, Path = path, Year = year });

    private static void AddRola
    (
        UnitOfWork uow,
        long idPerformer,
        long idAlbum,
        int track,
        string title,
        int year,
        string genre
    ) => uow.Rolas.Insert
    (
        new Rola
        {
            IdPerformer = idPerformer,
            IdAlbum = idAlbum,
            Path = $"/m/{title}.mp3",
            Title = title,
            Track = track,
            Year = year,
            Genre = genre
        }
    );

    private IReadOnlyList<Rola> Search(RolaSearch criteria)
    {
        using var uow = new UnitOfWork(_db);
        return uow.Rolas.Search(criteria);
    }

    private List<string> Titles(RolaSearch criteria) =>
        Search(criteria).Select(r => r.Title).ToList();

    /// <summary>
    /// Cada rola trae su intérprete y su álbum completos y consistentes.
    /// </summary>
    [Fact]
    public void Search_FillsPerformerAndAlbum()
    {
        var rola = Assert.Single(Search(new RolaSearch(Title: "Dunkelheit")));

        Assert.NotNull(rola.Performer);
        Assert.Equal(rola.IdPerformer, rola.Performer.Id);
        Assert.Equal("Burzum", rola.Performer.Name);

        Assert.NotNull(rola.Album);
        Assert.Equal(rola.IdAlbum, rola.Album.Id);
        Assert.Equal("Filosofem", rola.Album.Name);
        Assert.Equal("/m/filosofem", rola.Album.Path);
        Assert.Equal(1996, rola.Album.Year);
    }

    /// <summary>
    /// Una rola con un intérprete inexistente no desaparece (LEFT JOIN):
    /// sale con el nombre vacío.
    /// </summary>
    [Fact]
    public void Search_RolaWithBrokenPerformer_StillAppearsWithEmptyName()
    {
        Db.Exec
        (
            _db,
            "INSERT INTO rolas " +
            "(id_performer, id_album, path, title, track, year, genre) " +
            "VALUES (999, 0, '/m/huerfana.mp3', 'Huérfana', 1, 2000, 'X')"
        );

        var rola = Assert.Single(Search(new RolaSearch(Title: "huerfana")));

        Assert.Equal(999, rola.IdPerformer);
        Assert.Equal("", rola.Performer!.Name);
        Assert.Equal("Unknown", rola.Album!.Name);
    }

    /// <summary>
    /// Cada campo encuentra ignorando acentos y mayúsculas.
    /// </summary>
    [Theory]
    [InlineData("title", "CANON NANDU")]
    [InlineData("performer", "ninos nonos")]
    [InlineData("album", "ULTIMO")]
    [InlineData("genre", "pOp")]
    public void Search_ByField_IgnoresAccentsAndCase(string field, string text)
    {
        var criteria = field switch
        {
            "title" => new RolaSearch(Title: text),
            "performer" => new RolaSearch(Performer: text),
            "album" => new RolaSearch(Album: text),
            _ => new RolaSearch(Genre: text)
        };

        Assert.Equal(["Cañón ñandú"], Titles(criteria));
    }

    /// <summary>
    /// Any encuentra en cualquiera de los cuatro campos.
    /// </summary>
    [Theory]
    [InlineData("dunkel")]      // título
    [InlineData("burzum")]      // intérprete
    [InlineData("filosofem")]   // álbum
    [InlineData("black")]       // género
    public void Search_Any_MatchesAnyField(string text) =>
        Assert.Contains("Dunkelheit", Titles(new RolaSearch(Any: text)));

    /// <summary>
    /// Un texto que no está en ningún lado no encuentra nada.
    /// </summary>
    [Fact]
    public void Search_NoMatch_ReturnsEmpty() =>
        Assert.Empty(Search(new RolaSearch(Any: "zzz")));

    /// <summary>
    /// El rango de años es inclusivo en ambos extremos.
    /// </summary>
    [Fact]
    public void Search_YearRange_IsInclusive()
    {
        Assert.Equal
        (
            ["Cañón ñandú"],
            Titles(new RolaSearch(YearFrom: 1987, YearTo: 1987))
        );
        Assert.Equal(2, Search(new RolaSearch(YearFrom: 1990)).Count);
        Assert.Single(Search(new RolaSearch(YearTo: 1990)));
    }

    /// <summary>
    /// Ordena por intérprete, álbum y pista.
    /// </summary>
    [Fact]
    public void FindFirst_OrdersByPerformerAlbumAndTrack()
    {
        using var uow = new UnitOfWork(_db);

        Assert.Equal
        (
            ["Dunkelheit", "Jesu Død", "Cañón ñandú"],
            uow.Rolas.FindFirst().Select(r => r.Title)
        );
    }

    /// <summary>
    /// Respeta la cantidad máxima pedida.
    /// </summary>
    [Fact]
    public void FindFirst_RespectsCount()
    {
        using var uow = new UnitOfWork(_db);

        Assert.Equal
        (
            ["Dunkelheit", "Jesu Død"],
            uow.Rolas.FindFirst(2).Select(r => r.Title)
        );
    }

    /// <summary>
    /// Una cantidad no positiva lanza excepción.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Search_NonPositiveCount_Throws(int count)
    {
        using var uow = new UnitOfWork(_db);

        Assert.Throws<ArgumentOutOfRangeException>
        (
            () => uow.Rolas.Search(new RolaSearch(), count)
        );
    }

    /// <summary>
    /// Criterios null lanzan excepción.
    /// </summary>
    [Fact]
    public void Search_NullCriteria_Throws()
    {
        using var uow = new UnitOfWork(_db);

        Assert.Throws<ArgumentNullException>
        (
            () => uow.Rolas.Search(null!)
        );
    }

    private Rola Find(string title) =>
        Assert.Single(Search(new RolaSearch(Title: title)));

    /// <summary>
    /// Update cambia título, pista, año, género, intérprete y álbum; la
    /// ruta se queda igual.
    /// </summary>
    [Fact]
    public void Update_ChangesEditableFields_KeepsPath()
    {
        var original = Find("Dunkelheit");
        var ninos = Find("Cañón ñandú").IdPerformer;

        bool updated;
        using (var uow = new UnitOfWork(_db))
            updated = uow.Rolas.Update
            (
                new Rola
                {
                    Id = original.Id,
                    Title = "Nuevo título",
                    Track = 9,
                    Year = 2001,
                    Genre = "Ambient",
                    IdPerformer = ninos,
                    IdAlbum = Album.UnknownId,
                    // Distinta a propósito: Update debe ignorarla.
                    Path = "/otra/ruta.mp3"
                }
            );

        Assert.True(updated);
        var after = Find("Nuevo título");
        Assert.Equal(original.Id, after.Id);
        Assert.Equal(9, after.Track);
        Assert.Equal(2001, after.Year);
        Assert.Equal("Ambient", after.Genre);
        Assert.Equal("Los Niños Ñoños", after.Performer!.Name);
        Assert.Equal(original.Path, after.Path);
        Assert.Equal(Album.UnknownId, after.IdAlbum);
    }

    /// <summary>
    /// Update solo toca la rola indicada.
    /// </summary>
    [Fact]
    public void Update_OnlyChangesThatRola()
    {
        var target = Find("Dunkelheit");

        using (var uow = new UnitOfWork(_db))
        {
            target.Title = "Cambiada";
            uow.Rolas.Update(target);
        }

        Assert.Equal
        (
            ["Cambiada", "Jesu Død", "Cañón ñandú"],
            Titles(new RolaSearch())
        );
    }

    /// <summary>
    /// Un id inexistente no cambia nada y devuelve false.
    /// </summary>
    [Fact]
    public void Update_UnknownId_ReturnsFalseAndChangesNothing()
    {
        using var uow = new UnitOfWork(_db);

        Assert.False(uow.Rolas.Update(new Rola { Id = 999, Title = "X" }));
        Assert.DoesNotContain("X", Titles(new RolaSearch()));
    }

    /// <summary>
    /// FindById trae la rola con su intérprete y su álbum.
    /// </summary>
    [Fact]
    public void FindById_ReturnsRolaWithPerformerAndAlbum()
    {
        var id = Find("Dunkelheit").Id;
        using var uow = new UnitOfWork(_db);

        var rola = uow.Rolas.FindById(id);

        Assert.NotNull(rola);
        Assert.Equal("Dunkelheit", rola.Title);
        Assert.Equal("Burzum", rola.Performer!.Name);
        Assert.Equal("Filosofem", rola.Album!.Name);
    }

    /// <summary>
    /// Un id inexistente da null.
    /// </summary>
    [Fact]
    public void FindById_UnknownId_ReturnsNull()
    {
        using var uow = new UnitOfWork(_db);
        Assert.Null(uow.Rolas.FindById(999));
    }
}
