using System.Globalization;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MusicLib.Tests.Support;

/// <summary>
/// Consultas directas a la BD (distinta a la de la app) para
/// verificar qué quedó realmente guardado.
/// </summary>
internal static class Db
{
    private static SqliteConnection Open(string dbPath, bool foreignKeys)
    {
        var conn = new SqliteConnection
        (
            $"Data Source={dbPath};Foreign Keys={foreignKeys}"
        );
        conn.Open();
        return conn;
    }

    private static SqliteCommand Command
    (
        SqliteConnection conn,
        string sql,
        object[] args
    )
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        for (int i = 0; i < args.Length; i++)
            cmd.Parameters.AddWithValue($"$p{i}", args[i]);
        return cmd;
    }

    /// <summary>
    /// Ejecuta SQL sin resultado.
    /// </summary>
    public static void Exec
    (
        string dbPath,
        string sql,
        bool foreignKeys = false
    )
    {
        using var conn = Open(dbPath, foreignKeys);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Todas las filas.
    /// </summary>
    public static List<object?[]> Rows
    (
        string dbPath,
        string sql,
        params object[] args
    )
    {
        using var conn = Open(dbPath, false);
        using var cmd = Command(conn, sql, args);
        using var r = cmd.ExecuteReader();

        var rows = new List<object?[]>();
        while (r.Read())
        {
            var row = new object?[r.FieldCount];
            for (int i = 0; i < row.Length; i++)
                row[i] = r.IsDBNull(i) ? null : r.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }

    /// <summary>
    /// Exactamente una fila.
    /// </summary>
    public static object?[] Row(string dbPath, string sql, params object[] args) =>
        Assert.Single(Rows(dbPath, sql, args));

    /// <summary>
    /// Primera columna de la primera fila, convertida a T (int o long),
    /// para obtener tamaños y otros.
    /// </summary>
    public static T Scalar<T>
    (
        string dbPath,
        string sql,
        params object[] args
    )
    {
        using var conn = Open(dbPath, false);
        using var cmd = Command(conn, sql, args);
        var value = cmd.ExecuteScalar();
        return (T)Convert.ChangeType
        (
            value!,
            typeof(T),
            CultureInfo.InvariantCulture
        );
    }

    /// <summary>
    /// SELECT COUNT(*) de una tabla.
    /// </summary>
    public static int Count(string dbPath, string table) =>
        Scalar<int>(dbPath, $"SELECT COUNT(*) FROM {table}");

    /// <summary>
    /// Primera columna de cada fila, como texto.
    /// </summary>
    public static List<string> Strings
    (
        string dbPath,
        string sql,
        params object[] args
    ) => Rows(dbPath, sql, args).Select(r => (string)r[0]!).ToList();

    /// <summary>
    /// Tablas del usuario ordenadas.
    /// </summary>
    public static List<string> Tables(string dbPath) => Strings
    (
        dbPath,
        @"SELECT name FROM sqlite_master
          WHERE type = 'table' AND name NOT LIKE 'sqlite\_%' ESCAPE '\'
          ORDER BY name"
    );

    /// <summary>
    /// Índices creados por la app, ordenados.
    /// </summary>
    public static List<string> Indexes(string dbPath) => Strings
    (
        dbPath,
        @"SELECT name FROM sqlite_master
          WHERE type = 'index' AND name LIKE 'idx\_%' ESCAPE '\'
          ORDER BY name"
    );
}
