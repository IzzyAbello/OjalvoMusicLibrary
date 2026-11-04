using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MusicLib.UI.Views;

/// <summary>
/// Formulario para crear un grupo musical.
/// </summary>
public partial class GroupWindow : Window
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public GroupWindow() => InitializeComponent();

    // Por ahora los dos botones solo cierran la ventana.
    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
