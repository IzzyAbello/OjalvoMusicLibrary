using System.Text;

namespace MusicLib.Data;

/// <summary>
/// Arma la consulta SQL (con parámetros) de una búsqueda de rolas. Nunca
/// concatena texto del usuario: solo agrega condiciones fijas y sus
/// parámetros.
/// </summary>
internal static class RolaSearchSql
{
    // Índices de columnas que lee RolaDao.Read.
    private const string Select =
        "SELECT r.id_rola, r.id_performer, r.id_album, r.path, r.title, " +
        "r.track, r.year, r.genre, " +
        "p.id_type, p.name, " +
        "a.path, a.name, a.year " +
        "FROM rolas r " +
        "LEFT JOIN performers p ON p.id_performer = r.id_performer " +
        "LEFT JOIN albums a ON a.id_album = r.id_album";

    private const string OrderBy =
        " ORDER BY p.name, a.name, r.track, r.title, r.id_rola";

    /// <summary>
    /// Construye la consulta.
    /// </summary>
    /// <param name="c">Criterios.</param>
    /// <param name="count">Máximo de filas.</param>
    /// <returns>
    /// El SQL y sus parámetros (nombre, valor).
    /// </returns>
    public static (string Sql, List<(string Name, object Value)> Parameters)
        Build(RolaSearch c, int count)
    {
        var where = new List<string>();
        var parameters = new List<(string, object)>();

        void AddContains(string? text, string name, params string[] columns)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            parameters.Add((name, ContainsPattern(text)));
            where.Add
            (
                "(" + string.Join
                (
                    " OR ",
                    columns.Select(col => $"norm({col}) LIKE {name} ESCAPE '\\'")
                ) + ")"
            );
        }

        AddContains(c.Any, "$any", "r.title", "p.name", "a.name", "r.genre");
        AddContains(c.Title, "$title", "r.title");
        AddContains(c.Performer, "$performer", "p.name");
        AddContains(c.Album, "$album", "a.name");
        AddContains(c.Genre, "$genre", "r.genre");

        if (c.YearFrom is int from)
        {
            where.Add("r.year >= $yearFrom");
            parameters.Add(("$yearFrom", from));
        }
        if (c.YearTo is int to)
        {
            where.Add("r.year <= $yearTo");
            parameters.Add(("$yearTo", to));
        }

        var sql = new StringBuilder(Select);
        if (where.Count > 0)
            sql.Append(" WHERE ").Append(string.Join(" AND ", where));
        sql.Append(OrderBy).Append(" LIMIT $count");
        parameters.Add(("$count", count));

        return (sql.ToString(), parameters);
    }

    // "%texto%" normalizado, escapando los comodines de LIKE.
    private static string ContainsPattern(string text)
    {
        var escaped = TextNormalizer.Normalize(text.Trim())
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
        return $"%{escaped}%";
    }
}
