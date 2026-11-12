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

    /// <summary>
    /// Personas que muestra la lista.
    /// </summary>
    public ObservableCollection<Person> Persons { get; } = new();

    /// <summary>
    /// Persona seleccionada en la lista, o null si no hay ninguna.
    /// </summary>
    [ObservableProperty]
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
    public PersonsViewModel(string dbPath, ILog log, IDialogService dialogs)
    {
        _dbPath = dbPath;
        _log = log;
        _dialogs = dialogs;
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

    // Botón Cerrar (o Esc).
    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
