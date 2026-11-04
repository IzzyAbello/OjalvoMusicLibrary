using Avalonia;
using MusicLib.Cli;

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

    private static void StartAvalonia() =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(Array.Empty<string>());

    /// <summary>
    /// Previsualizador de XAML.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
