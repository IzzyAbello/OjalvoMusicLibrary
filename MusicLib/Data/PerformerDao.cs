using MusicLib.Logging;
using MusicLib.Models;

namespace MusicLib.Data;

internal sealed class PerformerDao : IPerformerDao
{
    private readonly UnitOfWork _uow;
    private readonly ILog _log;

    public PerformerDao(UnitOfWork uow, ILog log)
    {
        _uow = uow;
        _log = log;
        _log.Write("PerformerDao creado.");
    }

    public Performer? FindByName(string name)
    {
        _log.Write($"Buscando Performer por nombre: {name}");
        using var cmd = _uow.CreateCommand
        (
            "SELECT id_performer, id_type, name FROM performers " +
            "WHERE name = $name " +
            "ORDER BY id_performer LIMIT 1"
        );
        cmd.Parameters.AddWithValue("$name", name);

        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        return new Performer
        {
            Id = r.GetInt64(0),
            Type = r.IsDBNull(1) ? 
                PerformerType.Unknown : (PerformerType)r.GetInt32(1),
            Name = r.IsDBNull(2) ? "" : r.GetString(2)
        };
    }

    public long Insert(Performer performer)
    {
        _log.Write($"Insertando Performer: {performer.Name}");
        using var cmd = _uow.CreateCommand(
            "INSERT INTO performers (id_type, name) VALUES ($type, $name) " +
            "RETURNING id_performer");
        cmd.Parameters.AddWithValue("$type", (int)performer.Type);
        cmd.Parameters.AddWithValue("$name", performer.Name);

        performer.Id = (long)cmd.ExecuteScalar()!;
        return performer.Id;
    }

    public bool UpdateType(string name, PerformerType type)
    {
        _log.Write($"Cambiando el tipo del Performer '{name}' a {type}");
        using var cmd = _uow.CreateCommand
        (
            "UPDATE performers SET id_type = $type WHERE name = $name"
        );
        cmd.Parameters.AddWithValue("$type", (int)type);
        cmd.Parameters.AddWithValue("$name", name);
        return cmd.ExecuteNonQuery() == 1;
    }
}
