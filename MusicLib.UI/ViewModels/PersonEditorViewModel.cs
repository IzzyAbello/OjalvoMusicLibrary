using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLib.Data;
using MusicLib.Models;

namespace MusicLib.UI.ViewModels;

/// <summary>
/// ViewModel del formulario de persona, para agregar o editar: valida
/// mientras se escribe (con las mismas reglas del núcleo), guarda con
/// PersonsEditor y avisa a la ventana cuándo cerrarse.
/// </summary>
public sealed partial class PersonEditorViewModel : ObservableObject
{
    private readonly PersonsEditor _editor;

    // Id de la persona que se edita; null = alta.
    private readonly long? _id;

    /// <summary>
    /// true si se edita una persona existente (el nombre queda bloqueado).
    /// </summary>
    public bool IsEditing => _id is not null;

    /// <summary>
    /// Título de la ventana según el modo.
    /// </summary>
    public string Title => IsEditing ? "Editar persona" : "Agregar persona";

    /// <summary>
    /// Nombre artístico (obligatorio).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Problem))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string StageName { get; set; } = "";

    /// <summary>
    /// Nombre real (opcional).
    /// </summary>
    [ObservableProperty]
    public partial string RealName { get; set; } = "";

    /// <summary>
    /// Fecha de nacimiento, AAAA-MM-DD (opcional).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Problem))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string BirthDate { get; set; } = "";

    /// <summary>
    /// Fecha de muerte, AAAA-MM-DD (opcional).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Problem))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string DeathDate { get; set; } = "";

    /// <summary>
    /// Por qué no se puede guardar todavía; vacío si los datos son
    /// válidos.
    /// </summary>
    public string Problem =>
        PersonsEditor.Validate(StageName, BirthDate, DeathDate) ?? "";

    /// <summary>
    /// Error que dio el núcleo al guardar; vacío si no hay.
    /// </summary>
    [ObservableProperty]
    public partial string Error { get; set; } = "";

    /// <summary>
    /// Pide a la ventana que se cierre con el id de la persona guardada
    /// (null = cancelado).
    /// </summary>
    public event Action<long?>? CloseRequested;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="editor">Hace los cambios en la BD.</param>
    /// <param name="person">
    /// Persona a editar (se copian sus valores, no se modifica); null para
    /// dar de alta una nueva.
    /// </param>
    public PersonEditorViewModel(PersonsEditor editor, Person? person = null)
    {
        _editor = editor;
        if (person is null) return;

        _id = person.Id;
        StageName = person.StageName;
        RealName = person.RealName ?? "";
        BirthDate = person.BirthDate ?? "";
        DeathDate = person.DeathDate ?? "";
    }

    // Botón Guardar (o Enter): solo se puede con datos válidos. Si el
    // núcleo lo rechaza, el error se muestra y la ventana sigue abierta.
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        try
        {
            if (_id is not long id)
                CloseRequested?.Invoke
                (
                    _editor.Add(StageName, RealName, BirthDate, DeathDate)
                );
            else if (_editor.Update(id, RealName, BirthDate, DeathDate))
                CloseRequested?.Invoke(id);
            else
                Error = "La persona ya no existe.";
        }
        catch (Exception ex)
            when (ex is ArgumentException or InvalidOperationException)
        {
            Error = ex.Message;
        }
    }

    private bool CanSave() => Problem.Length == 0;

    // Al corregir el nombre, el error de "ya existe" deja de aplicar.
    // Se encarga el Toolkit.
    partial void OnStageNameChanged(string value) => Error = "";

    // Botón Cancelar (o Esc): cierra sin guardar.
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(null);
}
