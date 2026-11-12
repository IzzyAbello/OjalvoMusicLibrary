using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLib.Data;

namespace MusicLib.UI.ViewModels;

/// <summary>
/// ViewModel del formulario de persona: valida mientras se escribe (con
/// las mismas reglas del núcleo), guarda con PersonsEditor y avisa a la
/// ventana cuándo cerrarse.
/// </summary>
public sealed partial class PersonEditorViewModel : ObservableObject
{
    private readonly PersonsEditor _editor;

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
    public PersonEditorViewModel(PersonsEditor editor) => _editor = editor;

    // Botón Guardar (o Enter): solo se puede con datos válidos. Si el
    // núcleo lo rechaza, el error se muestra y la ventana sigue abierta.
    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        try
        {
            var id = _editor.Add(StageName, RealName, BirthDate, DeathDate);
            CloseRequested?.Invoke(id);
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
