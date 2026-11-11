using MusicLib.Logging;
using MusicLib.Models;

namespace MusicLib.Data;

/// <summary>
/// Ediciones de grupos hechas por el usuario.
/// </summary>
public sealed class GroupsEditor
{
    private readonly string _dbPath;
    private readonly ILog _log;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dbPath">
    /// Ruta de la DB.
    /// </param>
    /// <param name="log">
    /// Destino de los mensajes. Si es null, los mensajes se descartan.
    /// </param>
    public GroupsEditor(string dbPath, ILog? log = null)
    {
        _dbPath = dbPath;
        _log = log ?? NullLog.Instance;
    }

    /// <summary>
    /// Revisa los campos de un grupo para deshabilitar
    /// Guardar y decir por qué.
    /// </summary>
    /// <param name="name">Nombre del grupo; obligatorio.</param>
    /// <param name="startDate">Inicio; vacío o AAAA-MM-DD.</param>
    /// <param name="endDate">Disolución; vacío o AAAA-MM-DD.</param>
    /// <returns>
    /// El primer problema encontrado, o null si todo está bien.
    /// </returns>
    public static string? Validate
    (
        string? name,
        string? startDate,
        string? endDate
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            return "El nombre del grupo no puede estar vacío.";
        if (!DateText.IsValid(startDate))
            return "La fecha de inicio debe ser una fecha AAAA-MM-DD.";
        if (!DateText.IsValid(endDate))
            return "La fecha de disolución debe ser una fecha AAAA-MM-DD.";
        if (!DateText.IsInOrder(startDate, endDate))
            return "La fecha de disolución no puede ser antes del inicio.";
        return null;
    }

    /// <summary>
    /// Da de alta un grupo con sus miembros.
    /// </summary>
    /// <param name="name">Nombre del grupo; obligatorio.</param>
    /// <param name="startDate">Inicio; vacío o AAAA-MM-DD.</param>
    /// <param name="endDate">Disolución; vacío o AAAA-MM-DD.</param>
    /// <param name="memberIds">
    /// Ids de las personas miembro; los repetidos se ignoran.
    /// </param>
    /// <returns>El id del grupo nuevo.</returns>
    /// <exception cref="ArgumentException">
    /// Si los campos no pasan Validate, o algún miembro no es una persona
    /// existente.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Si ya existe un grupo o una persona con ese nombre.
    /// </exception>
    public long Add
    (
        string name,
        string? startDate,
        string? endDate,
        IEnumerable<long> memberIds
    )
    {
        if (Validate(name, startDate, endDate) is { } problem)
            throw new ArgumentException(problem);

        var clean = name.Trim();

        using var uow = new UnitOfWork(_dbPath, _log);
        uow.Begin();

        if (uow.Groups.FindByName(clean) is not null)
            throw new InvalidOperationException
            (
                $"Ya existe el grupo \"{clean}\"."
            );
        if (uow.Persons.FindByStageName(clean) is not null)
            throw new InvalidOperationException
            (
                $"\"{clean}\" ya es una persona."
            );

        var members = ExistingPersons(uow, memberIds);

        if (!uow.Performers.UpdateType(clean, PerformerType.Group))
            uow.Performers.Insert
            (
                new Performer { Name = clean, Type = PerformerType.Group }
            );

        var id = uow.Groups.Insert
        (
            new Group
            {
                Name = clean,
                StartDate = DateText.Normalize(startDate),
                EndDate = DateText.Normalize(endDate)
            }
        );
        foreach (var idPerson in members) uow.Memberships.Add(idPerson, id);

        uow.Commit();
        return id;
    }

    private static List<long> ExistingPersons
    (
        UnitOfWork uow,
        IEnumerable<long> ids
    )
    {
        var distinct = ids.Distinct().ToList();
        foreach (var id in distinct)
            if (uow.Persons.FindById(id) is null)
                throw new ArgumentException
                (
                    $"No existe la persona con id {id}."
                );
        return distinct;
    }
}
