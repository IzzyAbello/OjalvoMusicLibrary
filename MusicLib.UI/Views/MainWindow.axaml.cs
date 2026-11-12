using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MusicLib.UI.ViewModels;

namespace MusicLib.UI.Views;

/// <summary>
/// Ventana principal. Su comportamiento está en MainViewModel; solo
/// se avisa cuándo el usuario pulsa y suelta la barra de progreso.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        PositionSlider.AddHandler
        (
            PointerPressedEvent,
            (_, _) => Player?.BeginSeek(),
            RoutingStrategies.Tunnel,
            handledEventsToo: true
        );
        PositionSlider.AddHandler
        (
            PointerReleasedEvent,
            (_, _) => Player?.EndSeek(),
            RoutingStrategies.Tunnel,
            handledEventsToo: true
        );
    }

    private PlayerViewModel? Player => (DataContext as MainViewModel)?.Player;
}
