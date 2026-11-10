using Avalonia.Controls;
using MusicLib.UI.ViewModels;

namespace MusicLib.UI.Views;

/// <summary>
/// Ventana para editar los datos de una rola.
/// </summary>
public partial class RolaEditorWindow : Window
{
    /// <summary>
    /// Constructor para el previsualizador de XAML.
    /// </summary>
    public RolaEditorWindow() => InitializeComponent();

    /// <summary>
    /// Constructor de producción.
    /// </summary>
    /// <param name="vm">
    /// ViewModel de la ventana; cuando pide cerrar, la ventana se cierra
    /// devolviendo si se guardó.
    /// </param>
    public RolaEditorWindow(RolaEditorViewModel vm) : this()
    {
        DataContext = vm;
        vm.CloseRequested += saved => Close(saved);
    }
}
