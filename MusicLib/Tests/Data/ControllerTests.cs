using MusicLib.Cli;
using MusicLib.Mining;
using MusicLib.Tests.Support;

namespace MusicLib.Tests.Cli;

/// <summary>
/// Pruebas del Controller con mocking de consola, minero con escáner/lector falsos
/// y una vista falsa, sobre carpetas temporales.
/// </summary>
public sealed class ControllerTests : IDisposable
{
    private readonly TempDir _tmp = new();
    private readonly StringWriter _out = new();
    private readonly StringWriter _err = new();
    private readonly FakeFileScanner _scanner = new();
    private readonly FakeTagReader _reader = new();
    private int _minersCreated;
    private int _viewCalls;

    /// <summary>
    /// Elimina los datos temporales.
    /// </summary>
    public void Dispose() => _tmp.Dispose();

    private Controller Make(params string[] args) =>
        new
        (
            args,
            _out,
            _err,
            _tmp.Combine("db", "lib.db"),
            db =>
            {
                _minersCreated++;
                return new Miner(db, _scanner, _reader);
            },
            () => _viewCalls++
        );

    private string AddMp3(string name)
    {
        var path = _tmp.Combine(name);
        _scanner.Files.Add(path);
        _reader.Set
        (
            path,
            new TagInfo("Titulo", "Interprete", null, "Album", 1, 2000, "Rock")
        );
        return path;
    }

    /// <summary>
    /// --help imprime la ayuda, sale bien y no hace nada más.
    /// </summary>
    [Fact]
    public void Help_PrintsHelp_AndDoesNothingElse()
    {
        var code = Make("--help").Execute();

        Assert.Equal(Controller.ExitOk, code);
        Assert.Contains("Uso:", _out.ToString());
        Assert.Equal(0, _minersCreated);
        Assert.Equal(0, _viewCalls);
    }

    /// <summary>
    /// Parámetros inválidos: error limpio, código 2, nada se ejecuta.
    /// </summary>
    [Fact]
    public void InvalidArgs_ReportsCleanly_AndExitsWithUsageCode()
    {
        var code = Make("--basura").Execute();

        Assert.Equal(Controller.ExitUsage, code);
        Assert.Contains("Error de USO:", _err.ToString());
        Assert.Contains("--basura", _err.ToString());
        Assert.Equal(0, _minersCreated);
        Assert.Equal(0, _viewCalls);
    }

    /// <summary>
    /// Minar una ruta que no existe es un error de uso y no crea el minero.
    /// </summary>
    [Fact]
    public void Mine_NonexistentPath_IsUsageError()
    {
        var missing = _tmp.Combine("no_existe");

        var code = Make("-m", missing).Execute();

        Assert.Equal(Controller.ExitUsage, code);
        Assert.Contains("no existe", _err.ToString());
        Assert.Equal(0, _minersCreated);
    }

    /// <summary>
    /// -m mina y NO abre la vista.
    /// </summary>
    [Fact]
    public void Mine_ExistingPath_Mines_AndSkipsView()
    {
        AddMp3("a.mp3");

        var code = Make("-m", _tmp.Root).Execute();

        Assert.Equal(Controller.ExitOk, code);
        Assert.Equal(1, _minersCreated);
        Assert.Contains("Insertadas:  1", _out.ToString());
        Assert.Equal(0, _viewCalls);
    }

    /// <summary>
    /// Una ruta suelta mina y luego abre la vista.
    /// </summary>
    [Fact]
    public void BarePath_Mines_ThenOpensView()
    {
        AddMp3("a.mp3");

        var code = Make(_tmp.Root).Execute();

        Assert.Equal(Controller.ExitOk, code);
        Assert.Equal(1, _minersCreated);
        Assert.Equal(1, _viewCalls);
    }

    /// <summary>
    /// Sin parámetros solo se abre la vista, sin minar.
    /// </summary>
    [Fact]
    public void NoArgs_OpensView_WithoutMining()
    {
        var code = Make().Execute();

        Assert.Equal(Controller.ExitOk, code);
        Assert.Equal(0, _minersCreated);
        Assert.Equal(1, _viewCalls);
    }

    /// <summary>
    /// Mientras la vista no exista, abrirla se reporta limpio (sin trazas).
    /// </summary>
    [Fact]
    public void DefaultView_IsNotImplemented_ReportedCleanly()
    {
        var controller = new Controller
        (
            Array.Empty<string>(),
            _out,
            _err,
            _tmp.Combine("lib.db")
        );

        var code = controller.Execute();

        Assert.Equal(Controller.ExitError, code);
        Assert.Contains("no está implementada", _err.ToString());
    }

    /// <summary>
    /// Un fallo inesperado del minero se reporta limpio y no abre la vista.
    /// </summary>
    [Fact]
    public void MinerFailure_IsReported_AndViewIsSkipped()
    {
        _scanner.Failure = new IOException("disco roto");

        var code = Make(_tmp.Root).Execute();

        Assert.Equal(Controller.ExitError, code);
        Assert.Contains("disco roto", _err.ToString());
        Assert.Equal(0, _viewCalls);
    }

    /// <summary>
    /// Un archivo con etiquetas ilegibles no tumba la corrida: se lista como
    /// error y el resto se inserta.
    /// </summary>
    [Fact]
    public void UnreadableFile_IsListed_AndRunContinues()
    {
        AddMp3("buena.mp3");
        var bad = AddMp3("mala.mp3");
        _reader.Fail(bad, new InvalidDataException("etiqueta corrupta"));

        var code = Make("-m", _tmp.Root).Execute();

        Assert.Equal(Controller.ExitOk, code);
        Assert.Contains("Insertadas:  1", _out.ToString());
        Assert.Contains("Fallidas:    1", _out.ToString());
        Assert.Contains("etiqueta corrupta", _err.ToString());
    }

    /// <summary>
    /// Una ruta absoluta existente se devuelve normalizada.
    /// </summary>
    [Fact]
    public void ResolveDirectory_Absolute_ReturnsFullPath()
    {
        Assert.Equal
        (
            Path.GetFullPath(_tmp.Root),
            Controller.ResolveDirectory(_tmp.Root)
        );
    }

    /// <summary>
    /// Una ruta relativa se resuelve contra el directorio actual.
    /// </summary>
    [Fact]
    public void ResolveDirectory_Relative_ResolvesAgainstCurrentDirectory()
    {
        var relative = Path.GetRelativePath
        (
            Environment.CurrentDirectory,
            _tmp.Root
        );

        Assert.Equal
        (
            Path.GetFullPath(_tmp.Root),
            Controller.ResolveDirectory(relative)
        );
    }

    /// <summary>
    /// Un archivo no es un directorio donde minar.
    /// </summary>
    [Fact]
    public void ResolveDirectory_File_Throws()
    {
        var file = _tmp.Combine("cancion.mp3");
        File.WriteAllBytes(file, Array.Empty<byte>());

        var ex = Assert.Throws<UsageException>
        (
            () => Controller.ResolveDirectory(file)
        );
        Assert.Contains("archivo", ex.Message);
    }

    /// <summary>
    /// Una ruta que no existe lanza error de uso.
    /// </summary>
    [Fact]
    public void ResolveDirectory_Missing_Throws()
    {
        Assert.Throws<UsageException>
        (
            () => Controller.ResolveDirectory(_tmp.Combine("nada"))
        );
    }
}