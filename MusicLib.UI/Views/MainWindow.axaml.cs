using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MusicLib.UI.Views;

/// <summary>
/// Ventana principal. Solo sabe abrir las ventanas de los botones que las
/// tienen; el resto de botones todavía no hace nada.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public MainWindow() => InitializeComponent();

    private async void OnAddPerson(object? sender, RoutedEventArgs e) =>
        await new PersonWindow().ShowDialog(this);

    private async void OnAddGroup(object? sender, RoutedEventArgs e) =>
        await new GroupWindow().ShowDialog(this);

    private async void OnEditRola(object? sender, RoutedEventArgs e) =>
        await new RolaEditorWindow().ShowDialog(this);
}
