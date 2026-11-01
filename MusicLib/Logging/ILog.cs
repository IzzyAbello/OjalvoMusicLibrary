namespace MusicLib.Logging;

/// <summary>
/// Interfaz de bitácora del programa.
/// </summary>
public interface ILog
{
    /// <summary>
    /// Escribe un mensaje de seguimiento.
    /// </summary>
    /// <param name="message">
    /// Mensaje, ya formateado.
    /// </param>
    void Write(string message);
}