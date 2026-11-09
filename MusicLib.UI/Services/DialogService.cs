using Avalonia.Controls;
using MusicLib.Data;
using MusicLib.UI.ViewModels;
using MusicLib.UI.Views;

namespace MusicLib.UI.Services;

/// <summary>
/// Abre las ventanas secundarias como modales sobre una ventana dueña.
/// </summary>
public sealed class DialogService : IDialogService
{
    private readonly Window _owner;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="owner">
    /// Ventana sobre la que se abren los diálogos.
    /// </param>
    public DialogService(Window owner) => _owner = owner;

    /// <inheritdoc />
    public Task<RolaSearch?> ShowAdvancedSearchAsync() =>
        new AdvancedSearchWindow(new AdvancedSearchViewModel())
            .ShowDialog<RolaSearch?>(_owner);
}
