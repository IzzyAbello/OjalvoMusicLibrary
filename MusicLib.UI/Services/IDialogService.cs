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

    /// <summary>
    /// Muestra el editor de un álbum como ventana modal.
    /// </summary>
    /// <param name="album">
    /// Álbum a editar.
    /// </param>
    /// <returns>
    /// true si se guardaron los cambios; false si se canceló.
    /// </returns>
    Task<bool> ShowAlbumEditorAsync(Album album);

    /// <summary>
    /// Pregunta al usuario si quiere continuar (ventana modal).
    /// </summary>
    /// <param name="title">Título de la ventana.</param>
    /// <param name="message">Pregunta que se le hace.</param>
    /// <returns>
    /// true si aceptó; false si canceló o cerró la ventana.
    /// </returns>
    Task<bool> ConfirmAsync(string title, string message);

    /// <summary>
    /// Muestra la ventana de personas como modal.
    /// </summary>
    /// <returns>Se completa cuando la ventana se cierra.</returns>
    Task ShowPersonsAsync();

    /// <summary>
    /// Muestra el formulario de persona (modal).
    /// </summary>
    /// <param name="person">
    /// Persona a editar, o null para agregar una nueva.
    /// </param>
    /// <returns>
    /// El id de la persona guardada, o null si se canceló.
    /// </returns>
    Task<long?> ShowPersonEditorAsync(Person? person = null);

    /// <summary>
    /// Muestra la ventana de grupos como modal.
    /// </summary>
    /// <returns>Se completa cuando la ventana se cierra.</returns>
    Task ShowGroupsAsync();

    /// <summary>
    /// Muestra el formulario de grupo (modal).
    /// </summary>
    /// <param name="group">
    /// Grupo a editar, o null para agregar uno nuevo.
    /// </param>
    /// <returns>
    /// El id del grupo guardado, o null si se canceló.
    /// </returns>
    Task<long?> ShowGroupEditorAsync(Group? group = null);
}
