using Microsoft.Data.Sqlite;
using MusicLib.Logging;
using MusicLib.Models;

namespace MusicLib.Data;

internal sealed class GroupDao : IGroupDao
{
    private const string Select =
        "SELECT id_group, name, start_date, end_date FROM groups";

    private readonly UnitOfWork _uow;
    private readonly ILog _log;

    public GroupDao(UnitOfWork uow, ILog log)
    {
        _uow = uow;
        _log = log;
        _log.Write("GroupDao creado.");
    }

    public IReadOnlyList<Group> FindAll()
    {
        _log.Write("Leyendo todos los grupos.");
        using var cmd = _uow.CreateCommand
        (
            Select + " ORDER BY name, id_group"
        );

        using var r = cmd.ExecuteReader();
        var groups = new List<Group>();
        while (r.Read()) groups.Add(Read(r));
        return groups;
    }

    public Group? FindById(long id)
    {
        _log.Write($"Buscando Grupo por id: {id}");
        using var cmd = _uow.CreateCommand(Select + " WHERE id_group = $id");
        cmd.Parameters.AddWithValue("$id", id);
        return ReadOne(cmd);
    }

    public Group? FindByName(string name)
    {
        _log.Write($"Buscando Grupo por nombre: {name}");
        using var cmd = _uow.CreateCommand
        (
            Select + " WHERE name = $name ORDER BY id_group LIMIT 1"
        );
        cmd.Parameters.AddWithValue("$name", name);
        return ReadOne(cmd);
    }

    public long Insert(Group group)
    {
        _log.Write($"Insertando Grupo: {group.Name}");
        using var cmd = _uow.CreateCommand
        (
            "INSERT INTO groups (name, start_date, end_date) " +
            "VALUES ($name, $start, $end) " +
            "RETURNING id_group"
        );
        cmd.Parameters.AddWithValue("$name", group.Name);
        AddDates(cmd, group);

        group.Id = (long)cmd.ExecuteScalar()!;
        return group.Id;
    }

    public bool Update(Group group)
    {
        _log.Write($"Actualizando el Grupo {group.Id}: {group.Name}");
        using var cmd = _uow.CreateCommand
        (
            "UPDATE groups SET start_date = $start, end_date = $end " +
            "WHERE id_group = $id"
        );
        AddDates(cmd, group);
        cmd.Parameters.AddWithValue("$id", group.Id);

        return cmd.ExecuteNonQuery() == 1;
    }

    public bool Delete(long id)
    {
        _log.Write($"Borrando el Grupo {id}");
        using var cmd = _uow.CreateCommand
        (
            "DELETE FROM groups WHERE id_group = $id"
        );
        cmd.Parameters.AddWithValue("$id", id);

        return cmd.ExecuteNonQuery() == 1;
    }

    private static void AddDates(SqliteCommand cmd, Group group)
    {
        cmd.Parameters.AddWithValue("$start", Nullable(group.StartDate));
        cmd.Parameters.AddWithValue("$end", Nullable(group.EndDate));
    }

    private static object Nullable(string? value) =>
        (object?)value ?? DBNull.Value;

    private static Group? ReadOne(SqliteCommand cmd)
    {
        using var r = cmd.ExecuteReader();
        return r.Read() ? Read(r) : null;
    }

    private static Group Read(SqliteDataReader r) => new()
    {
        Id = r.GetInt64(0),
        Name = r.IsDBNull(1) ? "" : r.GetString(1),
        StartDate = r.IsDBNull(2) ? null : r.GetString(2),
        EndDate = r.IsDBNull(3) ? null : r.GetString(3)
    };
}
