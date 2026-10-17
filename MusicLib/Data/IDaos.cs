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
    /// Todos los paths ya minados (para saltar archivos existentes).
    /// </summary>
    HashSet<string> GetAllPaths();

    /// <summary>
    /// Inserta y devuelve el id generado.
    /// </summary>
    long Insert(Rola rola);
}