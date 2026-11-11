using MusicLib.Data;
using MusicLib.Models;
using MusicLib.Tests.Support;
using Xunit;

namespace MusicLib.Tests.Data;

/// <summary>
/// Pruebas de PerformerDao.UpdateType contra una DB real.
/// </summary>
public sealed class PerformerDaoTests : IDisposable
{
    private readonly TempDir _tmp = new();
    private readonly string _db;

    /// <summary>
    /// Crea una DB con tres intérpretes Unknown: "Burzum", "burzum" y
    /// "Mayhem".
    /// </summary>
    public PerformerDaoTests()
    {
        _db = _tmp.Combine("music.db");
        DatabaseInitializer.EnsureCreated(_db);

        using var uow = new UnitOfWork(_db);
        foreach (var name in new[] { "Burzum", "burzum", "Mayhem" })
            uow.Performers.Insert(new Performer { Name = name });
    }

    /// <summary>
    /// Borra el directorio temporal.
    /// </summary>
    public void Dispose() => _tmp.Dispose();

    private PerformerType TypeOf(string name)
    {
        using var uow = new UnitOfWork(_db);
        return uow.Performers.FindByName(name)!.Type;
    }

    private bool UpdateType(string name, PerformerType type)
    {
        using var uow = new UnitOfWork(_db);
        return uow.Performers.UpdateType(name, type);
    }

    /// <summary>
    /// Cambia el tipo del intérprete con ese nombre y nada más.
    /// </summary>
    [Fact]
    public void UpdateType_ChangesOnlyThatName()
    {
        Assert.True(UpdateType("Burzum", PerformerType.Person));

        Assert.Equal(PerformerType.Person, TypeOf("Burzum"));
        Assert.Equal(PerformerType.Unknown, TypeOf("burzum"));
        Assert.Equal(PerformerType.Unknown, TypeOf("Mayhem"));
    }

    /// <summary>
    /// También sirve para regresar un intérprete a Unknown.
    /// </summary>
    [Fact]
    public void UpdateType_CanGoBackToUnknown()
    {
        UpdateType("Burzum", PerformerType.Person);
        UpdateType("Burzum", PerformerType.Unknown);

        Assert.Equal(PerformerType.Unknown, TypeOf("Burzum"));
    }

    /// <summary>
    /// Un nombre que no existe no cambia nada y regresa false.
    /// </summary>
    [Fact]
    public void UpdateType_UnknownName_ReturnsFalse() =>
        Assert.False(UpdateType("Darkthrone", PerformerType.Group));
}
