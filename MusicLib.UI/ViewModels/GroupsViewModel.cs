using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLib.Data;
using MusicLib.Logging;
using MusicLib.Models;
using MusicLib.UI.Services;

namespace MusicLib.UI.ViewModels;

/// <summary>
/// ViewModel de la ventana de grupos: muestra la tabla groups.
/// </summary>
public sealed partial class GroupsViewModel : ObservableObject
{
    private readonly string _dbPath;
    private readonly ILog _log;
    private readonly IDialogService _dialogs;

    /// <summary>
    /// Grupos que muestra la lista.
    /// </summary>
    public ObservableCollection<Group> Groups { get; } = new();

    /// <summary>
    /// Grupo seleccionado en la lista, o null si no hay ninguno.
    /// </summary>
    [ObservableProperty]
    public partial Group? SelectedGroup { get; set; }

    /// <summary>
    /// Mensaje de error; vacío si no hay.
    /// </summary>
    [ObservableProperty]
    public partial string Error { get; set; } = "";

    /// <summary>
    /// Pide a la ventana que se cierre.
    /// </summary>
    public event Action? CloseRequested;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dbPath">Ruta de la BD.</param>
    /// <param name="log">Log del programa.</param>
    /// <param name="dialogs">
    /// Abre los diálogos sobre la ventana de grupos.
    /// </param>
    public GroupsViewModel(string dbPath, ILog log, IDialogService dialogs)
    {
        _dbPath = dbPath;
        _log = log;
        _dialogs = dialogs;
    }

    /// <summary>
    /// Lee los grupos de la DB y reemplaza los de la lista. Si falla, lo
    /// muestra en Error (y en el log) sin lanzar.
    /// </summary>
    /// <returns>true si se cargaron; false si hubo un error.</returns>
    public bool Load()
    {
        try
        {
            using var uow = new UnitOfWork(_dbPath, _log);
            var groups = uow.Groups.FindAll();

            Groups.Clear();
            foreach (var group in groups) Groups.Add(group);
            Error = "";
            return true;
        }
        catch (Exception ex)
        {
            Error = $"Error al cargar los grupos: {ex.Message}";
            _log.Write(ex.ToString());
            return false;
        }
    }

    // Botón Agregar: abre el formulario y, si se guardó, recarga la lista
    // y selecciona al grupo nuevo.
    [RelayCommand]
    private async Task AddAsync()
    {
        if (await _dialogs.ShowGroupEditorAsync() is not long id) return;
        if (!Load()) return;
        SelectedGroup = Groups.FirstOrDefault(g => g.Id == id);
    }

    // Botón Cerrar (o Esc).
    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
