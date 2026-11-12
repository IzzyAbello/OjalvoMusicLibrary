using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLib.Data;
using MusicLib.Models;

namespace MusicLib.UI.ViewModels;

/// <summary>
/// ViewModel del formulario de grupo, para agregar o editar: valida
/// mientras se escribe (con las mismas reglas del núcleo), guarda con
/// GroupsEditor y avisa a la ventana cuándo cerrarse.
/// </summary>
public sealed partial class GroupEditorViewModel : ObservableObject
{
    private readonly GroupsEditor _editor;

    // Id del grupo que se edita; null = alta.
    private readonly long? _id;

    // Miembros que se guardan. Todavía no se editan en el formulario: al
    // editar se conservan los actuales; al agregar, el grupo nace vacío.
    private readonly List<long> _memberIds;

    /// <summary>
    /// true si se edita un grupo existente (el nombre queda bloqueado).
    /// </summary>
    public bool IsEditing => _id is not null;

    /// <summary>
    /// Título de la ventana según el modo.
    /// </summary>
    public string Title => IsEditing ? "Editar grupo" : "Agregar grupo";

    /// <summary>
    /// Nombre del grupo (obligatorio).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Problem))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Name { get; set; } = "";

    /// <summary>
    /// Fecha de inicio, AAAA-MM-DD (opcional).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Problem))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string StartDate { get; set; } = "";

    /// <summary>
    /// Fecha de disolución, AAAA-MM-DD (opcional).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Problem))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string EndDate { get; set; } = "";

    /// <summary>
    /// Por qué no se puede guardar todavía; vacío si los datos son
    /// válidos.
    /// </summary>
    public string Problem =>
        GroupsEditor.Validate(Name, StartDate, EndDate) ?? "";

    /// <summary>
    /// Error que dio el núcleo al guardar; vacío si no hay.
    /// </summary>
    [ObservableProperty]
    public partial string Error { get; set; } = "";

    /// <summary>
    /// Pide a la ventana que se cierre con el id del grupo guardado
    /// (null = cancelado).
    /// </summary>
    public event Action<long?>? CloseRequested;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="editor">Hace los cambios en la BD.</param>
    /// <param name="group">
    /// Grupo a editar (se copian sus valores, no se modifica); null para
    /// dar de alta uno nuevo.
    /// </param>
    /// <param name="memberIds">
    /// Ids de los miembros actuales del grupo a editar.
    /// </param>
    public GroupEditorViewModel
    (
        GroupsEditor editor,
        Group? group = null,
        IEnumerable<long>? memberIds = null
    )
    {
        _editor = editor;
        _memberIds = memberIds?.ToList() ?? [];
        if (group is null) return;

        _id = group.Id;
        Name = group.Name;
        StartDate = group.StartDate ?? "";
        EndDate = group.EndDate ?? "";
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
                    _editor.Add(Name, StartDate, EndDate, _memberIds)
                );
            else if (_editor.Update(id, StartDate, EndDate, _memberIds))
                CloseRequested?.Invoke(id);
            else
                Error = "El grupo ya no existe.";
        }
        catch (Exception ex)
            when (ex is ArgumentException or InvalidOperationException)
        {
            Error = ex.Message;
        }
    }

    private bool CanSave() => Problem.Length == 0;

    // Al corregir el nombre, el error de "ya existe" deja de aplicar.
    // Toolkit
    partial void OnNameChanged(string value) => Error = "";

    // Botón Cancelar (o Esc): cierra sin guardar.
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(null);
}
