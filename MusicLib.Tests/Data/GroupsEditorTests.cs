using MusicLib.Data;
using MusicLib.Models;
using MusicLib.Tests.Support;
using Xunit;

namespace MusicLib.Tests.Data;

/// <summary>
/// Pruebas de GroupsEditor contra una DB real.
/// </summary>
public sealed class GroupsEditorTests : IDisposable
{
    private readonly TempDir _tmp = new();
    private readonly string _db;
    private readonly GroupsEditor _editor;
    private readonly long _abbath;
    private readonly long _demonaz;
    private readonly long _mayhem;

    /// <summary>
    /// DB con:
    /// <list type="bullet">
    /// <item>"Immortal": intérprete Unknown con una rola.</item>
    /// <item>"Abbath" y "Demonaz": personas.</item>
    /// <item>"Mayhem": grupo (inicio 1984-01-01), con Abbath como
    /// miembro.</item>
    /// </list>
    /// </summary>
    public GroupsEditorTests()
    {
        _db = _tmp.Combine("music.db");
        DatabaseInitializer.EnsureCreated(_db);
        _editor = new GroupsEditor(_db);

        using var uow = new UnitOfWork(_db);
        var immortal = uow.Performers.Insert
        (
            new Performer { Name = "Immortal" }
        );
        uow.Rolas.Insert
        (
            new Rola
            {
                IdPerformer = immortal,
                Path = "/m/Tyrants.mp3",
                Title = "Tyrants"
            }
        );
        _abbath = uow.Persons.Insert(new Person { StageName = "Abbath" });
        _demonaz = uow.Persons.Insert(new Person { StageName = "Demonaz" });
        uow.Performers.Insert
        (
            new Performer { Name = "Mayhem", Type = PerformerType.Group }
        );
        _mayhem = uow.Groups.Insert
        (
            new Group { Name = "Mayhem", StartDate = "1984-01-01" }
        );
        uow.Memberships.Add(_abbath, _mayhem);
    }

    /// <summary>
    /// Borra el directorio temporal.
    /// </summary>
    public void Dispose() => _tmp.Dispose();

    private Performer? Performer(string name)
    {
        using var uow = new UnitOfWork(_db);
        return uow.Performers.FindByName(name);
    }

    private Group? Group(string name)
    {
        using var uow = new UnitOfWork(_db);
        return uow.Groups.FindByName(name);
    }

    private List<string> Members(long idGroup)
    {
        using var uow = new UnitOfWork(_db);
        return uow.Memberships.Members(idGroup)
            .Select(p => p.StageName)
            .ToList();
    }

    // Snapshot del count de las tablas que Add puede tocar.
    private (int, int, int) Counts() =>
    (
        Db.Count(_db, "performers"),
        Db.Count(_db, "groups"),
        Db.Count(_db, "in_group")
    );

    /// <summary>
    /// Datos correctos no tienen problema.
    /// </summary>
    [Theory]
    [InlineData("Darkthrone", null, null)]
    [InlineData("Darkthrone", "1986-01-01", "")]
    [InlineData("Darkthrone", "1986-01-01", "1986-01-01")]
    public void Validate_ValidData_ReturnsNull
    (
        string name,
        string? start,
        string? end
    ) => Assert.Null(GroupsEditor.Validate(name, start, end));

    /// <summary>
    /// Cada dato inválido da su propio mensaje.
    /// </summary>
    [Theory]
    [InlineData("  ", null, null, "nombre")]
    [InlineData("Darkthrone", "1986", null, "inicio")]
    [InlineData("Darkthrone", null, "2024-02-30", "disolución")]
    [InlineData("Darkthrone", "2000-01-01", "1999-01-01", "antes")]
    public void Validate_InvalidData_SaysWhy
    (
        string name,
        string? start,
        string? end,
        string expectedWord
    ) => Assert.Contains(expectedWord, GroupsEditor.Validate(name, start, end));

