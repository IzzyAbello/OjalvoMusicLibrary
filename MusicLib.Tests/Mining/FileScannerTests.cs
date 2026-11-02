using MusicLib.Mining;
using MusicLib.Tests.Support;
using Xunit;

namespace MusicLib.Tests.Mining;

/// <summary>
/// Prueba el escáner de verdad sobre carpetas temporales:
/// encuentra .mp3 recursivamente
/// </summary>
public sealed class FileScannerTests : IDisposable
{
    private readonly TempDir _tmp = new();
    private readonly FileScanner _scanner = new();

    /// <summary>
    /// Cumple el contrato con IDisposable y elimina los datos temporales. 
    /// </summary>
    public void Dispose() => _tmp.Dispose();

    private string Touch(params string[] parts)
    {
        var path = _tmp.Combine(parts);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, Array.Empty<byte>());
        return path;
    }

    /// <summary>
    /// Obtiene toos los MP3 e ignora a los demás archivos.
    /// </summary>
    [Fact]
    public void EnumerateMp3_FindsMp3RecursivelyAndIgnoresOtherFormats()
    {
        var a = Touch("a.mp3");
        var b = Touch("Disco", "b.mp3");
        var c = Touch("Disco", "Bonus", "c.mp3");
        Touch("notas.txt");
        Touch("Disco", "portada.jpg");
        Touch("Disco", "cancion.flac");
        Touch("Disco", "cancion.mp3.bak");

        var found = _scanner.EnumerateMp3(_tmp.Root).ToList();

        Assert.Equal(new[] { a, b, c }.OrderBy(x => x), found.OrderBy(x => x));
    }

    /// <summary>
    /// Reconoce los MP3 (Case sensitive).
    /// </summary>
    [Fact]
    public void EnumerateMp3_IsCaseInsensitiveOnTheExtension()
    {
        var lower = Touch("minusculas.mp3");
        var upper = Touch("MAYUSCULAS.MP3");
        var mixed = Touch("Sub", "Mixta.Mp3");

        var found = _scanner.EnumerateMp3(_tmp.Root).ToList();

        Assert.Equal(new[] { lower, upper, mixed }.OrderBy(x => x), found.OrderBy(x => x));
    }

    /// <summary>
    /// Devuelve las rutas absolutas.
    /// </summary>
    [Fact]
    public void EnumerateMp3_ReturnsAbsolutePaths()
    {
        Touch("Disco", "a.mp3");

        var found = _scanner.EnumerateMp3(_tmp.Root).ToList();

        Assert.NotEmpty(found);
        Assert.All(found, f => Assert.True(Path.IsPathRooted(f)));
    }

    /// <summary>
    /// Directorio vacío no escanea nada.
    /// </summary>
    [Fact]
    public void EnumerateMp3_EmptyDirectory_ReturnsNothing()
    {
        Assert.Empty(_scanner.EnumerateMp3(_tmp.Root));
    }

    /// <summary>
    /// Maneja bien UTF-8.
    /// </summary>
    [Fact]
    public void EnumerateMp3_FindsFilesWithSpacesAndAccentsInTheName()
    {
        var path = Touch("Año Nuevo (copia)", "01 Canción — ñandú.mp3");

        var found = _scanner.EnumerateMp3(_tmp.Root).ToList();

        Assert.Equal(new[] { path }, found);
    }

    /// <summary>
    /// Excepción cuando no existe el directorio donde se escanea.
    /// </summary>
    [Fact]
    public void EnumerateMp3_MissingDirectory_ThrowsDirectoryNotFound()
    {
        var missing = _tmp.Combine("no_existe");

        Assert.Throws<DirectoryNotFoundException>(() => _scanner.EnumerateMp3(missing).ToList());
    }
}
