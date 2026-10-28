namespace MusicLib.Cli;

/// <summary>
/// Parámetros de línea de comandos inválidos.
/// </summary>
public sealed class UsageException : Exception
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">
    /// Qué estuvo mal.
    /// </param>
    public UsageException(string message) : base("Error de USO: " + message) { }
}