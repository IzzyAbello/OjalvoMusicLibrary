using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLib.Models;
using MusicLib.UI.Services;

namespace MusicLib.UI.ViewModels;

/// <summary>
/// ViewModel del reproductor: un botón Reproducir/Pausa para la rola
/// seleccionada. Lo que suena sigue sonando aunque cambie la selección.
/// </summary>
public sealed partial class PlayerViewModel : ObservableObject
{
    private const string PlayText = "Reproducir";
    private const string PauseText = "Pausa";

    private readonly IAudioPlayer _player;

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
            IsPlaying = true;
        }

        NowPlaying = (IsPlaying ? "Sonando: " : "En pausa: ") + Describe(rola);
    }

    private bool CanPlayPause() => IsAvailable && Selected is not null;

    private bool IsCurrent(Rola? rola) =>
        rola is not null && _current is not null && rola.Id == _current.Id;

    private void OnEnded()
    {
        _current = null;
        IsPlaying = false;
        NowPlaying = "";
    }

    private static string Describe(Rola rola) =>
        string.IsNullOrEmpty(rola.Performer?.Name)
            ? rola.Title
            : $"{rola.Title} — {rola.Performer.Name}";
}
