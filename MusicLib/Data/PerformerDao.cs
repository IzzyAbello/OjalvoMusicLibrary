using MusicLib.Models;

namespace MusicLib.Data;

internal sealed class PerformerDao : IPerformerDao
{
    private readonly UnitOfWork _uow;

    public PerformerDao(UnitOfWork uow) => _uow = uow;

    public Performer? FindByName(string name)
    {
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
        using var cmd = _uow.CreateCommand(
            "INSERT INTO performers (id_type, name) VALUES ($type, $name) " +
            "RETURNING id_performer");
        cmd.Parameters.AddWithValue("$type", (int)performer.Type);
        cmd.Parameters.AddWithValue("$name", performer.Name);

        performer.Id = (long)cmd.ExecuteScalar()!;
        return performer.Id;
    }
}
