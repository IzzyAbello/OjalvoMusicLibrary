using MusicLib.Models;

namespace MusicLib.Data;

/// <summary>
/// DAO para Album.
/// </summary>
public interface IAlbumDao
{
    /// <summary>
    /// Busca por nombre literal + directorio.
    /// </summary>
    Album? FindByNameAndPath(string name, string path);

    /// <summary>
    /// Inserta y devuelve el id generado.
    /// </summary>
    long Insert(Album album);

    /// <summary>
    /// Actualiza nombre y año del álbum con ese id; todas sus rolas lo
    /// siguen porque apuntan al id.
    /// </summary>
    /// <param name="album">
    /// Álbum con el id a actualizar y los valores nuevos.
    /// </param>
    /// <returns>
    /// true si se actualizó; false si no existe un álbum con ese id.
    /// </returns>
    bool Update(Album album);
}

/// <summary>
/// DAO para Performer.
/// </summary>
public interface IPerformerDao
{
    /// <summary>
    /// Busca por nombre literal. Null si no existe.
    /// </summary>
    Performer? FindByName(string name);

    /// <summary>
    /// Inserta y devuelve el id generado.
    /// </summary>
    long Insert(Performer performer);
}

/// <summary>
/// DAO para Rola.
/// </summary>
public interface IRolaDao
{
    /// <summary>
    /// Cantidad de rolas que devuelve FindFirst si no se indica otra.
    /// </summary>
    const int DefaultFindFirstCount = int.MaxValue;

    /// <summary>
    /// Consulta a la BD si ya existe una rola con esa ruta.
    /// </summary>
    bool ExistsByPath(string path);

    /// <summary>
    /// Inserta y devuelve el id generado.
    /// </summary>
    long Insert(Rola rola);

    /// <summary>
    /// Actualiza título, pista, año, género e intérprete de la rola con
    /// ese id.
    /// </summary>
    /// <param name="rola">
    /// Rola con el id a actualizar y los valores nuevos.
    /// </param>
    /// <returns>
    /// true si se actualizó; false si no existe una rola con ese id.
    /// </returns>
    bool Update(Rola rola);

    /// <summary>
    /// Cantidad máxima de rolas que devuelve Search si no se indica otra.
    /// </summary>
    const int DefaultSearchCount = int.MaxValue;

    /// <summary>
    /// Devuelve las primeras rolas de la BD (equivale a una búsqueda sin
    /// criterios), con su intérprete y su álbum.
    /// </summary>
    /// <param name="count">
    /// Cuántas rolas devolver como máximo. Debe ser positivo.
    /// </param>
    /// <returns>
    /// A lo más count rolas; vacía si la BD no tiene ninguna.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Si count no es positivo.
    /// </exception>
    IReadOnlyList<Rola> FindFirst(int count = DefaultFindFirstCount);

    /// <summary>
    /// Busca rolas que cumplan TODOS los criterios dados, con su intérprete
    /// y su álbum. Ordenadas por intérprete, álbum, pista y título.
    /// </summary>
    /// <param name="criteria">
    /// Qué buscar; los campos null o vacíos no filtran.
    /// </param>
    /// <param name="count">
    /// Cuántas rolas devolver como máximo. Debe ser positivo.
    /// </param>
    /// <returns>
    /// A lo más count rolas.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Si count no es positivo.
    /// </exception>
    IReadOnlyList<Rola> Search
    (
        RolaSearch criteria,
        int count = DefaultSearchCount
    );
}
