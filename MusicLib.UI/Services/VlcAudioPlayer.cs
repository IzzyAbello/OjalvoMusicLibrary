using Avalonia.Threading;
using LibVLCSharp.Shared;
using MusicLib.Logging;

namespace MusicLib.UI.Services;

/// <summary>
/// Reproductor sobre la libVLC instalada en el sistema (LibVLCSharp). Si
/// libVLC no está, queda no disponible en vez de lanzar.
/// </summary>
public sealed class VlcAudioPlayer : IAudioPlayer
{
    private readonly LibVLC? _vlc;
    private readonly MediaPlayer? _player;
    private Media? _media;

    /// <inheritdoc />
    public bool IsAvailable => _player is not null;

    /// <inheritdoc />
    public bool IsPlaying => _player?.IsPlaying ?? false;

    /// <inheritdoc />
    public long Length => Math.Max(_player?.Length ?? 0, 0);

    /// <inheritdoc />
    public long Time
    {
        get => Math.Max(_player?.Time ?? 0, 0);
        set { if (_player is not null) _player.Time = value; }
    }

    /// <inheritdoc />
    public event Action? Ended;

    /// <summary>
    /// Inicia libVLC. Si falla, lo escribe en el log y
    /// el reproductor queda no disponible.
    /// </summary>
    /// <param name="log">Log del programa.</param>
    public VlcAudioPlayer(ILog log)
    {
        try
        {
            Core.Initialize();
            _vlc = new LibVLC("--no-video");
            _player = new MediaPlayer(_vlc);

            // VLC avisa desde su propio hilo, y dentro de ese aviso no se
            // debe llamar al reproductor: se pasa al hilo de la UI.
            _player.EndReached += (_, _) =>
                Dispatcher.UIThread.Post(() => Ended?.Invoke());
        }
        catch (Exception ex)
        {
            log.Write($"No se pudo iniciar VLC: {ex}");
            _player?.Dispose();
            _vlc?.Dispose();
            _player = null;
            _vlc = null;
        }
    }

    /// <inheritdoc />
    public void Play(string path)
    {
        if (_vlc is null || _player is null) return;

        var old = _media;
        _media = new Media(_vlc, path, FromType.FromPath);
        _player.Play(_media);
        old?.Dispose();
    }

    /// <inheritdoc />
    public void Pause() => _player?.SetPause(true);

    /// <inheritdoc />
    public void Resume() => _player?.SetPause(false);

    /// <summary>
    /// Detiene y libera libVLC.
    /// </summary>
    public void Dispose()
    {
        _player?.Stop();
        _player?.Dispose();
        _media?.Dispose();
        _vlc?.Dispose();
    }
}
