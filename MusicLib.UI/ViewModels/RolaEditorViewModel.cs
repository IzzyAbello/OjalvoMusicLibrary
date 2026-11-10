using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLib.Data;
using MusicLib.Models;

namespace MusicLib.UI.ViewModels;

/// <summary>
/// ViewModel del editor de rola: copia los valores de la rola, los guarda
/// con LibraryEditor y avisa a la ventana cuándo cerrarse.
/// </summary>
public sealed partial class RolaEditorViewModel : ObservableObject
{
    private readonly long _id;
    private readonly LibraryEditor _editor;

    /// <summary>
    /// Título de la rola.
    /// </summary>
    [ObservableProperty]
    public partial string Title { get; set; }

    /// <summary>
    /// Nombre del intérprete.
    /// </summary>
    [ObservableProperty]
    public partial string PerformerName { get; set; }

    /// <summary>
    /// Nombre del álbum.
    /// </summary>
    [ObservableProperty]
    public partial string AlbumName { get; set; }

    /// <summary>
    /// Número de pista.
    /// </summary>
    [ObservableProperty]
    public partial decimal? Track { get; set; }

    /// <summary>
    /// Año.
    /// </summary>
    [ObservableProperty]
    public partial decimal? Year { get; set; }

    /// <summary>
    /// Género.
    /// </summary>
    [ObservableProperty]
    public partial string Genre { get; set; }

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
    /// <param name="rola">
    /// Rola a editar; se copian sus valores, no se modifica.
    /// </param>
    /// <param name="editor">
    /// Hace los cambios en la DB.
    /// </param>
    public RolaEditorViewModel(Rola rola, LibraryEditor editor)
    {
        _id = rola.Id;
        _editor = editor;
        Title = rola.Title;
        PerformerName = rola.Performer?.Name ?? "";
        AlbumName = rola.Album?.Name ?? "";
        Track = rola.Track;
        Year = rola.Year;
        Genre = rola.Genre;
    }

    // Botón Guardar (o Enter): si LibraryEditor rechaza los datos, el
    // error se muestra y la ventana sigue abierta para corregirlo.
    [RelayCommand]
    private void Save()
    {
        try
        {
            var saved = _editor.UpdateRola
            (
                _id,
                Title,
                PerformerName,
                AlbumName,
                (int)(Track ?? 0),
                (int)(Year ?? 0),
                Genre
            );

            if (saved) CloseRequested?.Invoke(true);
            else Error = "La rola ya no existe.";
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
