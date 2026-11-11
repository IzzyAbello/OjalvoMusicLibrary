using MusicLib.Logging;
using MusicLib.Models;

namespace MusicLib.Data;

/// <summary>
/// Ediciones de personas hechas por el usuario. 
/// Cada operación valida y luego cambia la DB 
/// en una sola transacción.
/// </summary>
public sealed class PersonsEditor
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
    public PersonsEditor(string dbPath, ILog? log = null)
    {
        _dbPath = dbPath;
        _log = log ?? NullLog.Instance;
    }

    /// <summary>
    /// Revisa los campos de una persona para deshabilitar Guardar y decir
    /// por qué.
    /// </summary>
    /// <param name="stageName">Nombre artístico; obligatorio.</param>
    /// <param name="birthDate">Nacimiento; vacío o AAAA-MM-DD.</param>
    /// <param name="deathDate">Muerte; vacío o AAAA-MM-DD.</param>
    /// <returns>
    /// El primer problema encontrado, o null si todo está bien.
    /// </returns>
    public static string? Validate
    (
        string? stageName,
        string? birthDate,
        string? deathDate
    )
    {
        if (string.IsNullOrWhiteSpace(stageName))
            return "El nombre artístico no puede estar vacío.";
        if (!DateText.IsValid(birthDate))
            return "La fecha de nacimiento debe ser una fecha AAAA-MM-DD.";
        if (!DateText.IsValid(deathDate))
            return "La fecha de muerte debe ser una fecha AAAA-MM-DD.";
        if (!DateText.IsInOrder(birthDate, deathDate))
            return "La fecha de muerte no puede ser antes del nacimiento.";
        return null;
    }

    /// <summary>
    /// Da de alta una persona.
    /// </summary>
    /// <param name="stageName">Nombre artístico; obligatorio.</param>
    /// <param name="realName">Nombre real; vacío se guarda como NULL.</param>
    /// <param name="birthDate">Nacimiento; vacío o AAAA-MM-DD.</param>
    /// <param name="deathDate">Muerte; vacío o AAAA-MM-DD.</param>
    /// <returns>El id de la persona nueva.</returns>
    /// <exception cref="ArgumentException">
    /// Si los campos no pasan Validate.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Si ya existe una persona o un grupo con ese nombre.
    /// </exception>
    public long Add
    (
        string stageName,
        string? realName,
        string? birthDate,
        string? deathDate
    )
    {
        if (Validate(stageName, birthDate, deathDate) is { } problem)
            throw new ArgumentException(problem);

        var name = stageName.Trim();

        using var uow = new UnitOfWork(_dbPath, _log);
        uow.Begin();

        if (uow.Persons.FindByStageName(name) is not null)
            throw new InvalidOperationException
            (
                $"Ya existe la persona \"{name}\"."
            );
        if (uow.Groups.FindByName(name) is not null)
            throw new InvalidOperationException($"\"{name}\" ya es un grupo.");

        if (!uow.Performers.UpdateType(name, PerformerType.Person))
            uow.Performers.Insert
            (
                new Performer { Name = name, Type = PerformerType.Person }
            );

        var id = uow.Persons.Insert
        (
            new Person
            {
                StageName = name,
                RealName = DateText.Normalize(realName),
                BirthDate = DateText.Normalize(birthDate),
                DeathDate = DateText.Normalize(deathDate)
            }
        );

        uow.Commit();
        return id;
    }
}
