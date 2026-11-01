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
}