using Avalonia.Controls;
using MusicLib.Data;
using MusicLib.Logging;
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
    private readonly string _dbPath;
    private readonly ILog _log;
    private readonly LibraryEditor _editor;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="owner">
    /// Ventana sobre la que se abren los diálogos.
    /// </param>
    /// <param name="dbPath">
    /// Ruta de la BD (la usan las ventanas que leen o editan).
    /// </param>
    /// <param name="log">
    /// Log del programa.
    /// </param>
    public DialogService(Window owner, string dbPath, ILog log)
    {
        _owner = owner;
        _dbPath = dbPath;
        _log = log;
        _editor = new LibraryEditor(dbPath, log);
    }

    /// <inheritdoc />
    public Task<RolaSearch?> ShowAdvancedSearchAsync() =>
        new AdvancedSearchWindow(new AdvancedSearchViewModel())
            .ShowDialog<RolaSearch?>(_owner);

    /// <inheritdoc />
    public Task<bool> ShowRolaEditorAsync(Rola rola) =>
        new RolaEditorWindow(new RolaEditorViewModel(rola, _editor))
            .ShowDialog<bool>(_owner);

    /// <inheritdoc />
    public Task<bool> ShowAlbumEditorAsync(Album album) =>
        new AlbumEditorWindow(new AlbumEditorViewModel(album, _editor))
            .ShowDialog<bool>(_owner);

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message) =>
        new ConfirmWindow(new ConfirmViewModel(title, message))
            .ShowDialog<bool>(_owner);

    /// <inheritdoc />
    public Task ShowPersonsAsync()
    {
        var vm = new PersonsViewModel(_dbPath, _log);
        vm.Load();
        return new PersonsWindow(vm).ShowDialog(_owner);
    }
}
