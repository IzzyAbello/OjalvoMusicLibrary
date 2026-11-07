using Avalonia;
using MusicLib.Cli;
using MusicLib.Logging;

namespace MusicLib.UI;

/// <summary>
/// Punto de entrada de 'ojalvomusic'.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Punto de entrada del programa.
    /// </summary>
    /// <param name="args">
    /// Parámetros de entrada.
    /// </param>
    /// <returns>
    /// Estado de salida.
    /// </returns>
    [STAThread]
    public static int Main(string[] args) =>
        new Controller(args, runView: StartAvalonia).Execute();

    private static void StartAvalonia(string dbPath, ILog log) =>
        AppBuilder.Configure(() => new App(dbPath, log))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .StartWithClassicDesktopLifetime(Array.Empty<string>());

    /// <summary>
    /// Previsualizador de XAML.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
