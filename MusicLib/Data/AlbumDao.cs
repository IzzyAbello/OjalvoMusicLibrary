using MusicLib.Models;

namespace MusicLib.Data;

internal sealed class AlbumDao : IAlbumDao
{
    private readonly UnitOfWork _uow;

    public AlbumDao(UnitOfWork uow) => _uow = uow;

    public Album? FindByNameAndPath(string name, string path)
    {
        using var cmd = _uow.CreateCommand
        (
            "SELECT id_album, path, name, year FROM albums " +
            "WHERE name = $name AND path = $path " +
            "ORDER BY id_album LIMIT 1"
        );
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$path", path);

        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        return new Album
        {
            Id = r.GetInt64(0),
            Path = r.IsDBNull(1) ? "" : r.GetString(1),
            Name = r.IsDBNull(2) ? "" : r.GetString(2),
            Year = r.IsDBNull(3) ? 0 : r.GetInt32(3)
        };
    }

    public long Insert(Album album)
    {
        using var cmd = _uow.CreateCommand
        (
            "INSERT INTO albums (path, name, year) VALUES ($path, $name, $year) " +
            "RETURNING id_album"
        );
        cmd.Parameters.AddWithValue("$path", album.Path);
        cmd.Parameters.AddWithValue("$name", album.Name);
        cmd.Parameters.AddWithValue("$year", album.Year);

        album.Id = (long)cmd.ExecuteScalar()!;
        return album.Id;
    }
}
