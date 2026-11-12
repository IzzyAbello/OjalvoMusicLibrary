using Avalonia.Controls;
using MusicLib.UI.ViewModels;

namespace MusicLib.UI.Views;

/// <summary>
/// Ventana con la lista de grupos.
/// </summary>
public partial class GroupsWindow : Window
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public GroupsWindow() => InitializeComponent();

    /// <summary>
    /// Conecta el ViewModel: la ventana se crea antes que él porque es la
    /// dueña de los diálogos que él abre.
    /// </summary>
    /// <param name="vm">
    /// ViewModel de la ventana; cuando pide cerrar, la ventana se cierra.
    /// </param>
    public void Attach(GroupsViewModel vm)
    {
        DataContext = vm;
        vm.CloseRequested += Close;
    }
}
