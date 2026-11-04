using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MusicLib.UI.Views;

namespace MusicLib.UI;

/// <summary>
/// Aplicación Avalonia: abre la ventana principal.
/// </summary>
public partial class App : Application
{
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
            desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
