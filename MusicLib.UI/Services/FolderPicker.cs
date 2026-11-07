using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace MusicLib.UI.Services;

/// <summary>
/// Selector de carpetas usando el StorageProvider de una ventana.
/// </summary>
public sealed class FolderPicker : IFolderPicker
{
    private readonly TopLevel _owner;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="owner">
    /// Ventana dueña del diálogo.
    /// </param>
    public FolderPicker(TopLevel owner) => _owner = owner;

    /// <inheritdoc />
    public async Task<string?> PickAsync(string title)
    {
        var folders = await _owner.StorageProvider.OpenFolderPickerAsync
        (
            new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            }
        );

        return (folders.Count == 0) ? null : folders[0].TryGetLocalPath();
    }
}