    /// <summary>
    /// Un nombre nuevo crea su intérprete, el grupo y sus miembros.
    /// </summary>
    [Fact]
    public void Add_NewName_CreatesPerformerGroupAndMembers()
    {
        var id = _editor.Add
        (
            " Darkthrone ", " 1986-01-01 ", " ", [_demonaz, _abbath]
        );

        Assert.Equal(PerformerType.Group, Performer("Darkthrone")!.Type);
        var group = Group("Darkthrone")!;
        Assert.Equal(id, group.Id);
        Assert.Equal("1986-01-01", group.StartDate);
        Assert.Null(group.EndDate);
        Assert.Equal(["Abbath", "Demonaz"], Members(id));
    }

    /// <summary>
    /// Un grupo puede crearse sin miembros.
    /// </summary>
    [Fact]
    public void Add_WithoutMembers_IsAllowed()
    {
        var id = _editor.Add("Darkthrone", null, null, []);
        Assert.Empty(Members(id));
    }

    /// <summary>
    /// Los ids repetidos se ignoran.
    /// </summary>
    [Fact]
    public void Add_RepeatedMembers_AreAddedOnce()
    {
        var id = _editor.Add("Darkthrone", null, null, [_abbath, _abbath]);
        Assert.Equal(["Abbath"], Members(id));
    }

    /// <summary>
    /// Un intérprete Unknown con ese nombre se define como grupo y 
    /// sus rolas siguen apuntando a él.
    /// </summary>
    [Fact]
    public void Add_ExistingUnknownPerformer_BecomesGroup()
    {
        var before = Performer("Immortal")!;
        var performers = Db.Count(_db, "performers");

        _editor.Add("Immortal", null, null, [_abbath]);

        var after = Performer("Immortal")!;
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(PerformerType.Group, after.Type);
        Assert.Equal(performers, Db.Count(_db, "performers"));
        Assert.Equal
        (
            before.Id,
            Db.Scalar<long>
            (
                _db,
                "SELECT id_performer FROM rolas WHERE title = 'Tyrants'"
            )
        );
    }

    /// <summary>
    /// Un nombre que ya es grupo o persona da error y no cambia nada.
    /// </summary>
    [Theory]
    [InlineData("Mayhem", "Ya existe el grupo")]
    [InlineData(" Abbath ", "ya es una persona")]
    public void Add_NameAlreadyGroupOrPerson_ThrowsAndChangesNothing
    (
        string name,
        string expected
    )
    {
        var before = Counts();

        var ex = Assert.Throws<InvalidOperationException>
        (
            () => _editor.Add(name, null, null, [_abbath])
        );

        Assert.Contains(expected, ex.Message);
        Assert.Equal(before, Counts());
    }

    /// <summary>
    /// Datos inválidos lanzan con el mensaje de Validate y no cambian nada.
    /// </summary>
    [Theory]
    [InlineData("", null, null)]
    [InlineData("Darkthrone", "1986", null)]
    [InlineData("Darkthrone", "2000-01-01", "1999-01-01")]
    public void Add_InvalidData_ThrowsAndChangesNothing
    (
        string name,
        string? start,
        string? end
    )
    {
        var before = Counts();

        var ex = Assert.Throws<ArgumentException>
        (
            () => _editor.Add(name, start, end, [])
        );

        Assert.Equal(GroupsEditor.Validate(name, start, end), ex.Message);
        Assert.Equal(before, Counts());
    }

    /// <summary>
    /// Un miembro que no es persona da error y no se guarda nada.
    /// </summary>
    [Fact]
    public void Add_UnknownMember_ThrowsAndChangesNothing()
    {
        var before = Counts();

        Assert.Throws<ArgumentException>
        (
            () => _editor.Add("Immortal", null, null, [_abbath, 999])
        );

        Assert.Equal(before, Counts());
        Assert.Equal(PerformerType.Unknown, Performer("Immortal")!.Type);
    }

    private Group Mayhem()
    {
        using var uow = new UnitOfWork(_db);
        return uow.Groups.FindById(_mayhem)!;
    }

