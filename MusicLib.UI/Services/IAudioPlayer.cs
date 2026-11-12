namespace MusicLib.UI.Services;

/// <summary>
/// Reproduce un archivo de audio a la vez.
/// </summary>
public interface IAudioPlayer : IDisposable
{
    /// <summary>
    /// false si no se pudo iniciar el motor de audio.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// true mientras suena.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Duración del archivo cargado, en milisegundos.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// Posición actual, en milisegundos. Asignarla salta a esa posición.
    /// </summary>
    long Time { get; set; }

    /// <summary>
    /// Se lanza cuando el archivo termina de sonar.
    /// </summary>
    event Action? Ended;

    /// <summary>
    /// Carga el archivo y lo reproduce desde el inicio; detiene lo que
    /// sonara antes.
    /// </summary>
    /// <param name="path">Ruta del archivo.</param>
    void Play(string path);

    /// <summary>
    /// Pausa lo que suena.
    /// </summary>
    void Pause();

    /// <summary>
    /// Reanuda lo que estaba en pausa.
    /// </summary>
    void Resume();
}
