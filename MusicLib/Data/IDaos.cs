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
    /// Busca por id. Null si no existe.
    /// </summary>
    Album? FindById(long id);

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
/// DAO para Person.
/// </summary>
public interface IPersonDao
{
    /// <summary>
    /// Todas las personas, ordenadas por nombre artístico.
    /// </summary>
    IReadOnlyList<Person> FindAll();

    /// <summary>
    /// Busca por id. Null si no existe.
    /// </summary>
    Person? FindById(long id);

    /// <summary>
    /// Busca por nombre artístico exacto. Null si no existe.
    /// </summary>
    Person? FindByStageName(string stageName);

    /// <summary>
    /// Inserta y devuelve el id generado.
    /// </summary>
    long Insert(Person person);

    /// <summary>
    /// Actualiza nombre real y fechas de la persona con ese id. El nombre
    /// artístico no cambia (es su vínculo con su intérprete).
    /// </summary>
    /// <returns>
    /// true si se actualizó; false si no existe una persona con ese id.
    /// </returns>
    bool Update(Person person);

    /// <summary>
    /// Borra la persona con ese id. Antes hay que quitarla de sus grupos
    /// (in_group), o fallan las llaves foráneas (integridad).
    /// </summary>
    /// <returns>
    /// true si se borró; false si no existe una persona con ese id.
    /// </returns>
    bool Delete(long id);
}

/// <summary>
/// DAO para Group.
/// </summary>
public interface IGroupDao
{
    /// <summary>
    /// Todos los grupos, ordenados por nombre.
    /// </summary>
    IReadOnlyList<Group> FindAll();

    /// <summary>
    /// Busca por id. Null si no existe.
    /// </summary>
    Group? FindById(long id);

    /// <summary>
    /// Busca por nombre exacto. Null si no existe.
    /// </summary>
    Group? FindByName(string name);

    /// <summary>
    /// Inserta y devuelve el id generado.
    /// </summary>
    long Insert(Group group);

    /// <summary>
    /// Actualiza las fechas del grupo con ese id. El nombre no cambia (es
    /// su vínculo con su intérprete).
    /// </summary>
    /// <returns>
    /// true si se actualizó; false si no existe un grupo con ese id.
    /// </returns>
    bool Update(Group group);

    /// <summary>
    /// Borra el grupo con ese id. Antes hay que quitarle sus miembros
    /// (in_group), o fallan las llaves foráneas.
    /// </summary>
    /// <returns>
    /// true si se borró; false si no existe un grupo con ese id.
    /// </returns>
    bool Delete(long id);
}

/// <summary>
/// DAO para las membresías (tabla in_group).
/// </summary>
public interface IMembershipDao
{
    /// <summary>
    /// Las personas que son miembros del grupo, ordenadas por nombre
    /// artístico. Vacío si no tiene miembros o el grupo no existe.
    /// </summary>
    IReadOnlyList<Person> Members(long idGroup);

    /// <summary>
    /// Agrega la persona al grupo. Falla si ya es miembro o si la persona
    /// o el grupo no existe.
    /// </summary>
    void Add(long idPerson, long idGroup);

    /// <summary>
    /// Quita a todos los miembros del grupo.
    /// </summary>
    /// <returns>Cuántas membresías se quitaron.</returns>
    int RemoveAllOfGroup(long idGroup);

    /// <summary>
    /// Saca a la persona de todos sus grupos.
    /// </summary>
    /// <returns>Cuántas membresías se quitaron.</returns>
    int RemoveAllOfPerson(long idPerson);
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
    /// Busca una rola por su id, con su intérprete y su álbum.
    /// </summary>
    /// <param name="id">Id de la rola.</param>
    /// <returns>La rola, o null si no existe.</returns>
    Rola? FindById(long id);

    /// <summary>
    /// Consulta a la BD si ya existe una rola con esa ruta.
    /// </summary>
    bool ExistsByPath(string path);

    /// <summary>
    /// Inserta y devuelve el id generado.
    /// </summary>
    long Insert(Rola rola);

    /// <summary>
    /// Actualiza título, pista, año, género, intérprete y álbum de la
    /// rola con ese id (la ruta no cambia).
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
