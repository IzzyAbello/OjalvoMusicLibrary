using Avalonia.Controls;
using MusicLib.UI.ViewModels;

namespace MusicLib.UI.Views;

/// <summary>
/// Formulario de búsqueda avanzada por campos.
/// </summary>
public partial class AdvancedSearchWindow : Window
{
    /// <summary>
    /// Constructor para el previsualizador de XAML.
    /// </summary>
    public AdvancedSearchWindow() => InitializeComponent();

    /// <summary>
    /// Constructor de producción.
    /// </summary>
    /// <param name="vm">
    /// ViewModel de la ventana; cuando pide cerrar, la ventana se cierra
    /// devolviendo su resultado.
    /// </param>
    public AdvancedSearchWindow(AdvancedSearchViewModel vm) : this()
    {
        DataContext = vm;
        // Suscripcion a evento.
        vm.CloseRequested += Close;
    }
}
