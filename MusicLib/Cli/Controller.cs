using MusicLib.Data;
using MusicLib.Logging;
using MusicLib.Mining;

namespace MusicLib.Cli;

/// <summary>
/// Orquesta la ejecución del programa: depura parámetros, mina si hace falta
/// y abre la vista si corresponde. Nunca deja escapar excepciones: todo se
/// reporta limpiamente y se traduce a un código de salida.
/// </summary>
public sealed class Controller
{
    /// <summary>
    /// Todo salió bien.
    /// </summary>
    public const int ExitOk = 0;

    /// <summary>
    /// Falló algo en tiempo de ejecución.
    /// </summary>
    public const int ExitError = 1;

    /// <summary>
    /// Parámetros inválidos.
    /// </summary>
    public const int ExitUsage = 2;

    /// <summary>
    /// El usuario canceló con Ctrl+C.
    /// </summary>
    public const int ExitCancelled = 130;

    internal const string HelpText =
        """
        ojalvomusic - Biblioteca musical

        Uso:
          ojalvomusic                   Abre la vista.
          ojalvomusic <ruta>            Mina <ruta> y luego abre la vista.
          ojalvomusic -m <ruta>         Solo mina <ruta> (sin abrir la vista).
          ojalvomusic [opciones]

        Opciones:
          -m, --miner, --minero, --minar <ruta>
                                        Mina el directorio indicado. La ruta
                                        es obligatoria; puede ser relativa al
                                        directorio actual o absoluta.
          -l, --log                     Muestra en consola el log del programa.
          -h, --help                    Muestra esta ayuda y sale.
        """;

    private readonly string[] _args;
    private readonly TextWriter _out;
    private readonly TextWriter _err;
    private readonly string _dbPath;
    private readonly Func<string, ILog, Miner> _minerFactory;
    private readonly Action _runView;
    private const int _progressFileCount = 3;

    // Descarta todo hasta que se depuran los parámetros; si trae -l, pasa a
    // ser un ConsoleLog.
    private ILog _log = NullLog.Instance;

    /// <summary>
    /// Constructor de producción: consola real, DB en la carpeta de datos del
    /// usuario, minero real.
    /// </summary>
    /// <param name="args">
    /// Argumentos tal como llegan a Main.
    /// </param>
    public Controller(string[] args)
        : this(args, Console.Out, Console.Error, DefaultDbPath()) { }

    /// <summary>
    /// Constructor con dependencias inyectables (para pruebas).
    /// </summary>
    /// <param name="args">Argumentos.</param>
    /// <param name="output">
    /// Salida normal (ayuda y, con -l, el log).
    /// </param>
    /// <param name="error">
    /// Salida de errores.
    /// </param>
    /// <param name="dbPath">Ruta del archivo de la BD.</param>
    /// <param name="minerFactory">
    /// Crea el minero dadas la ruta de la DB y el log. Null = minero real.
    /// </param>
    /// <param name="runView">
    /// Abre la vista. Null = todavía no implementada (lanza
    /// NotImplementedException).
    /// </param>
    internal Controller
    (
        string[] args,
        TextWriter output,
        TextWriter error,
        string dbPath,
        Func<string, ILog, Miner>? minerFactory = null,
        Action? runView = null
    )
    {
        _args = args;
        _out = output;
        _err = error;
        _dbPath = dbPath;
        _minerFactory = minerFactory
            ?? ((db, l) => new Miner(db, new FileScanner(l), new TagReader(l), l));
        _runView = runView
            ?? 
            (
                () => throw new NotImplementedException
                (
                    "La vista aún no está implementada :'("
                )
            );
    }

