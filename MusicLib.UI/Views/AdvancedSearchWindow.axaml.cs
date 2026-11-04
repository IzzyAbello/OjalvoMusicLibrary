using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MusicLib.UI.Views;

/// <summary>
/// Formulario de búsqueda avanzada por campos.
/// </summary>
public partial class AdvancedSearchWindow : Window
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public AdvancedSearchWindow() => InitializeComponent();

    // Por ahora los dos botones solo cierran la ventana.
    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
