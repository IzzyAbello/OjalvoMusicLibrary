using Avalonia.Controls;
using MusicLib.UI.ViewModels;

namespace MusicLib.UI.Views;

/// <summary>
/// Formulario de un grupo.
/// </summary>
public partial class GroupWindow : Window
{
    /// <summary>
    /// Constructor para el previsualizador de XAML.
    /// </summary>
    public GroupWindow() => InitializeComponent();

    /// <summary>
    /// Constructor de producción.
    /// </summary>
    /// <param name="vm">
    /// ViewModel de la ventana; cuando pide cerrar, la ventana se cierra
    /// devolviendo el id guardado (o null).
    /// </param>
    public GroupWindow(GroupEditorViewModel vm) : this()
    {
        DataContext = vm;
        vm.CloseRequested += id => Close(id);
    }
}
