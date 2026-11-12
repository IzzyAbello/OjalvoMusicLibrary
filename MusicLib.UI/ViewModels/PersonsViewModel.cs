using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLib.Data;
using MusicLib.Logging;
using MusicLib.Models;
using MusicLib.UI.Services;

namespace MusicLib.UI.ViewModels;

/// <summary>
/// ViewModel de la ventana de personas: muestra la tabla persons.
/// </summary>
public sealed partial class PersonsViewModel : ObservableObject
{
    private readonly string _dbPath;
    private readonly ILog _log;
    private readonly IDialogService _dialogs;
    private readonly PersonsEditor _editor;

    /// <summary>
    /// Personas que muestra la lista.
    /// </summary>
    public ObservableCollection<Person> Persons { get; } = new();

    /// <summary>
    /// Persona seleccionada en la lista, o null si no hay ninguna.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    public partial Person? SelectedPerson { get; set; }

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
    /// Abre los diálogos sobre la ventana de personas.
    /// </param>
    /// <param name="editor">Elimina personas de la BD.</param>
    public PersonsViewModel
    (
        string dbPath,
        ILog log,
        IDialogService dialogs,
        PersonsEditor editor
    )
    {
        _dbPath = dbPath;
        _log = log;
        _dialogs = dialogs;
        _editor = editor;
    }

    /// <summary>
    /// Lee las personas de la DB y reemplaza las de la lista. Si falla, lo
    /// muestra en Error (y en el log) sin lanzar.
    /// </summary>
    /// <returns>true si se cargaron; false si hubo un error.</returns>
    public bool Load()
    {
        try
        {
            using var uow = new UnitOfWork(_dbPath, _log);
            var persons = uow.Persons.FindAll();

            Persons.Clear();
            foreach (var person in persons) Persons.Add(person);
            Error = "";
            return true;
        }
        catch (Exception ex)
        {
            Error = $"Error al cargar las personas: {ex.Message}";
            _log.Write(ex.ToString());
            return false;
        }
    }

    // Botón Agregar: abre el formulario y, si se guardó, recarga la lista
    // y selecciona a la persona nueva.
    [RelayCommand]
    private async Task AddAsync()
    {
        if (await _dialogs.ShowPersonEditorAsync() is not long id) return;
        if (!Load()) return;
        SelectedPerson = Persons.FirstOrDefault(p => p.Id == id);
    }

    // Botón Editar: abre el formulario con la persona seleccionada y, si se
    // guardó, recarga la lista y la vuelve a seleccionar.
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task EditAsync()
    {
        if (SelectedPerson is not { } person) return;
        if (await _dialogs.ShowPersonEditorAsync(person) is not long id) 
            return;
        if (!Load()) return;
        SelectedPerson = Persons.FirstOrDefault(p => p.Id == id);
    }

    private bool HasSelection() => SelectedPerson is not null;

    // Botón Eliminar: pide confirmación y, si se acepta, borra a la
    // persona y recarga la lista.
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteAsync()
    {
        if (SelectedPerson is not { } person) return;

        var confirmed = await _dialogs.ConfirmAsync
        (
            "Eliminar persona",
            $"¿Eliminar a \"{person.StageName}\"? También sale de los " +
            "grupos en los que esté. Sus rolas no se tocan."
        );
        if (!confirmed) return;

        var deleted = _editor.Delete(person.Id);
        if (!Load()) return;
        if (!deleted) Error = "La persona ya no existe.";
    }

    // Botón Cerrar (o Esc).
    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
