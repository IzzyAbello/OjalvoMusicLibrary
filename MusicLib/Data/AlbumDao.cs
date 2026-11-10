using MusicLib.Logging;
using MusicLib.Models;

namespace MusicLib.Data;

internal sealed class AlbumDao : IAlbumDao
{
    private readonly UnitOfWork _uow;
    private readonly ILog _log;

    public AlbumDao(UnitOfWork uow, ILog log)
    {
        _uow = uow;
        _log = log;
        _log.Write("AlbumDao creado.");
    }

    public Album? FindByNameAndPath(string name, string path)
    {
        _log.Write($"Buscando Album por nombre: {name} y ruta: {path}");
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
        _log.Write($"Insertando Album: {album.Name}");
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

    public bool Update(Album album)
    {
        _log.Write($"Actualizando el Album {album.Id}: {album.Name}");
        using var cmd = _uow.CreateCommand
        (
            "UPDATE albums SET name = $name, year = $year " +
            "WHERE id_album = $id"
        );
        cmd.Parameters.AddWithValue("$name", album.Name);
        cmd.Parameters.AddWithValue("$year", album.Year);
        cmd.Parameters.AddWithValue("$id", album.Id);

        return cmd.ExecuteNonQuery() == 1;
    }
}
