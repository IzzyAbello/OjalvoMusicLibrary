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
    private string _status = "";

    /// <summary>
    /// Texto del botón Minar (cambia a Cancelar mientras se mina).
    /// </summary>
    [ObservableProperty]
    private string _mineButtonText = MineText;

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
    public MainViewModel
    (
        string dbPath,
        ILog log,
        Miner miner,
        IFolderPicker folderPicker
    )
    {
        _dbPath = dbPath;
        _log = log;
        _miner = miner;
        _folderPicker = folderPicker;
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

        LoadFirstRolas();
    }

    private void ReportProgress(MiningProgress p)
    {
        if (_cts is null) return;

        var phase = p.Phase == MiningPhase.Reading ? "Leyendo" : "Guardando";
        Status = $"{phase} {p.Processed}/{p.Total}...";
    }

    private void LoadFirstRolas()
    {
        try
        {
            using var uow = new UnitOfWork(_dbPath, _log);
            var rolas = uow.Rolas.FindFirst();

            Rolas.Clear();
            foreach (var rola in rolas) Rolas.Add(rola);
        }
        catch (Exception ex)
        {
            Status = $"Error al cargar las rolas: {ex.Message}";
            _log.Write(ex.ToString());
        }
    }

    private static string Summary(MiningResult r) =>
        (r.Cancelled ? "Minado cancelado. " : "Minado terminado. ") +
        $"Encontradas: {r.Found}, insertadas: {r.Inserted}, " +
        $"omitidas: {r.Skipped}, fallidas: {r.Failed}.";
}
