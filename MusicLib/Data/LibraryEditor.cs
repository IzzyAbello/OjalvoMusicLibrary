using MusicLib.Logging;
using MusicLib.Models;

namespace MusicLib.Data;

/// <summary>
/// Ediciones de la biblioteca hechas por el usuario. Cada operación valida
/// y luego cambia la DB en una sola transacción.
/// </summary>
public sealed class LibraryEditor
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
    public LibraryEditor(string dbPath, ILog? log = null)
    {
        _dbPath = dbPath;
        _log = log ?? NullLog.Instance;
    }

    /// <summary>
    /// Edita una rola. El intérprete y el álbum se reasignan solo para
    /// esta rola: si ya existe uno con ese nombre se usa; si no, se crea.
    /// El álbum se busca en el directorio de la rola (nombre + directorio)
    /// y, si el nombre no cambió, la rola se queda en su álbum actual.
    /// </summary>
    /// <param name="id">Id de la rola.</param>
    /// <param name="title">Título nuevo; no puede estar vacío.</param>
    /// <param name="performerName">
    /// Nombre del intérprete; no puede estar vacío.
    /// </param>
    /// <param name="albumName">
    /// Nombre del álbum; no puede estar vacío. Un álbum nuevo toma el año
    /// de la rola.
    /// </param>
    /// <param name="track">Pista; no negativa.</param>
    /// <param name="year">Año; no negativo.</param>
    /// <param name="genre">Género; vacío se guarda como Rola.UnknownGenre.</param>
    /// <returns>
    /// true si se guardó; false si no existe una rola con ese id (y
    /// entonces no se crea ningún intérprete ni álbum).
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Si el título, el intérprete o el álbum están vacíos, o la pista o
    /// el año son negativos.
    /// </exception>
    public bool UpdateRola
    (
        long id,
        string title,
        string performerName,
        string albumName,
        int track,
        int year,
        string genre
    )
    {
        var cleanTitle = Required(title, "El título no puede estar vacío.");
        var cleanPerformer = Required
        (
            performerName, "El intérprete no puede estar vacío."
        );
        var cleanAlbum = Required
        (
            albumName, "El álbum no puede estar vacío."
        );
        if (track < 0)
            throw new ArgumentException("La pista no puede ser negativa.");
        if (year < 0)
            throw new ArgumentException("El año no puede ser negativo.");

        using var uow = new UnitOfWork(_dbPath, _log);
        uow.Begin();

        var rola = uow.Rolas.FindById(id);
        if (rola is null) return false;

        long idPerformer = uow.Performers.FindByName(cleanPerformer)?.Id
            ?? uow.Performers.Insert
            (
                new Performer
                {
                    Name = cleanPerformer,
                    Type = PerformerType.Unknown
                }
            );

        long idAlbum = rola.IdAlbum;
        if (cleanAlbum != rola.Album?.Name)
        {
            var dir = Path.GetDirectoryName(rola.Path) ?? "";
            idAlbum = uow.Albums.FindByNameAndPath(cleanAlbum, dir)?.Id
                ?? uow.Albums.Insert
                (
                    new Album { Name = cleanAlbum, Path = dir, Year = year }
                );
        }

        var updated = uow.Rolas.Update
        (
            new Rola
            {
                Id = id,
                Title = cleanTitle,
                IdPerformer = idPerformer,
                IdAlbum = idAlbum,
                Track = track,
                Year = year,
                Genre = string.IsNullOrWhiteSpace(genre)
                    ? Rola.UnknownGenre : genre.Trim()
            }
        );
        if (!updated) return false;

        uow.Commit();
        return true;
    }

    /// <summary>
    /// Edita un álbum (nombre y año); todas sus rolas ven el cambio.
    /// </summary>
    /// <param name="id">Id del álbum.</param>
    /// <param name="name">Nombre nuevo; no puede estar vacío.</param>
    /// <param name="year">Año; no negativo.</param>
    /// <returns>
    /// true si se guardó; false si no existe un álbum con ese id.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Si el nombre está vacío o el año es negativo.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Si es el álbum comodín "Unknown" (lo comparten todas las rolas sin
    /// álbum), o si ya hay otro álbum con ese nombre en el mismo
    /// directorio (el álbum se identifica por nombre + directorio).
    /// </exception>
    public bool UpdateAlbum(long id, string name, int year)
    {
        if (id == Album.UnknownId)
            throw new InvalidOperationException
            (
                "El álbum \"Unknown\" no se puede editar."
            );

        var cleanName = Required(name, "El nombre del álbum no puede estar vacío.");
        if (year < 0)
            throw new ArgumentException("El año no puede ser negativo.");

        using var uow = new UnitOfWork(_dbPath, _log);
        uow.Begin();

        var album = uow.Albums.FindById(id);
        if (album is null) return false;

        var same = uow.Albums.FindByNameAndPath(cleanName, album.Path);
        if (same is not null && same.Id != id)
            throw new InvalidOperationException
            (
                $"Ya existe el álbum \"{cleanName}\" en ese directorio."
            );

        album.Name = cleanName;
        album.Year = year;
        uow.Albums.Update(album);

        uow.Commit();
        return true;
    }

    private static string Required(string? text, string message) =>
        string.IsNullOrWhiteSpace(text)
            ? throw new ArgumentException(message)
            : text.Trim();
}