    /// <summary>
    /// Ejecuta el programa.
    /// </summary>
    /// <returns>
    /// Código de salida del proceso.
    /// </returns>
    public int Execute()
    {
        try
        {
            var options = ArgumentParser.Parse(_args);

            if (options.Help)
            {
                _out.WriteLine(HelpText);
                return ExitOk;
            }

            if (options.Log) _log = new ConsoleLog(_out);

            _log.Write($"Parámetros: {options}");

            if (options.MinePath is not null)
            {
                var result = Mine(options.MinePath);
                if (result.Cancelled) return ExitCancelled;
            }

            if (options.OpenView) _runView();

            return ExitOk;
        }
        catch (UsageException ex)
        {
            _err.WriteLine($"{ex.Message}\n Usa 'ojalvomusic --help' para ver las opciones.");
            return ExitUsage;
        }
        catch (Exception ex)
        {
            _err.WriteLine($"Error: {ex.Message}");
            _log.Write(ex.ToString());
            return ExitError;
        }
    }

    /// <summary>
    /// Convierte la ruta del usuario en un directorio absoluto que existe.
    /// </summary>
    /// <param name="raw">
    /// Ruta tal como la escribió el usuario.
    /// </param>
    /// <returns>
    /// Ruta absoluta de un directorio existente.
    /// </returns>
    /// <exception cref="UsageException">
    /// Si la ruta no existe o no es un directorio.
    /// </exception>
    internal static string ResolveDirectory(string raw)
    {
        string full;
        try { full = Path.GetFullPath(raw); }
        catch (Exception ex) when 
        (
            ex is ArgumentException or NotSupportedException or PathTooLongException
        )
        {
            throw new UsageException($"La ruta '{raw}' no es válida.");
        }

        if (Directory.Exists(full)) return full;
        else throw new UsageException
        (
            File.Exists(full) 
            ? $"'{raw}' es un archivo, no un directorio."
            : $"La ruta '{raw}' no existe."
        );
    }

    internal static string DefaultDbPath()
    {
        var baseDir = Environment.GetFolderPath
        (
            Environment.SpecialFolder.LocalApplicationData
        );
        if (string.IsNullOrEmpty(baseDir)) baseDir = AppContext.BaseDirectory;

        return Path.Combine(baseDir, "OjalvoMusic", "library.db");
    }

    private MiningResult Mine(string rawPath)
    {
        var root = ResolveDirectory(rawPath);

        DatabaseInitializer.EnsureCreated(_dbPath, _log);
        var miner = _minerFactory(_dbPath, _log);

        _log.Write($"Minando '{root}'...");

        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        Console.CancelKeyPress += onCancel;

        try
        {
            var result = miner
                .MineAsync(root, new InlineProgress(ReportProgress), cts.Token)
                .GetAwaiter()
                .GetResult();

            PrintSummary(result);
            return result;
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
        }
    }

    private void ReportProgress(MiningProgress p)
    {
        if (p.Processed != p.Total && p.Processed % _progressFileCount != 0) return;

        var phase = p.Phase == MiningPhase.Reading ? "Leyendo" : "Guardando";
        _log.Write($"  [{phase}] {p.Processed}/{p.Total}");
    }

    private void PrintSummary(MiningResult r)
    {
        _log.Write(r.Cancelled ? "Minado cancelado." : "Minado terminado.");
        _log.Write($"  Encontradas: {r.Found}");
        _log.Write($"  Insertadas:  {r.Inserted}");
        _log.Write($"  Omitidas:    {r.Skipped}");
        _log.Write($"  Fallidas:    {r.Failed}");

        foreach (var e in r.Errors) _log.Write($"  ! {e}");
    }

    /// <summary>
    /// IProgress síncrono: Progress&lt;T&gt; publica en otro hilo y el resumen
    /// final podría imprimirse antes que los últimos avances.
    /// </summary>
    private sealed class InlineProgress : IProgress<MiningProgress>
    {
        private readonly Action<MiningProgress> _handler;

        public InlineProgress(Action<MiningProgress> handler) =>
            _handler = handler;

        public void Report(MiningProgress value) => _handler(value);
    }
}