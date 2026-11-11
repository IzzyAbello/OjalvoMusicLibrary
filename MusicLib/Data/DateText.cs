using System.Globalization;

namespace MusicLib.Data;

/// <summary>
/// Reglas de las fechas opcionales de personas y grupos.
/// </summary>
public static class DateText
{
    /// <summary>
    /// Formato de las fechas guardadas.
    /// </summary>
    public const string Format = "yyyy-MM-dd";

    /// <summary>
    /// Dice si el texto es una fecha válida u opcional vacía.
    /// </summary>
    /// <param name="text">
    /// Texto a revisar; se ignoran los espacios.
    /// </param>
    /// <returns>
    /// true si está vacío o es una fecha real en formato AAAA-MM-DD.
    /// </returns>
    public static bool IsValid(string? text) =>
        Normalize(text) is not { } clean || TryParse(clean, out _);

    /// <summary>
    /// Dice si el fin no es antes del inicio.
    /// </summary>
    /// <param name="start">Fecha de inicio.</param>
    /// <param name="end">Fecha de fin.</param>
    /// <returns>
    /// false solo si ambas son fechas válidas y el fin es antes del
    /// inicio. Si falta alguna o no es válida, regresa true.
    /// </returns>
    public static bool IsInOrder(string? start, string? end) =>
        !(TryParse(Normalize(start), out var from)
          && TryParse(Normalize(end), out var to)
          && to < from);

    /// <summary>
    /// Limpia el texto para guardarlo.
    /// </summary>
    /// <param name="text">
    /// Texto escrito por el usuario.
    /// </param>
    /// <returns>
    /// El texto sin espacios alrededor, o null si queda vacío.
    /// </returns>
    public static string? Normalize(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static bool TryParse(string? text, out DateOnly date) =>
        DateOnly.TryParseExact
        (
            text,
            Format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date
        );
}
