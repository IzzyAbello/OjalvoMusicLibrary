using MusicLib.Data;

namespace MusicLib.UI.Services;

/// <summary>
/// Abre las ventanas secundarias. Separa a los ViewModels de las ventanas.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Muestra la búsqueda avanzada como ventana modal.
    /// </summary>
    /// <returns>
    /// Los criterios elegidos, o null si el usuario canceló.
    /// </returns>
    Task<RolaSearch?> ShowAdvancedSearchAsync();
}
