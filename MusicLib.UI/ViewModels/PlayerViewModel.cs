using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLib.Models;
using MusicLib.UI.Services;

namespace MusicLib.UI.ViewModels;

/// <summary>
/// ViewModel del reproductor: un botón Reproducir/Pausa para la rola
/// seleccionada y una barra de progreso que permite saltar. Lo que suena
/// sigue sonando aunque cambie la selección.
/// </summary>
public sealed partial class PlayerViewModel : ObservableObject
{
    private const string PlayText = "Reproducir";
    private const string PauseText = "Pausa";

    private readonly IAudioPlayer _player;

    // Pregunta la posición al reproductor mientras suena.
    private readonly DispatcherTimer _timer = new()
    {
        Interval = TimeSpan.FromMilliseconds(250)
    };

    // true mientras el usuario arrastra o pulsa la barra: el timer no la
    // mueve para que no brinque bajo el cursor.
    private bool _seeking;

    // Rola cargada en el reproductor; null si no hay.
    private Rola? _current;

    /// <summary>
    /// Rola seleccionada en la tabla.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ButtonText))]
    [NotifyCanExecuteChangedFor(nameof(PlayPauseCommand))]
    public partial Rola? Selected { get; set; }

    /// <summary>
    /// true mientras suena la rola cargada.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ButtonText))]
    public partial bool IsPlaying { get; set; }

    /// <summary>
    /// Qué está cargado ("Sonando: …" o "En pausa: …"); vacío si nada.
    /// </summary>
    [ObservableProperty]
    public partial string NowPlaying { get; set; } = "";

    /// <summary>
    /// Posición de la barra, en milisegundos.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeText))]
    public partial double Position { get; set; }

    /// <summary>
    /// Duración de la rola cargada, en milisegundos (0 si no se sabe).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeText))]
    public partial double Duration { get; set; }

    /// <summary>
    /// Tiempo transcurrido y total, "m:ss / m:ss".
    /// </summary>
    public string TimeText => $"{Format(Position)} / {Format(Duration)}";

    /// <summary>
    /// true si hay una rola cargada (la barra se puede mover).
    /// </summary>
    public bool CanSeek => _current is not null;

    /// <summary>
    /// "Pausa" si la seleccionada es la que suena; si no, "Reproducir".
    /// </summary>
    public string ButtonText => IsPlaying && IsCurrent(Selected)
        ? PauseText
        : PlayText;

    /// <summary>
    /// false si no hay motor de audio (VLC no instalado).
    /// </summary>
    public bool IsAvailable => _player.IsAvailable;

    /// <summary>
    /// Mensajes para la barra de estado (errores al reproducir).
    /// </summary>
    public event Action<string>? StatusMessage;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="player">Motor de audio.</param>
    public PlayerViewModel(IAudioPlayer player)
    {
        _player = player;
        _player.Ended += OnEnded;
        _timer.Tick += (_, _) => Refresh();
    }

    /// <summary>
    /// El usuario empezó a pulsar o arrastrar la barra.
    /// </summary>
    public void BeginSeek() => _seeking = true;

    /// <summary>
    /// El usuario soltó la barra: salta a la posición donde quedó.
    /// </summary>
    public void EndSeek()
    {
        if (!_seeking) return;
        _seeking = false;
        if (CanSeek) _player.Time = (long)Position;
    }

    // Botón Reproducir/Pausa. Si la seleccionada es la cargada, alterna
    // pausa y reanudar; si es otra, la reproduce desde el inicio.
    [RelayCommand(CanExecute = nameof(CanPlayPause))]
    private void PlayPause()
    {
        if (Selected is not { } rola) return;

        if (IsCurrent(rola))
        {
            if (IsPlaying) _player.Pause();
            else _player.Resume();
            IsPlaying = !IsPlaying;
        }
        else
        {
            if (!File.Exists(rola.Path))
            {
                StatusMessage?.Invoke
                (
                    $"No se encontró el archivo '{rola.Path}'."
                );
                return;
            }

            _player.Play(rola.Path);
            _current = rola;
            OnPropertyChanged(nameof(CanSeek));
            Position = 0;
            Duration = 0;
            IsPlaying = true;
        }

        NowPlaying = (IsPlaying ? "Sonando: " : "En pausa: ") + Describe(rola);
    }

    private bool CanPlayPause() => IsAvailable && Selected is not null;

    private bool IsCurrent(Rola? rola) =>
        rola is not null && _current is not null && rola.Id == _current.Id;

    // El timer solo corre mientras suena; en pausa la barra se queda.
    // Toolkit
    partial void OnIsPlayingChanged(bool value)
    {
        if (value) _timer.Start();
        else _timer.Stop();
    }

    // Copia posición y duración del reproductor, salvo mientras el usuario
    // mueve la barra.
    private void Refresh()
    {
        if (_seeking) return;
        Duration = _player.Length;
        Position = _player.Time;
    }

    private void OnEnded()
    {
        _current = null;
        OnPropertyChanged(nameof(CanSeek));
        _seeking = false;
        IsPlaying = false;
        NowPlaying = "";
        Position = 0;
        Duration = 0;
    }

    private static string Format(double ms)
    {
        var time = TimeSpan.FromMilliseconds(ms);
        return $"{(int)time.TotalMinutes}:{time.Seconds:00}";
    }

    private static string Describe(Rola rola) =>
        string.IsNullOrEmpty(rola.Performer?.Name)
            ? rola.Title
            : $"{rola.Title} — {rola.Performer.Name}";
}
