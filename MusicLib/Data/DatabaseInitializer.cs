using System.Reflection;
using Microsoft.Data.Sqlite;

namespace MusicLib.Data;

/// <summary>
/// Prepara la BD: si está vacía, crea el esquema ejecutando el rolas.sql
/// embebido; además asegura los índices.
/// </summary>
public static class DatabaseInitializer
{
    private const string ScriptResource = "rolas.sql";

    private static readonly string[] ExpectedTables =
    {
        "types",
        "performers",
        "persons",
        "groups",
        "in_group",
        "albums",
        "rolas"
    };

    private static readonly string[] IndexStatements =
    {
        "CREATE INDEX IF NOT EXISTS idx_rolas_path ON rolas(path)",
        "CREATE INDEX IF NOT EXISTS idx_performers_name ON performers(name)",
        "CREATE INDEX IF NOT EXISTS idx_albums_name_path ON albums(name, path)"
    };

    /// <summary>
    /// Deja la BD lista para usarse. Todo ocurre en una sola transacción.
    /// </summary>
    /// <param name="dbPath">
    /// Ruta del archivo de la BD. Si no existe, se crea.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Si la BD ya tiene tablas pero le faltan las del esquema esperado, o si
    /// el recurso embebido no se encuentra. No se modifica nada en ese caso.
    /// </exception>
    public static void EnsureCreated(string dbPath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(dbPath));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        using var conn = new SqliteConnection($"Data Source={dbPath};Foreign Keys=True");
        conn.Open();

        using var tx = conn.BeginTransaction(deferred: false);

        var existing = ListTables(conn, tx);

        if (existing.Count == 0) Execute(conn, tx, ReadScript());
        else
        {
            var missing = ExpectedTables.Where(t => !existing.Contains(t)).ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException
                (
                    $"La BD '{dbPath}' ya tiene tablas, pero no el esquema esperado " +
                    $"(faltan: {string.Join(", ", missing)})."
                );
        }

        foreach (var sql in IndexStatements) Execute(conn, tx, sql);

        tx.Commit();
    }

    private static HashSet<string> ListTables(SqliteConnection conn, SqliteTransaction tx)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            "SELECT name FROM sqlite_master " +
            "WHERE type = 'table' AND name NOT LIKE 'sqlite\\_%' ESCAPE '\\'";

        using var r = cmd.ExecuteReader();
        while (r.Read()) tables.Add(r.GetString(0));

        return tables;
    }

    private static void Execute(SqliteConnection conn, SqliteTransaction tx, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static string ReadScript()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(ScriptResource)
            ?? throw new InvalidOperationException
            (
                $"No se encontró el recurso embebido '{ScriptResource}'."
            );
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}