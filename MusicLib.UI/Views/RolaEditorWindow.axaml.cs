using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MusicLib.UI.Views;

/// <summary>
/// Ventana dedicada a editar los metadatos de una rola.
/// </summary>
public partial class RolaEditorWindow : Window
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public RolaEditorWindow() => InitializeComponent();

    // Por ahora los dos botones solo cierran la ventana.
    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
