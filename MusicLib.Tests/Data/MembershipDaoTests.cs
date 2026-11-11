using Microsoft.Data.Sqlite;
using MusicLib.Data;
using MusicLib.Models;
using MusicLib.Tests.Support;
using Xunit;

namespace MusicLib.Tests.Data;

/// <summary>
/// Pruebas de MembershipDao (tabla in_group) contra una DB real.
/// </summary>
public sealed class MembershipDaoTests : IDisposable
{
    private readonly TempDir _tmp = new();
    private readonly string _db;
    private readonly long _varg;
    private readonly long _abbath;
    private readonly long _demonaz;
    private readonly long _immortal;
    private readonly long _mayhem;

    /// <summary>
    /// Crea una DB con tres personas y dos grupos:
    /// <list type="bullet">
    /// <item>Immortal: Demonaz y Abbath</item>
    /// <item>Mayhem: Varg</item>
    /// </list>
    /// </summary>
    public MembershipDaoTests()
    {
        _db = _tmp.Combine("music.db");
        DatabaseInitializer.EnsureCreated(_db);

        using var uow = new UnitOfWork(_db);
        _varg = uow.Persons.Insert(new Person { StageName = "Varg" });
        _abbath = uow.Persons.Insert(new Person { StageName = "Abbath" });
        _demonaz = uow.Persons.Insert(new Person { StageName = "Demonaz" });
        _immortal = uow.Groups.Insert(new Group { Name = "Immortal" });
        _mayhem = uow.Groups.Insert(new Group { Name = "Mayhem" });

        uow.Memberships.Add(_demonaz, _immortal);
        uow.Memberships.Add(_abbath, _immortal);
        uow.Memberships.Add(_varg, _mayhem);
    }

    /// <summary>
    /// Borra el directorio temporal.
    /// </summary>
    public void Dispose() => _tmp.Dispose();

    private List<string> Members(long idGroup)
    {
        using var uow = new UnitOfWork(_db);
        return uow.Memberships.Members(idGroup)
            .Select(p => p.StageName)
            .ToList();
    }

    /// <summary>
    /// Members regresa solo los de ese grupo, ordenados por nombre.
    /// </summary>
    [Fact]
    public void Members_ReturnsOnlyThatGroup_OrderedByName()
    {
        Assert.Equal(["Abbath", "Demonaz"], Members(_immortal));
        Assert.Equal(["Varg"], Members(_mayhem));
    }

    /// <summary>
    /// Un grupo sin miembros (o inexistente) da una lista vacía.
    /// </summary>
    [Fact]
    public void Members_GroupWithoutMembers_IsEmpty() =>
        Assert.Empty(Members(999));

    /// <summary>
    /// Una persona puede estar en varios grupos.
    /// </summary>
    [Fact]
    public void Add_PersonCanBeInSeveralGroups()
    {
        using (var uow = new UnitOfWork(_db))
            uow.Memberships.Add(_abbath, _mayhem);

        Assert.Equal(["Abbath", "Varg"], Members(_mayhem));
        Assert.Contains("Abbath", Members(_immortal));
    }

    /// <summary>
    /// Agregar dos veces la misma membresía falla.
    /// </summary>
    [Fact]
    public void Add_Duplicate_Throws()
    {
        using var uow = new UnitOfWork(_db);

        Assert.Throws<SqliteException>
        (
            () => uow.Memberships.Add(_varg, _mayhem)
        );
    }

    /// <summary>
    /// Agregar una persona o un grupo inexistente falla.
    /// </summary>
    [Fact]
    public void Add_UnknownPersonOrGroup_Throws()
    {
        using var uow = new UnitOfWork(_db);

        Assert.Throws<SqliteException>(() => uow.Memberships.Add(999, _mayhem));
        Assert.Throws<SqliteException>(() => uow.Memberships.Add(_varg, 999));
    }

    /// <summary>
    /// RemoveAllOfGroup quita solo a los miembros de ese grupo.
    /// </summary>
    [Fact]
    public void RemoveAllOfGroup_RemovesOnlyThatGroup()
    {
        using (var uow = new UnitOfWork(_db))
            Assert.Equal(2, uow.Memberships.RemoveAllOfGroup(_immortal));

        Assert.Empty(Members(_immortal));
        Assert.Equal(["Varg"], Members(_mayhem));
    }

    /// <summary>
    /// RemoveAllOfPerson saca a la persona de todos sus grupos, sin tocar
    /// a los demás.
    /// </summary>
    [Fact]
    public void RemoveAllOfPerson_RemovesItFromAllItsGroups()
    {
        using (var uow = new UnitOfWork(_db))
        {
            uow.Memberships.Add(_abbath, _mayhem);
            Assert.Equal(2, uow.Memberships.RemoveAllOfPerson(_abbath));
        }

        Assert.Equal(["Demonaz"], Members(_immortal));
        Assert.Equal(["Varg"], Members(_mayhem));
    }

    /// <summary>
    /// Una persona o un grupo con membresías no se puede borrar hasta
    /// limpiarlas.
    /// </summary>
    [Fact]
    public void Delete_WithMemberships_FailsUntilCleaned()
    {
        using var uow = new UnitOfWork(_db);

        Assert.Throws<SqliteException>(() => uow.Persons.Delete(_varg));
        Assert.Throws<SqliteException>(() => uow.Groups.Delete(_mayhem));

        uow.Memberships.RemoveAllOfPerson(_varg);
        Assert.True(uow.Persons.Delete(_varg));
        Assert.True(uow.Groups.Delete(_mayhem));
    }
}
