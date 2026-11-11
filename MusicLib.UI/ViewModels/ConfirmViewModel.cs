using CommunityToolkit.Mvvm.Input;

namespace MusicLib.UI.ViewModels;

/// <summary>
/// ViewModel del diálogo genérico de confirmación.
/// </summary>
public sealed partial class ConfirmViewModel
{
    /// <summary>
    /// Título de la ventana.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Pregunta que se le hace al usuario.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Pide a la ventana que se cierre; true = el usuario aceptó.
    /// </summary>
    public event Action<bool>? CloseRequested;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="title">Título de la ventana.</param>
    /// <param name="message">Pregunta que se le hace al usuario.</param>
    public ConfirmViewModel(string title, string message)
    {
        Title = title;
        Message = message;
    }

    // Botón Aceptar.
    [RelayCommand]
    private void Accept() => CloseRequested?.Invoke(true);

    // Botón Cancelar (o Esc).
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}
