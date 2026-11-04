using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MusicLib.UI.Views;

/// <summary>
/// Formulario para dar de alta a una persona (artista individual).
/// </summary>
public partial class PersonWindow : Window
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public PersonWindow() => InitializeComponent();

    // Por ahora los dos botones solo cierran la ventana.
    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
