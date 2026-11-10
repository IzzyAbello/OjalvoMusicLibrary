using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLib.Data;
using MusicLib.Models;

namespace MusicLib.UI.ViewModels;

/// <summary>
/// ViewModel del editor de álbum: copia los valores del álbum, los guarda
/// con LibraryEditor (todas sus rolas ven el cambio) y avisa a la ventana
/// cuándo cerrarse.
/// </summary>
public sealed partial class AlbumEditorViewModel : ObservableObject
{
    private readonly long _id;
    private readonly LibraryEditor _editor;

    /// <summary>
    /// Nombre del álbum.
    /// </summary>
    [ObservableProperty]
    public partial string Name { get; set; }

    /// <summary>
    /// Año; null (caja vacía) se guarda como 0.
    /// </summary>
    [ObservableProperty]
    public partial decimal? Year { get; set; }

    /// <summary>
    /// Directorio del álbum; solo se muestra.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Mensaje de error.
    /// </summary>
    [ObservableProperty]
    public partial string Error { get; set; } = "";

    /// <summary>
    /// Pide a la ventana que se cierre.
    /// </summary>
    public event Action<bool>? CloseRequested;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="album">
    /// Álbum a editar; se copian sus valores, no se modifica.
    /// </param>
    /// <param name="editor">
    /// Hace los cambios en la DB.
    /// </param>
    public AlbumEditorViewModel(Album album, LibraryEditor editor)
    {
        _id = album.Id;
        _editor = editor;
        Name = album.Name;
        Year = album.Year;
        Path = album.Path;
    }

    // Botón Guardar (o Enter): si LibraryEditor rechaza los datos (vacío,
    // nombre repetido en la carpeta...), el error se muestra y la ventana
    // sigue abierta para corregirlo.
    [RelayCommand]
    private void Save()
    {
        try
        {
            if (_editor.UpdateAlbum(_id, Name, (int)(Year ?? 0)))
                CloseRequested?.Invoke(true);
            else
                Error = "El álbum ya no existe.";
        }
        catch (Exception ex)
            when (ex is ArgumentException or InvalidOperationException)
        {
            Error = ex.Message;
        }
    }

    // Botón Cancelar (o Esc): cierra sin guardar.
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}
