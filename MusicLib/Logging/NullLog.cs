namespace MusicLib.Logging;

/// <summary>
/// Log que descarta todo. Es el que se usa cuando NO se pide -l y el valor por
/// defecto cuando a una clase no se le inyecta ningún log.
/// Se hace uso del patrón de diseño NullObject.
/// </summary>
public sealed class NullLog : ILog
{
    /// <summary>
    /// Única instancia.
    /// </summary>
    public static readonly NullLog Instance = new();

    private NullLog() { }

    /// <summary>
    /// No hace nada.
    /// </summary>
    public void Write(string message) { }
}