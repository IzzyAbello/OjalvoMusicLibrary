using MusicLib.Data;
using MusicLib.Models;
using MusicLib.Tests.Support;
using Xunit;

namespace MusicLib.Tests.Data;

/// <summary>
/// Pruebas de PersonsEditor contra una DB real.
/// </summary>
public sealed class PersonsEditorTests : IDisposable
{
    private readonly TempDir _tmp = new();
    private readonly string _db;
    private readonly PersonsEditor _editor;

    /// <summary>
    /// DB con:
    /// <list type="bullet">
    /// <item>"Burzum": intérprete Unknown con una rola.</item>
    /// <item>"Varg": persona (con su intérprete Person).</item>
    /// <item>"Mayhem": grupo.</item>
    /// </list>
    /// </summary>
    public PersonsEditorTests()
    {
        _db = _tmp.Combine("music.db");
        DatabaseInitializer.EnsureCreated(_db);
        _editor = new PersonsEditor(_db);

        using var uow = new UnitOfWork(_db);
        var burzum = uow.Performers.Insert(new Performer { Name = "Burzum" });
        uow.Rolas.Insert
        (
            new Rola
            {
                IdPerformer = burzum,
                Path = "/m/Dunkelheit.mp3",
                Title = "Dunkelheit"
            }
        );
        uow.Performers.Insert
        (
            new Performer { Name = "Varg", Type = PerformerType.Person }
        );
        uow.Persons.Insert(new Person { StageName = "Varg" });
        uow.Groups.Insert(new Group { Name = "Mayhem" });
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

    private Person? Person(string stageName)
    {
        using var uow = new UnitOfWork(_db);
        return uow.Persons.FindByStageName(stageName);
    }

    // Snapshot del count de tablas que Add puede tocar.
    private (int, int) Counts() =>
        (Db.Count(_db, "performers"), Db.Count(_db, "persons"));

    /// <summary>
    /// Datos correctos (fechas opcionales) no tienen problema.
    /// </summary>
    [Theory]
    [InlineData("Abbath", null, null)]
    [InlineData("Abbath", "1973-06-27", "")]
    [InlineData("Abbath", "1973-06-27", "1973-06-27")]
    public void Validate_ValidData_ReturnsNull
    (
        string name,
        string? birth,
        string? death
    ) => Assert.Null(PersonsEditor.Validate(name, birth, death));

    /// <summary>
    /// Cada dato inválido da su propio mensaje.
    /// </summary>
    [Theory]
    [InlineData("  ", null, null, "nombre")]
    [InlineData("Abbath", "27-06-1973", null, "nacimiento")]
    [InlineData("Abbath", null, "2024-02-30", "muerte")]
    [InlineData("Abbath", "2000-01-01", "1999-01-01", "antes")]
    public void Validate_InvalidData_SaysWhy
    (
        string name,
        string? birth,
        string? death,
        string expectedWord
    ) => Assert.Contains(expectedWord, PersonsEditor.Validate(name, birth, death));

    /// <summary>
    /// Un nombre nuevo crea su intérprete y la persona, con los
    /// campos recortados y los vacíos como null.
    /// </summary>
    [Fact]
    public void Add_NewName_CreatesPerformerAndPerson()
    {
        var id = _editor.Add(" Abbath ", " Olve Eikemo ", " 1973-06-27 ", "  ");

        Assert.Equal(PerformerType.Person, Performer("Abbath")!.Type);
        var abbath = Person("Abbath")!;
        Assert.Equal(id, abbath.Id);
        Assert.Equal("Olve Eikemo", abbath.RealName);
        Assert.Equal("1973-06-27", abbath.BirthDate);
        Assert.Null(abbath.DeathDate);
    }

    /// <summary>
    /// Un intérprete Unknown con ese nombre se define como persona: no se
    /// crea otro y sus rolas siguen apuntando a él.
    /// </summary>
    [Fact]
    public void Add_ExistingUnknownPerformer_BecomesPerson()
    {
        var before = Performer("Burzum")!;
        var performers = Db.Count(_db, "performers");

        _editor.Add("Burzum", null, null, null);

        var after = Performer("Burzum")!;
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(PerformerType.Person, after.Type);
        Assert.Equal(performers, Db.Count(_db, "performers"));
        Assert.NotNull(Person("Burzum"));
        Assert.Equal
        (
            before.Id,
            Db.Scalar<long>
            (
                _db,
                "SELECT id_performer FROM rolas WHERE title = 'Dunkelheit'"
            )
        );
    }

    /// <summary>
    /// El nombre se compara exacto.
    /// </summary>
    [Fact]
    public void Add_SameNameDifferentCase_IsAnotherPerson()
    {
        _editor.Add("varg", null, null, null);
        Assert.NotNull(Person("varg"));
    }

    /// <summary>
    /// Un nombre que ya es persona o grupo da error y no cambia nada.
    /// </summary>
    [Theory]
    [InlineData("Varg", "Ya existe la persona")]
    [InlineData(" Mayhem ", "ya es un grupo")]
    public void Add_NameAlreadyPersonOrGroup_ThrowsAndChangesNothing
    (
        string name,
        string expected
    )
    {
        var before = Counts();

        var ex = Assert.Throws<InvalidOperationException>
        (
            () => _editor.Add(name, null, null, null)
        );

        Assert.Contains(expected, ex.Message);
        Assert.Equal(before, Counts());
        Assert.Equal(PerformerType.Unknown, Performer("Burzum")!.Type);
    }

    /// <summary>
    /// Datos inválidos lanzan con el mensaje de Validate y no cambian nada.
    /// </summary>
    [Theory]
    [InlineData("", null, null)]
    [InlineData("Abbath", "1973", null)]
    [InlineData("Abbath", "2000-01-01", "1999-01-01")]
    public void Add_InvalidData_ThrowsAndChangesNothing
    (
        string name,
        string? birth,
        string? death
    )
    {
        var before = Counts();

        var ex = Assert.Throws<ArgumentException>
        (
            () => _editor.Add(name, null, birth, death)
        );

        Assert.Equal(PersonsEditor.Validate(name, birth, death), ex.Message);
        Assert.Equal(before, Counts());
    }
}
