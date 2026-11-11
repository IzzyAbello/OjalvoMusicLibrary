using MusicLib.Data;
using MusicLib.Models;
using MusicLib.Tests.Support;
using Xunit;

namespace MusicLib.Tests.Data;

/// <summary>
/// Pruebas de PersonDao contra una DB real.
/// </summary>
public sealed class PersonDaoTests : IDisposable
{
    private readonly TempDir _tmp = new();
    private readonly string _db;
    private readonly long _varg;

    /// <summary>
    /// Crea una DB con dos personas: "Varg" (con todos sus datos) y
    /// "Abbath" (solo con nombre artístico).
    /// </summary>
    public PersonDaoTests()
    {
        _db = _tmp.Combine("music.db");
        DatabaseInitializer.EnsureCreated(_db);

        using var uow = new UnitOfWork(_db);
        _varg = uow.Persons.Insert
        (
            new Person
            {
                StageName = "Varg",
                RealName = "Kristian Vikernes",
                BirthDate = "1973-02-11"
            }
        );
        uow.Persons.Insert(new Person { StageName = "Abbath" });
    }

    /// <summary>
    /// Borra el directorio temporal.
    /// </summary>
    public void Dispose() => _tmp.Dispose();

    private Person? FindById(long id)
    {
        using var uow = new UnitOfWork(_db);
        return uow.Persons.FindById(id);
    }

    /// <summary>
    /// Insert guarda todos los campos y FindById los regresa; los
    /// opcionales vacíos quedan como null.
    /// </summary>
    [Fact]
    public void Insert_SavesAllFields()
    {
        var varg = FindById(_varg);

        Assert.NotNull(varg);
        Assert.Equal("Varg", varg.StageName);
        Assert.Equal("Kristian Vikernes", varg.RealName);
        Assert.Equal("1973-02-11", varg.BirthDate);
        Assert.Null(varg.DeathDate);
    }

    /// <summary>
    /// FindAll regresa todas, ordenadas por nombre artístico.
    /// </summary>
    [Fact]
    public void FindAll_OrdersByStageName()
    {
        using var uow = new UnitOfWork(_db);

        Assert.Equal
        (
            ["Abbath", "Varg"],
            uow.Persons.FindAll().Select(p => p.StageName)
        );
    }

    /// <summary>
    /// FindByStageName busca el nombre exacto.
    /// </summary>
    [Fact]
    public void FindByStageName_IsExact()
    {
        using var uow = new UnitOfWork(_db);

        Assert.Equal(_varg, uow.Persons.FindByStageName("Varg")?.Id);
        Assert.Null(uow.Persons.FindByStageName("varg"));
        Assert.Null(uow.Persons.FindByStageName("Var"));
    }

    /// <summary>
    /// Un id inexistente da null.
    /// </summary>
    [Fact]
    public void FindById_UnknownId_ReturnsNull() => Assert.Null(FindById(999));

    /// <summary>
    /// Update cambia nombre real y fechas, pero no el nombre artístico.
    /// </summary>
    [Fact]
    public void Update_ChangesDetails_KeepsStageName()
    {
        bool updated;
        using (var uow = new UnitOfWork(_db))
            updated = uow.Persons.Update
            (
                new Person
                {
                    Id = _varg,
                    StageName = "Otro nombre",
                    RealName = "Louis Cachet",
                    BirthDate = null,
                    DeathDate = "2050-01-01"
                }
            );

        Assert.True(updated);
        var varg = FindById(_varg)!;
        Assert.Equal("Varg", varg.StageName);
        Assert.Equal("Louis Cachet", varg.RealName);
        Assert.Null(varg.BirthDate);
        Assert.Equal("2050-01-01", varg.DeathDate);
    }

    /// <summary>
    /// Update con un id inexistente regresa false.
    /// </summary>
    [Fact]
    public void Update_UnknownId_ReturnsFalse()
    {
        using var uow = new UnitOfWork(_db);
        Assert.False(uow.Persons.Update(new Person { Id = 999 }));
    }

    /// <summary>
    /// Delete borra solo esa persona.
    /// </summary>
    [Fact]
    public void Delete_RemovesOnlyThatPerson()
    {
        using (var uow = new UnitOfWork(_db))
            Assert.True(uow.Persons.Delete(_varg));

        Assert.Null(FindById(_varg));
        Assert.Equal(1, Db.Count(_db, "persons"));
    }

    /// <summary>
    /// Delete con un id inexistente regresa false.
    /// </summary>
    [Fact]
    public void Delete_UnknownId_ReturnsFalse()
    {
        using var uow = new UnitOfWork(_db);
        Assert.False(uow.Persons.Delete(999));
    }
}
