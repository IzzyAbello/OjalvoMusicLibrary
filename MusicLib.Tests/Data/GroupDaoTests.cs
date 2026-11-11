using MusicLib.Data;
using MusicLib.Models;
using MusicLib.Tests.Support;
using Xunit;

namespace MusicLib.Tests.Data;

/// <summary>
/// Pruebas de GroupDao contra una DB real.
/// </summary>
public sealed class GroupDaoTests : IDisposable
{
    private readonly TempDir _tmp = new();
    private readonly string _db;
    private readonly long _mayhem;

    /// <summary>
    /// Crea una DB con dos grupos: "Mayhem" (con fecha de inicio) e
    /// "Immortal" (solo con nombre).
    /// </summary>
    public GroupDaoTests()
    {
        _db = _tmp.Combine("music.db");
        DatabaseInitializer.EnsureCreated(_db);

        using var uow = new UnitOfWork(_db);
        _mayhem = uow.Groups.Insert
        (
            new Group { Name = "Mayhem", StartDate = "1984-01-01" }
        );
        uow.Groups.Insert(new Group { Name = "Immortal" });
    }

    /// <summary>
    /// Borra el directorio temporal.
    /// </summary>
    public void Dispose() => _tmp.Dispose();

    private Group? FindById(long id)
    {
        using var uow = new UnitOfWork(_db);
        return uow.Groups.FindById(id);
    }

    /// <summary>
    /// Insert guarda todos los campos y FindById los regresa; los
    /// opcionales vacíos quedan como null.
    /// </summary>
    [Fact]
    public void Insert_SavesAllFields()
    {
        var mayhem = FindById(_mayhem);

        Assert.NotNull(mayhem);
        Assert.Equal("Mayhem", mayhem.Name);
        Assert.Equal("1984-01-01", mayhem.StartDate);
        Assert.Null(mayhem.EndDate);
    }

    /// <summary>
    /// FindAll regresa todos, ordenados por nombre.
    /// </summary>
    [Fact]
    public void FindAll_OrdersByName()
    {
        using var uow = new UnitOfWork(_db);

        Assert.Equal
        (
            ["Immortal", "Mayhem"],
            uow.Groups.FindAll().Select(g => g.Name)
        );
    }

    /// <summary>
    /// FindByName busca el nombre exacto.
    /// </summary>
    [Fact]
    public void FindByName_IsExact()
    {
        using var uow = new UnitOfWork(_db);

        Assert.Equal(_mayhem, uow.Groups.FindByName("Mayhem")?.Id);
        Assert.Null(uow.Groups.FindByName("mayhem"));
        Assert.Null(uow.Groups.FindByName("May"));
    }

    /// <summary>
    /// Un id inexistente da null.
    /// </summary>
    [Fact]
    public void FindById_UnknownId_ReturnsNull() => Assert.Null(FindById(999));

    /// <summary>
    /// Update cambia las fechas, pero no el nombre.
    /// </summary>
    [Fact]
    public void Update_ChangesDates_KeepsName()
    {
        bool updated;
        using (var uow = new UnitOfWork(_db))
            updated = uow.Groups.Update
            (
                new Group
                {
                    Id = _mayhem,
                    Name = "Otro nombre",
                    StartDate = null,
                    EndDate = "2050-01-01"
                }
            );

        Assert.True(updated);
        var mayhem = FindById(_mayhem)!;
        Assert.Equal("Mayhem", mayhem.Name);
        Assert.Null(mayhem.StartDate);
        Assert.Equal("2050-01-01", mayhem.EndDate);
    }

    /// <summary>
    /// Update con un id inexistente regresa false.
    /// </summary>
    [Fact]
    public void Update_UnknownId_ReturnsFalse()
    {
        using var uow = new UnitOfWork(_db);
        Assert.False(uow.Groups.Update(new Group { Id = 999 }));
    }

    /// <summary>
    /// Delete borra solo ese grupo.
    /// </summary>
    [Fact]
    public void Delete_RemovesOnlyThatGroup()
    {
        using (var uow = new UnitOfWork(_db))
            Assert.True(uow.Groups.Delete(_mayhem));

        Assert.Null(FindById(_mayhem));
        Assert.Equal(1, Db.Count(_db, "groups"));
    }

    /// <summary>
    /// Delete con un id inexistente regresa false.
    /// </summary>
    [Fact]
    public void Delete_UnknownId_ReturnsFalse()
    {
        using var uow = new UnitOfWork(_db);
        Assert.False(uow.Groups.Delete(999));
    }
}
