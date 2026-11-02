using System.Collections.Concurrent;
using MusicLib.Mining;

namespace MusicLib.Tests.Support;

/// <summary>
/// Escáner falso: devuelve la lista de rutas que se le configure, sin tocar disco.
/// </summary>
public sealed class FakeFileScanner : IFileScanner
{
    /// <summary>
    /// Rutas que "existen".
    /// </summary>
    public List<string> Files { get; } = new();

    /// <summary>
    /// Si se asigna, EnumerateMp3 lanza esta excepción.
    /// </summary>
    public Exception? Failure { get; set; }

    /// <summary>
    /// Devuelve una copia de la lista original (fake) de files
    /// para simular el EnumerateMp3 de IFileScanner.
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    public IEnumerable<string> EnumerateMp3(string root)
    {
        if (Failure != null) throw Failure;
        return Files.ToList();
    }
}

/// <summary>
/// Lector de etiquetas falso: devuelve el TagInfo configurado por ruta, puede
/// fallar para rutas concretas y registra cada archivo que "abrió".
/// </summary>
public sealed class FakeTagReader : ITagReader
{
    private readonly ConcurrentDictionary<string, TagInfo> _tags = new();
    private readonly ConcurrentDictionary<string, Exception> _failures = new();
    private readonly ConcurrentQueue<string> _reads = new();

    /// <summary>
    /// Se invoca en cada lectura, antes de devolver (sirve para bloquear).
    /// </summary>
    public Action<string>? OnRead { get; set; }

    /// <summary>
    /// Rutas leídas, en orden.
    /// </summary>
    public IReadOnlyList<string> Reads => _reads.ToArray();

    /// <summary>
    /// Define (o redefine) las etiquetas de un archivo.
    /// </summary>
    public void Set(string path, TagInfo tag) => _tags[path] = tag;

    /// <summary>
    /// Hace que leer ese archivo lance la excepción dada.
    /// </summary>
    public void Fail(string path, Exception ex) => _failures[path] = ex;

    /// <summary>
    /// Deja de fallar para ese archivo.
    /// </summary>
    public void Repair(string path) => _failures.TryRemove(path, out _);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public TagInfo Read(string path)
    {
        _reads.Enqueue(path);
        OnRead?.Invoke(path);

        if (_failures.TryGetValue(path, out var ex)) throw ex;

        return _tags.TryGetValue(path, out var tag)
            ? tag
            : throw new FileNotFoundException
            ($"Sin etiquetas falsas para '{path}'.");
    }
}

/// <summary>
/// IProgress síncrono. Progress&lt;T&gt; publica en otro hilo y haría las pruebas
/// no deterministas. Este ejecuta el handler en el mismo hilo que reporta.
/// </summary>
/// <typeparam name="T">
/// Modelo para mostrar el progreso.
/// </typeparam>
public sealed class SyncProgress<T> : IProgress<T>
{
    private readonly Action<T>? _handler;
    private readonly object _lock = new();
    private readonly List<T> _reports = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="handler">
    /// Acción opcional que se ejecuta cuando se informe un progreso.
    /// </param>
    public SyncProgress(Action<T>? handler = null) => _handler = handler;

    /// <summary>
    /// Todo lo reportado, en orden.
    /// </summary>
    public IReadOnlyList<T> Reports
    {
        get { lock (_lock) return _reports.ToList(); }
    }

    /// <summary>
    /// Contrato de IProgress
    /// </summary>
    /// <param name="value">
    /// Modelo para mostrar el progreso.
    /// </param>
    public void Report(T value)
    {
        lock (_lock) _reports.Add(value);
        _handler?.Invoke(value);
    }
}
