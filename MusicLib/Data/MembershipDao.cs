using MusicLib.Logging;
using MusicLib.Models;

namespace MusicLib.Data;

internal sealed class MembershipDao : IMembershipDao
{
    private readonly UnitOfWork _uow;
    private readonly ILog _log;

    public MembershipDao(UnitOfWork uow, ILog log)
    {
        _uow = uow;
        _log = log;
        _log.Write("MembershipDao creado.");
    }

    public IReadOnlyList<Person> Members(long idGroup)
    {
        _log.Write($"Leyendo los miembros del Grupo {idGroup}");
        using var cmd = _uow.CreateCommand
        (
            PersonDao.Select +
            " WHERE id_person IN " +
            "(SELECT id_person FROM in_group WHERE id_group = $group) " +
            "ORDER BY stage_name, id_person"
        );
        cmd.Parameters.AddWithValue("$group", idGroup);

        using var r = cmd.ExecuteReader();
        var members = new List<Person>();
        while (r.Read()) members.Add(PersonDao.Read(r));
        return members;
    }

    public void Add(long idPerson, long idGroup)
    {
        _log.Write($"Agregando la Persona {idPerson} al Grupo {idGroup}");
        using var cmd = _uow.CreateCommand
        (
            "INSERT INTO in_group (id_person, id_group) " +
            "VALUES ($person, $group)"
        );
        cmd.Parameters.AddWithValue("$person", idPerson);
        cmd.Parameters.AddWithValue("$group", idGroup);
        cmd.ExecuteNonQuery();
    }

    public int RemoveAllOfGroup(long idGroup)
    {
        _log.Write($"Quitando a los miembros del Grupo {idGroup}");
        using var cmd = _uow.CreateCommand
        (
            "DELETE FROM in_group WHERE id_group = $group"
        );
        cmd.Parameters.AddWithValue("$group", idGroup);
        return cmd.ExecuteNonQuery();
    }

    public int RemoveAllOfPerson(long idPerson)
    {
        _log.Write($"Sacando a la Persona {idPerson} de sus grupos");
        using var cmd = _uow.CreateCommand
        (
            "DELETE FROM in_group WHERE id_person = $person"
        );
        cmd.Parameters.AddWithValue("$person", idPerson);
        return cmd.ExecuteNonQuery();
    }
}
