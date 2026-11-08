namespace MusicLib.Data;

/// <summary>
/// Criterios de búsqueda de rolas. Un campo null o vacío no filtra; los
/// que sí traen valor se combinan con AND. Los textos buscan "contiene",
/// sin distinguir mayúsculas ni acentos.
/// </summary>
/// <param name="Any">
/// Texto que debe aparecer en el título, el intérprete, el álbum o el
/// género (búsqueda simple).
/// </param>
/// <param name="Title">Texto contenido en el título.</param>
/// <param name="Performer">Texto contenido en el intérprete.</param>
/// <param name="Album">Texto contenido en el álbum.</param>
/// <param name="Genre">Texto contenido en el género.</param>
/// <param name="YearFrom">Año mínimo.</param>
/// <param name="YearTo">Año máximo.</param>
public sealed record RolaSearch
(
    string? Any = null,
    string? Title = null,
    string? Performer = null,
    string? Album = null,
    string? Genre = null,
    int? YearFrom = null,
    int? YearTo = null
);
