using Microsoft.Data.Sqlite;
using MusicLib.Logging;
using MusicLib.Models;

namespace MusicLib.Data;

internal sealed class PersonDao : IPersonDao
{
    internal const string Select =
        "SELECT id_person, stage_name, real_name, birth_date, death_date " +
        "FROM persons";

    private readonly UnitOfWork _uow;
    private readonly ILog _log;

    public PersonDao(UnitOfWork uow, ILog log)
    {
        _uow = uow;
        _log = log;
        _log.Write("PersonDao creado.");
    }

    public IReadOnlyList<Person> FindAll()
    {
        _log.Write("Leyendo todas las personas.");
        using var cmd = _uow.CreateCommand
        (
            Select + " ORDER BY stage_name, id_person"
        );

        using var r = cmd.ExecuteReader();
        var persons = new List<Person>();
        while (r.Read()) persons.Add(Read(r));
        return persons;
    }

    public Person? FindById(long id)
    {
        _log.Write($"Buscando Persona por id: {id}");
        using var cmd = _uow.CreateCommand(Select + " WHERE id_person = $id");
        cmd.Parameters.AddWithValue("$id", id);
        return ReadOne(cmd);
    }

    public Person? FindByStageName(string stageName)
    {
        _log.Write($"Buscando Persona por nombre: {stageName}");
        using var cmd = _uow.CreateCommand
        (
            Select + " WHERE stage_name = $name ORDER BY id_person LIMIT 1"
        );
        cmd.Parameters.AddWithValue("$name", stageName);
        return ReadOne(cmd);
    }

    public long Insert(Person person)
    {
        _log.Write($"Insertando Persona: {person.StageName}");
        using var cmd = _uow.CreateCommand
        (
            "INSERT INTO persons " +
            "(stage_name, real_name, birth_date, death_date) " +
            "VALUES ($stage, $real, $birth, $death) " +
            "RETURNING id_person"
        );
        cmd.Parameters.AddWithValue("$stage", person.StageName);
        AddDetails(cmd, person);

        person.Id = (long)cmd.ExecuteScalar()!;
        return person.Id;
    }

    public bool Update(Person person)
    {
        _log.Write($"Actualizando la Persona {person.Id}: {person.StageName}");
        using var cmd = _uow.CreateCommand
        (
            "UPDATE persons SET real_name = $real, birth_date = $birth, " +
            "death_date = $death WHERE id_person = $id"
        );
        AddDetails(cmd, person);
        cmd.Parameters.AddWithValue("$id", person.Id);

        return cmd.ExecuteNonQuery() == 1;
    }

    public bool Delete(long id)
    {
        _log.Write($"Borrando la Persona {id}");
        using var cmd = _uow.CreateCommand
        (
            "DELETE FROM persons WHERE id_person = $id"
        );
        cmd.Parameters.AddWithValue("$id", id);

        return cmd.ExecuteNonQuery() == 1;
    }

    private static void AddDetails(SqliteCommand cmd, Person person)
    {
        cmd.Parameters.AddWithValue("$real", Nullable(person.RealName));
        cmd.Parameters.AddWithValue("$birth", Nullable(person.BirthDate));
        cmd.Parameters.AddWithValue("$death", Nullable(person.DeathDate));
    }

    private static object Nullable(string? value) =>
        (object?)value ?? DBNull.Value;

    private static Person? ReadOne(SqliteCommand cmd)
    {
        using var r = cmd.ExecuteReader();
        return r.Read() ? Read(r) : null;
    }

    internal static Person Read(SqliteDataReader r) => new()
    {
        Id = r.GetInt64(0),
        StageName = r.IsDBNull(1) ? "" : r.GetString(1),
        RealName = r.IsDBNull(2) ? null : r.GetString(2),
        BirthDate = r.IsDBNull(3) ? null : r.GetString(3),
        DeathDate = r.IsDBNull(4) ? null : r.GetString(4)
    };
}
