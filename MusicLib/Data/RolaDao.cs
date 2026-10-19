using MusicLib.Models;

namespace MusicLib.Data;

internal sealed class RolaDao : IRolaDao
{
    private readonly UnitOfWork _uow;

    public RolaDao(UnitOfWork uow) => _uow = uow;

    public bool ExistsByPath(string path)
    {
        using var cmd = _uow.CreateCommand
        (
            "SELECT 1 FROM rolas WHERE path = $path LIMIT 1"
        );
        cmd.Parameters.AddWithValue("$path", path);

        return cmd.ExecuteScalar() is not null;
    }

    public long Insert(Rola rola)
    {
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