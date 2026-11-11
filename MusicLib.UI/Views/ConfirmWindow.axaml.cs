using Avalonia.Controls;
using MusicLib.UI.ViewModels;

namespace MusicLib.UI.Views;

/// <summary>
/// Diálogo genérico de confirmación.
/// </summary>
public partial class ConfirmWindow : Window
{
    /// <summary>
    /// Constructor para el previsualizador de XAML.
    /// </summary>
    public ConfirmWindow() => InitializeComponent();

    /// <summary>
    /// Constructor de producción.
    /// </summary>
    /// <param name="vm">
    /// ViewModel de la ventana; cuando pide cerrar, la ventana se cierra
    /// devolviendo si el usuario aceptó.
    /// </param>
    public ConfirmWindow(ConfirmViewModel vm) : this()
    {
        DataContext = vm;
        vm.CloseRequested += accepted => Close(accepted);
    }
}
