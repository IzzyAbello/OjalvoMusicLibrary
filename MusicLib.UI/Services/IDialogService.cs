using MusicLib.Data;
using MusicLib.Models;

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

    /// <summary>
    /// Muestra el editor de una rola como ventana modal.
    /// </summary>
    /// <param name="rola">
    /// Rola a editar.
    /// </param>
    /// <returns>
    /// true si se guardaron los cambios; false si se canceló.
    /// </returns>
    Task<bool> ShowRolaEditorAsync(Rola rola);
}
