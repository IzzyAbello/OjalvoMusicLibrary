using Avalonia.Controls;
using MusicLib.Data;
using MusicLib.Models;
using MusicLib.UI.ViewModels;
using MusicLib.UI.Views;

namespace MusicLib.UI.Services;

/// <summary>
/// Abre las ventanas secundarias como modales sobre una ventana dueña.
/// </summary>
public sealed class DialogService : IDialogService
{
    private readonly Window _owner;
    private readonly LibraryEditor _editor;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="owner">
    /// Ventana sobre la que se abren los diálogos.
    /// </param>
    /// <param name="editor">
    /// Lo usan los editores para guardar los cambios en la DB.
    /// </param>
    public DialogService(Window owner, LibraryEditor editor)
    {
        _owner = owner;
        _editor = editor;
    }

    /// <inheritdoc />
    public Task<RolaSearch?> ShowAdvancedSearchAsync() =>
        new AdvancedSearchWindow(new AdvancedSearchViewModel())
            .ShowDialog<RolaSearch?>(_owner);

    /// <inheritdoc />
    public Task<bool> ShowRolaEditorAsync(Rola rola) =>
        new RolaEditorWindow(new RolaEditorViewModel(rola, _editor))
            .ShowDialog<bool>(_owner);
}
