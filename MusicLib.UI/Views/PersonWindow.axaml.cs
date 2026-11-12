using Avalonia.Controls;
using MusicLib.UI.ViewModels;

namespace MusicLib.UI.Views;

/// <summary>
/// Formulario de una persona.
/// </summary>
public partial class PersonWindow : Window
{
    /// <summary>
    /// Constructor para el previsualizador de XAML.
    /// </summary>
    public PersonWindow() => InitializeComponent();

    /// <summary>
    /// Constructor de producción.
    /// </summary>
    /// <param name="vm">
    /// ViewModel de la ventana; cuando pide cerrar, la ventana se cierra
    /// devolviendo el id guardado (o null).
    /// </param>
    public PersonWindow(PersonEditorViewModel vm) : this()
    {
        DataContext = vm;
        vm.CloseRequested += id => Close(id);
    }
}
