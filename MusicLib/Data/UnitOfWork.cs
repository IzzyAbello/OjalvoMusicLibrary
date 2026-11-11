using Microsoft.Data.Sqlite;
using MusicLib.Logging;

namespace MusicLib.Data;

/// <summary>
/// Dueño de la conexión y de la transacción actual. Los DAOs reciben el
/// UnitOfWork (no la transacción), así pueden seguir funcionando cuando el
/// minero hace commit y abre una transacción nueva.
/// </summary>
public sealed class UnitOfWork : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly ILog _log;
    private SqliteTransaction? _tx;

    /// <summary>
    /// Acceso a los Álbumes.
    /// </summary>
    public IAlbumDao Albums { get; }
    
    /// <summary>
    /// Acceso a los Performers.
    /// </summary>
    public IPerformerDao Performers { get; }
    
    /// <summary>
    /// Acceso a las Personas.
    /// </summary>
    public IPersonDao Persons { get; }

    /// <summary>
    /// Acceso a los Grupos.
    /// </summary>
    public IGroupDao Groups { get; }

    /// <summary>
    /// Acceso a las membresías (personas en grupos).
    /// </summary>
    public IMembershipDao Memberships { get; }

    /// <summary>
    /// Acceso a las Rolas.
    /// </summary>
    public IRolaDao Rolas { get; }

    /// <summary>
    /// Abre una sola conexión, con las llaves foráneas activadas.
    /// Luego crea los DAOs
    /// </summary>
    /// <param name="dbPath"> Ruta de la DB </param>
    /// <param name="log">
    /// Destino de los mensajes. Si es null, los mensajes se descartan. Se
    /// comparte con los DAOs.
    /// </param>
    public UnitOfWork(string dbPath, ILog? log = null)
    {
        _log = log ?? NullLog.Instance;

        _log.Write($"Estableciendo conexión con la DB en {dbPath}");
        _conn = new SqliteConnection($"Data Source={dbPath};Foreign Keys=True");
        _conn.Open();
        TextNormalizer.Register(_conn);

        Albums = new AlbumDao(this, _log);
        Performers = new PerformerDao(this, _log);
        Persons = new PersonDao(this, _log);
        Groups = new GroupDao(this, _log);
        Memberships = new MembershipDao(this, _log);
        Rolas = new RolaDao(this, _log);

        _log.Write($"Unit of Work creada correctamente.");
    }

    /// <summary>
    /// Verificar si la transacción sigue activa.
    /// </summary>
    public bool InTransaction => _tx != null;

    /// <summary>
    /// Inicia una transacción tomando el candado de escritura desde el
    /// principio (BEGIN IMMEDIATE): si otro proceso está escribiendo, falla
    /// aquí, una sola vez, y no a la mitad del trabajo.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Si se trata de iniciar una transacción mientras ya hay una en curso. 
    /// </exception>
    public void Begin()
    {
        _log.Write($"Comenzando una transacción.");
        if (InTransaction)
            throw new InvalidOperationException
            (
                "Ya hay una transacción abierta."
            );
        _tx = _conn.BeginTransaction(deferred: false);
    }

    /// <summary>
    /// Confirma una transacción en la BD.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Si no hay una transacción en curso.
    /// </exception>
    public void Commit()
    {
        _log.Write($"Confirmando una transacción.");
        var tx = _tx ?? throw new InvalidOperationException
            (
                "No hay transacción abierta."
            );
        _tx = null;
        try { tx.Commit(); } finally { tx.Dispose(); }
    }

    /// <summary>
    /// Deshace cambios de una transacción.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Si no hay una transacción en curso.
    /// </exception>
    public void Rollback()
    {
        _log.Write($"Deshaciendo cambios.");
        var tx = _tx ?? throw new InvalidOperationException
            (
                "No hay transacción abierta."
            );
        _tx = null;
        try { tx.Rollback(); } finally { tx.Dispose(); }
    }

    /// <summary>
    /// Permite deshacer solo un archivo dentro de un lote.
    /// </summary>
    /// <param name="name">
    /// Punto de recuperación.
    /// </param>
    public void Savepoint(string name)
    {
        _log.Write($"SAVEPOINT.");
        CheckName(name);
        Exec($"SAVEPOINT {name}");
    }

    /// <summary>
    /// Libera el punto de recuperación especificado.
    /// </summary>
    /// <param name="name">
    /// Punto de recuperación.
    /// </param>
    public void ReleaseSavepoint(string name)
    {
        _log.Write($"REALEASE.");
        CheckName(name);
        Exec($"RELEASE {name}");
    }

    /// <summary>
    /// Deshace las operaciones realizadas después del punto de recuperación.
    /// </summary>
    /// <param name="name">
    /// Punto de recuperación.
    /// </param>
    public void RollbackToSavepoint(string name)
    {
        _log.Write($"ROLLBACK");
        CheckName(name);
        Exec($"ROLLBACK TO {name}");
        Exec($"RELEASE {name}");
    }

    /// <summary>
    /// Crea un comando asociado a la transacción en curso.
    /// </summary>
    /// <param name="sql">
    /// Cadena en SQL.
    /// </param>
    /// <returns>
    /// Comando de SQLite correspondiente.
    /// </returns>
    internal SqliteCommand CreateCommand(string sql)
    {
        _log.Write($"Creando comando.");
        var cmd = _conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = _tx;
        return cmd;
    }

    private void Exec(string sql)
    {
        _log.Write($"Ejecutando consulta a la DB");
        using var cmd = CreateCommand(sql);
        cmd.ExecuteNonQuery();
    }

    private static void CheckName(string name)
    {
        if
            (
                string.IsNullOrEmpty(name) ||
                !name.All(c => char.IsLetterOrDigit(c) || c == '_')
            )
            throw new ArgumentException("Nombre de savepoint inválido.", nameof(name));
    }

    /// <summary>
    /// Limpia la transacción con SQLite.
    /// </summary>
    public void Dispose()
    {
        _log.Write($"Limpiando transacción.");
        if (_tx != null)
        {
            try { _tx.Rollback(); } catch { /* ya cerrada */ }
            _tx.Dispose();
            _tx = null;
        }
        _conn.Dispose();
    }
}