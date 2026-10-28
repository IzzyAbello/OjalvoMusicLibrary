namespace MusicLib.Cli;

/// <summary>
/// Parámetros de la línea de comandos ya depurados.
/// </summary>
/// <param name="Help">
/// Mostrar la ayuda y salir. Si es true, el resto de campos no importa.
/// </param>
/// <param name="Log">
/// Activar el log del programa.
/// </param>
/// <param name="MinePath">
/// Ruta donde minar, tal como la escribió el usuario, o null si
/// no hay que minar.
/// </param>
/// <param name="OpenView">
/// Si después de (posiblemente) minar se debe abrir la vista.
/// </param>
public sealed record Options
(
    bool Help,
    bool Log,
    string? MinePath,
    bool OpenView
);