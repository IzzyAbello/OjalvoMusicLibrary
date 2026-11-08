using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;

namespace MusicLib.Data;

/// <summary>
/// Normaliza texto para comparar sin mayúsculas ni acentos.
/// El LIKE de SQLite solo ignora mayúsculas en ASCII, así
/// que se registra como función 'norm' en cada conexión.
/// </summary>
internal static class TextNormalizer
{
    /// <summary>
    /// Quita acentos/diacríticos y pasa a minúsculas.
    /// </summary>
    public static string Normalize(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
            if
            (
                CharUnicodeInfo.GetUnicodeCategory(ch) !=
                UnicodeCategory.NonSpacingMark
            )
                sb.Append(ch);

        return sb.ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();
    }

    /// <summary>
    /// Registra norm(texto) en la conexión. NULL se queda NULL.
    /// </summary>
    public static void Register(SqliteConnection conn) =>
        conn.CreateFunction<string?, string?>
        (
            "norm",
            s => s is null ? null : Normalize(s),
            isDeterministic: true
        );
}
