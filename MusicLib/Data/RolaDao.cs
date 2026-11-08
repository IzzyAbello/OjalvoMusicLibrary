using Microsoft.Data.Sqlite;
using MusicLib.Logging;
using MusicLib.Models;

namespace MusicLib.Data;

internal sealed class RolaDao : IRolaDao
{
    private readonly UnitOfWork _uow;
    private readonly ILog _log;

    public RolaDao(UnitOfWork uow, ILog log)
    {
        _uow = uow;
        _log = log;
        _log.Write("RolaDao creada.");
    }

    public bool ExistsByPath(string path)
    {
        _log.Write($"Comprobando si existe la Rola con la ruta: {path}");
        using var cmd = _uow.CreateCommand
        (
            "SELECT 1 FROM rolas WHERE path = $path LIMIT 1"
        );
        cmd.Parameters.AddWithValue("$path", path);

        return cmd.ExecuteScalar() is not null;
    }

    public long Insert(Rola rola)
    {
        _log.Write($"Insertando la Rola: {rola.Title}");
        using var cmd = _uow.CreateCommand
        (
            "INSERT INTO rolas (id_performer, id_album, path, title, track, year, genre) " +
            "VALUES ($perf, $album, $path, $title, $track, $year, $genre) " +
            "RETURNING id_rola"
        );

        cmd.Parameters.AddWithValue("$perf", rola.IdPerformer);
        cmd.Parameters.AddWithValue("$album", rola.IdAlbum);
        cmd.Parameters.AddWithValue("$path", rola.Path);
        cmd.Parameters.AddWithValue("$title", rola.Title);
        cmd.Parameters.AddWithValue("$track", rola.Track);
        cmd.Parameters.AddWithValue("$year", rola.Year);
        cmd.Parameters.AddWithValue("$genre", rola.Genre);

        rola.Id = (long)cmd.ExecuteScalar()!;
        return rola.Id;
    }

    public IReadOnlyList<Rola> FindFirst
    (
        int count = IRolaDao.DefaultFindFirstCount
    ) => Search(new RolaSearch(), count);

    public IReadOnlyList<Rola> Search
    (
        RolaSearch criteria,
        int count = IRolaDao.DefaultSearchCount
    )
    {
        ArgumentNullException.ThrowIfNull(criteria);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        _log.Write($"Buscando rolas: {criteria} (máximo {count}).");
        var (sql, parameters) = RolaSearchSql.Build(criteria, count);

        using var cmd = _uow.CreateCommand(sql);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);

        using var r = cmd.ExecuteReader();
        var rolas = new List<Rola>();
        while (r.Read()) rolas.Add(Read(r));

        return rolas;
    }

    // Lee una fila con las columnas de RolaSearchSql.Select.
    private static Rola Read(SqliteDataReader r)
    {
        long idPerformer = r.IsDBNull(1) ? 0 : r.GetInt64(1);
        long idAlbum = r.IsDBNull(2) ? Album.UnknownId : r.GetInt64(2);

        return new Rola
        {
            Id = r.GetInt64(0),
            IdPerformer = idPerformer,
            IdAlbum = idAlbum,
            Path = r.IsDBNull(3) ? "" : r.GetString(3),
            Title = r.IsDBNull(4) ? "" : r.GetString(4),
            Track = r.IsDBNull(5) ? 0 : r.GetInt32(5),
            Year = r.IsDBNull(6) ? 0 : r.GetInt32(6),
            Genre = r.IsDBNull(7) ? "" : r.GetString(7),
            Performer = new Performer
            {
                Id = idPerformer,
                Type = r.IsDBNull(8) ?
                    PerformerType.Unknown : (PerformerType)r.GetInt32(8),
                Name = r.IsDBNull(9) ? "" : r.GetString(9)
            },
            Album = new Album
            {
                Id = idAlbum,
                Path = r.IsDBNull(10) ? "" : r.GetString(10),
                Name = r.IsDBNull(11) ? "" : r.GetString(11),
                Year = r.IsDBNull(12) ? 0 : r.GetInt32(12)
            }
        };
    }
}
