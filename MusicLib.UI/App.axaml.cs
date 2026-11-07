using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MusicLib.Logging;
using MusicLib.Mining;
using MusicLib.UI.Services;
using MusicLib.UI.ViewModels;
using MusicLib.UI.Views;

namespace MusicLib.UI;

/// <summary>
/// Aplicación Avalonia: abre la ventana principal y le conecta su ViewModel.
/// </summary>
public partial class App : Application
{
    private readonly string? _dbPath;
    private readonly ILog _log = NullLog.Instance;

    /// <summary>
    /// Constructor para el previsualizador de XAML (sin DB).
    /// </summary>
    public App() { }

    /// <summary>
    /// Constructor de producción.
    /// </summary>
    /// <param name="dbPath">
    /// Ruta de la DB (ya creada).
    /// </param>
    /// <param name="log">
    /// Log del programa.
    /// </param>
    public App(string dbPath, ILog log)
    {
        _dbPath = dbPath;
        _log = log;
    }

    /// <summary>
    /// Carga la vista a partir de su XML.
    /// </summary>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Abre la ventana principal.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();

            if (_dbPath is not null)
                window.DataContext = new MainViewModel
                (
                    _dbPath,
                    _log,
                    new Miner
                    (
                        _dbPath,
                        new FileScanner(_log),
                        new TagReader(_log),
                        _log
                    ),
                    new FolderPicker(window)
                );

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
