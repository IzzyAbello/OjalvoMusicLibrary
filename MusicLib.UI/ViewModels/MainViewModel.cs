using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicLib.Data;
using MusicLib.Logging;
using MusicLib.Mining;
using MusicLib.Models;
using MusicLib.UI.Services;

namespace MusicLib.UI.ViewModels;

/// <summary>
/// ViewModel de la ventana principal.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private const string MineText = "Minar";
    private const string CancelText = "Cancelar";

    private readonly string _dbPath;
    private readonly ILog _log;
    private readonly Miner _miner;
    private readonly IFolderPicker _folderPicker;
    private readonly IDialogService _dialogs;

    // No null mientras hay una minería en curso.
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Rolas que muestra la tabla.
    /// </summary>
    public ObservableCollection<Rola> Rolas { get; } = new();

    /// <summary>
    /// Texto de la barra de estado.
    /// </summary>
    [ObservableProperty]
    public partial string Status { get; set; } = "";

    /// <summary>
    /// Texto del botón Minar (cambia a Cancelar mientras se mina).
    /// </summary>
    [ObservableProperty]
    public partial string MineButtonText { get; set; } = MineText;

    /// <summary>
    /// Texto de la caja de búsqueda simple.
    /// </summary>
    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dbPath">
    /// Ruta de la BD (ya creada).
    /// </param>
    /// <param name="log">
    /// Log del programa.
    /// </param>
    /// <param name="miner">
    /// Minero reutilizable para toda la vida de la app.
    /// </param>
    /// <param name="folderPicker">
    /// Selector de carpetas.
    /// </param>
    /// <param name="dialogs">
    /// Abre las ventanas secundarias.
    /// </param>
    public MainViewModel
    (
        string dbPath,
        ILog log,
        Miner miner,
        IFolderPicker folderPicker,
        IDialogService dialogs
    )
    {
        _dbPath = dbPath;
        _log = log;
        _miner = miner;
        _folderPicker = folderPicker;
        _dialogs = dialogs;
    }

    // Concurrente para que el mismo botón sirva de "Cancelar" mientras
    // la minería corre.
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task MineAsync()
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            Status = "Cancelando...";
            return;
        }

        var root = await _folderPicker.PickAsync("Elige la carpeta a minar");
        if (root is null) return;

        _cts = new CancellationTokenSource();
        MineButtonText = CancelText;
        Status = $"Minando '{root}'...";

        var progress = new Progress<MiningProgress>(ReportProgress);

        try
        {
            var result = await _miner.MineAsync(root, progress, _cts.Token);
            Status = Summary(result);
        }
        catch (Exception ex)
        {
            Status = $"Error al minar: {ex.Message}";
            _log.Write(ex.ToString());
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            MineButtonText = MineText;
        }

        LoadRolas();
    }

    private void ReportProgress(MiningProgress p)
    {
        if (_cts is null) return;

        var phase = p.Phase == MiningPhase.Reading ? "Leyendo" : "Guardando";
        Status = $"{phase} {p.Processed}/{p.Total}...";
    }

    /// <summary>
    /// Lee rolas de la DB y reemplaza las de la tabla. Si falla, lo
    /// reporta en la barra de estado (y en el log) sin lanzar.
    /// </summary>
    /// <param name="criteria">
    /// Qué buscar; null = todas las rolas (FindFirst).
    /// </param>
    /// <returns>
    /// true si se cargaron; false si hubo un error.
    /// </returns>
    public bool LoadRolas(RolaSearch? criteria = null)
    {
        try
        {
            using var uow = new UnitOfWork(_dbPath, _log);
            var rolas = criteria is null
                ? uow.Rolas.FindFirst()
                : uow.Rolas.Search(criteria);

            Rolas.Clear();
            foreach (var rola in rolas) Rolas.Add(rola);
            return true;
        }
        catch (Exception ex)
        {
            Status = $"Error al cargar las rolas: {ex.Message}";
            _log.Write(ex.ToString());
            return false;
        }
    }

    // Botón Recargar: limpia la búsqueda y vuelve a mostrar todo. El
    // estado solo se toca si la carga salió bien; si no, se queda el
    // mensaje de error.
    [RelayCommand]
    private void Reload()
    {
        SearchText = "";
        if (LoadRolas()) Status = $"Biblioteca recargada: {Rolas.Count} rolas.";
    }

    // Botón Buscar (o Enter en la caja). El texto se busca en título,
    // intérprete, álbum y género; vacío muestra todas las rolas.
    [RelayCommand]
    private void Search()
    {
        var text = SearchText.Trim();
        // Si falla cuando busca.
        if (!LoadRolas(new RolaSearch(Any: text))) return;

        Status = text.Length == 0
            ? $"Mostrando todas las rolas: {Rolas.Count}."
            : $"{Rolas.Count} resultados para '{text}'.";
    }

    // Botón Búsqueda avanzada: abre la ventana y, si no se canceló,
    // busca con los criterios que regresa.
    [RelayCommand]
    private async Task AdvancedSearchAsync()
    {
        var criteria = await _dialogs.ShowAdvancedSearchAsync();
        if (criteria is null) return;

        if (LoadRolas(criteria))
            Status = $"{Rolas.Count} resultados (búsqueda avanzada).";
    }

    private static string Summary(MiningResult r) =>
        (r.Cancelled ? "Minado cancelado. " : "Minado terminado. ") +
        $"Encontradas: {r.Found}, insertadas: {r.Inserted}, " +
        $"omitidas: {r.Skipped}, fallidas: {r.Failed}.";
}
