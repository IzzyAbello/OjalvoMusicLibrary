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
    const int DefaultFindFirstCount = 10;

    /// <summary>
    /// Consulta a la BD si ya existe una rola con esa ruta.
    /// </summary>
    bool ExistsByPath(string path);

    /// <summary>
    /// Inserta y devuelve el id generado.
    /// </summary>
    long Insert(Rola rola);

    /// <summary>
    /// Devuelve las primeras rolas de la BD, ordenadas por id.
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
}