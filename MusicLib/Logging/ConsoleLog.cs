namespace MusicLib.Logging;

/// <summary>
/// Escribe cada mensaje como una línea en la consola.
/// Es el que se usa con -l.
/// </summary>
public sealed class ConsoleLog : ILog
{
    private readonly TextWriter _out;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="output">
    /// Dónde se escribe.
    /// </param>
    public ConsoleLog(TextWriter output) => _out = output;

    /// <summary>
    /// Escribe el mensaje.
    /// </summary>
    public void Write(string message)
    {
        try
        {
            _out.WriteLine("[Log] " + message);
        }
        catch (IOException)
        {
            // Consola cerrada (p. ej. pipe roto): el log no debe tumbar nada.
        }
    }
}