    /// <summary>
    /// Update cambia las fechas y deja el nombre.
    /// </summary>
    [Fact]
    public void Update_ChangesDates_KeepsName()
    {
        Assert.True(_editor.Update(_mayhem, " ", " 2000-01-01 ", [_abbath]));

        var mayhem = Mayhem();
        Assert.Equal("Mayhem", mayhem.Name);
        Assert.Null(mayhem.StartDate);
        Assert.Equal("2000-01-01", mayhem.EndDate);
    }

    /// <summary>
    /// La lista de miembros reemplaza a la anterior.
    /// </summary>
    [Fact]
    public void Update_ReplacesMembers()
    {
        _editor.Update(_mayhem, null, null, [_demonaz]);
        Assert.Equal(["Demonaz"], Members(_mayhem));
    }

    /// <summary>
    /// Una lista vacía deja al grupo sin miembros.
    /// </summary>
    [Fact]
    public void Update_EmptyMembers_LeavesGroupWithoutMembers()
    {
        _editor.Update(_mayhem, null, null, []);

        Assert.Empty(Members(_mayhem));
        Assert.Equal(2, Db.Count(_db, "persons"));
    }

    /// <summary>
    /// Solo cambia la membresía de ese grupo.
    /// </summary>
    [Fact]
    public void Update_DoesNotTouchOtherGroups()
    {
        var darkthrone = _editor.Add("Darkthrone", null, null, [_abbath]);

        _editor.Update(_mayhem, null, null, []);

        Assert.Equal(["Abbath"], Members(darkthrone));
    }

    /// <summary>
    /// Fechas inválidas o un miembro inexistente lanzan y no cambian nada.
    /// </summary>
    [Theory]
    [InlineData("1984", null, false)]
    [InlineData("2000-01-01", "1999-01-01", false)]
    [InlineData(null, null, true)]
    public void Update_InvalidData_ThrowsAndChangesNothing
    (
        string? start,
        string? end,
        bool unknownMember
    )
    {
        long[] members = unknownMember ? [_demonaz, 999] : [_demonaz];

        Assert.Throws<ArgumentException>
        (
            () => _editor.Update(_mayhem, start, end, members)
        );

        Assert.Equal("1984-01-01", Mayhem().StartDate);
        Assert.Equal(["Abbath"], Members(_mayhem));
    }

    /// <summary>
    /// Update con un id inexistente regresa false.
    /// </summary>
    [Fact]
    public void Update_UnknownId_ReturnsFalse() =>
        Assert.False(_editor.Update(999, null, null, []));

    /// <summary>
    /// Delete borra el grupo y sus membresías; las personas se quedan.
    /// </summary>
    [Fact]
    public void Delete_RemovesGroupAndItsMemberships()
    {
        Assert.True(_editor.Delete(_mayhem));

        Assert.Null(Group("Mayhem"));
        Assert.Equal(0, Db.Count(_db, "in_group"));
        Assert.Equal(2, Db.Count(_db, "persons"));
    }

    /// <summary>
    /// Su intérprete regresa a Unknown y no se borra.
    /// </summary>
    [Fact]
    public void Delete_PerformerGoesBackToUnknown()
    {
        var performers = Db.Count(_db, "performers");

        _editor.Delete(_mayhem);

        Assert.Equal(PerformerType.Unknown, Performer("Mayhem")!.Type);
        Assert.Equal(performers, Db.Count(_db, "performers"));
    }

    /// <summary>
    /// Un grupo eliminado se puede volver a dar de alta.
    /// </summary>
    [Fact]
    public void Delete_ThenAdd_ReusesThePerformer()
    {
        var id = Performer("Mayhem")!.Id;

        _editor.Delete(_mayhem);
        _editor.Add("Mayhem", null, null, []);

        Assert.Equal(id, Performer("Mayhem")!.Id);
        Assert.Equal(PerformerType.Group, Performer("Mayhem")!.Type);
    }

    /// <summary>
    /// Delete con un id inexistente regresa false y no cambia nada.
    /// </summary>
    [Fact]
    public void Delete_UnknownId_ReturnsFalse()
    {
        var before = Counts();

        Assert.False(_editor.Delete(999));
        Assert.Equal(before, Counts());
    }
}
