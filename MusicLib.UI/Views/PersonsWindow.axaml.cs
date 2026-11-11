using Avalonia.Controls;
using MusicLib.UI.ViewModels;

namespace MusicLib.UI.Views;

/// <summary>
/// Ventana con la lista de personas.
/// </summary>
public partial class PersonsWindow : Window
{
    /// <summary>
    /// Constructor para el previsualizador de XAML.
    /// </summary>
    public PersonsWindow() => InitializeComponent();

    /// <summary>
    /// Constructor de producción.
    /// </summary>
    /// <param name="vm">
    /// ViewModel de la ventana; cuando pide cerrar, la ventana se cierra.
    /// </param>
    public PersonsWindow(PersonsViewModel vm) : this()
    {
        DataContext = vm;
        vm.CloseRequested += Close;
    }
}
