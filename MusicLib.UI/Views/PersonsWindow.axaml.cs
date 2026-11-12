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
    /// Conecta el ViewModel: la ventana se crea antes que él porque es la
    /// dueña de los diálogos que él abre.
    /// </summary>
    /// <param name="vm">
    /// ViewModel de la ventana; cuando pide cerrar, la ventana se cierra.
    /// </param>
    public void Attach(PersonsViewModel vm)
    {
        DataContext = vm;
        vm.CloseRequested += Close;
    }
}
