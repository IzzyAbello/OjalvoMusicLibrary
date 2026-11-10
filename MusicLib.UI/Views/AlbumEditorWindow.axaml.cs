using Avalonia.Controls;
using MusicLib.UI.ViewModels;

namespace MusicLib.UI.Views;

/// <summary>
/// Ventana para editar el nombre y el año de un álbum.
/// </summary>
public partial class AlbumEditorWindow : Window
{
    /// <summary>
    /// Constructor para el previsualizador de XAML.
    /// </summary>
    public AlbumEditorWindow() => InitializeComponent();

    /// <summary>
    /// Constructor de producción.
    /// </summary>
    /// <param name="vm">
    /// ViewModel de la ventana; cuando pide cerrar, la ventana se cierra
    /// devolviendo si se guardó.
    /// </param>
    public AlbumEditorWindow(AlbumEditorViewModel vm) : this()
    {
        DataContext = vm;
        vm.CloseRequested += saved => Close(saved);
    }
}
