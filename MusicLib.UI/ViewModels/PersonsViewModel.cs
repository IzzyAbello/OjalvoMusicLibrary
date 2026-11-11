using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLib.Data;
using MusicLib.Logging;
using MusicLib.Models;

namespace MusicLib.UI.ViewModels;

/// <summary>
/// ViewModel de la ventana de personas: muestra la tabla persons.
/// </summary>
public sealed partial class PersonsViewModel : ObservableObject
{
    private readonly string _dbPath;
    private readonly ILog _log;

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
    public PersonsViewModel(string dbPath, ILog log)
    {
        _dbPath = dbPath;
        _log = log;
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

    // Botón Cerrar (o Esc).
    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
