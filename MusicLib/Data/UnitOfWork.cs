using Microsoft.Data.Sqlite;

namespace MusicLib.Data;

/// <summary>
/// Dueño de la conexión y de la transacción actual. Los DAOs reciben el
/// UnitOfWork (no la transacción), así pueden seguir funcionando cuando el
/// minero hace commit por lotes y abre una transacción nueva.
/// </summary>
public sealed class UnitOfWork : IDisposable
{
    private readonly SqliteConnection _conn;
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
    /// Acceso a las Rolas.
    /// </summary>
    public IRolaDao Rolas { get; }

    /// <summary>
    /// Abre una sola conexión, con las llaves foráneas activadas.
    /// Luego crea los tres DAOs
    /// </summary>
    /// <param name="dbPath"> Ruta de la DB </param>
    public UnitOfWork(string dbPath)
    {
        _conn = new SqliteConnection($"Data Source={dbPath};Foreign Keys=True");
        _conn.Open();

        Albums = new AlbumDao(this);
        Performers = new PerformerDao(this);
        Rolas = new RolaDao(this);
    }

    /// <summary>
    /// Verificar si la transacción sigue activa.
    /// </summary>
    public bool InTransaction => _tx != null;

    /// <summary>
    /// Inicia una transacción
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Si se trata de iniciar una transacción mientras ya hay una en curso. 
    /// </exception>
    public void Begin()
    {
        if (InTransaction)
            throw new InvalidOperationException
            (
                "Ya hay una transacción abierta."
            );
        _tx = _conn.BeginTransaction();
    }

    /// <summary>
    /// Confirma una transacción en la BD.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Si no hay una transacción en curso.
    /// </exception>
    public void Commit()
    {
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
        var cmd = _conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = _tx;
        return cmd;
    }

    private void Exec(string sql)
    {
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
        if (_tx != null)
        {
            try { _tx.Rollback(); } catch { /* ya cerrada */ }
            _tx.Dispose();
            _tx = null;
        }
        _conn.Dispose();
    }
}