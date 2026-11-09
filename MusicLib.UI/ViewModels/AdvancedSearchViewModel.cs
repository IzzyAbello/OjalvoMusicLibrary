using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLib.Data;

namespace MusicLib.UI.ViewModels;

/// <summary>
/// ViewModel de la búsqueda avanzada: guarda los campos, valida y avisa a
/// la ventana cuándo cerrarse y con qué resultado.
/// </summary>
public sealed partial class AdvancedSearchViewModel : ObservableObject
{
    /// <summary>
    /// Texto contenido en el título.
    /// </summary>
    [ObservableProperty]
    public partial string Title { get; set; } = "";

    /// <summary>
    /// Texto contenido en el intérprete.
    /// </summary>
    [ObservableProperty]
    public partial string Performer { get; set; } = "";

    /// <summary>
    /// Texto contenido en el álbum.
    /// </summary>
    [ObservableProperty]
    public partial string Album { get; set; } = "";

    /// <summary>
    /// Texto contenido en el género.
    /// </summary>
    [ObservableProperty]
    public partial string Genre { get; set; } = "";

    /// <summary>
    /// Año mínimo; null = sin límite. Es decimal porque así lo maneja
    /// NumericUpDown.
    /// </summary>
    [ObservableProperty]
    public partial decimal? YearFrom { get; set; }

    /// <summary>
    /// Año máximo; null = sin límite.
    /// </summary>
    [ObservableProperty]
    public partial decimal? YearTo { get; set; }

    /// <summary>
    /// Mensaje de validación; vacío si no hay error.
    /// </summary>
    [ObservableProperty]
    public partial string Error { get; set; } = "";

    /// <summary>
    /// Pide a la ventana que se cierre con estos criterios (null =
    /// cancelado). Así el ViewModel no conoce a la ventana.
    /// </summary>
    public event Action<RolaSearch?>? CloseRequested;

    // Botón Buscar (o Enter): valida y, si todo está bien, cierra con
    // los criterios.
    [RelayCommand]
    private void Search()
    {
        try
        {
            var criteria = RolaSearch.FromFields
            (
                Title,
                Performer,
                Album,
                Genre,
                ToYear(YearFrom),
                ToYear(YearTo)
            );
            CloseRequested?.Invoke(criteria);
        }
        catch (ArgumentException ex)
        {
            Error = ex.Message;
        }
    }

    // Botón Cancelar (o Esc): cierra sin resultado.
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(null);

    private static int? ToYear(decimal? value) =>
        value is null ? null : (int)value;
}
