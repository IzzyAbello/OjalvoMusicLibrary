using Microsoft.Data.Sqlite;

namespace MusicLib.Tests.Support;

/// <summary>
/// Directorio temporal único por prueba. Al desecharse libera los pools de
/// conexiones de SQLite y borra todo.
/// </summary>
public sealed class TempDir : IDisposable
{
    /// <summary>
    /// Ruta absoluta del directorio.
    /// </summary>
    public string Root { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    public TempDir()
    {
        Root = Path.Combine
        (
            Path.GetTempPath(), "MusicLibTests_" + Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(Root);
    }

    /// <summary>
    /// Combina el directorio raíz con las partes dadas.
    /// </summary>
    public string Combine(params string[] parts) =>
        Path.Combine(new[] { Root }.Concat(parts).ToArray());

    /// <summary>
    /// Copia recursivamente un directorio dentro del temporal.
    /// </summary>
    /// <returns>
    /// Ruta del directorio copiado.
    /// </returns>
    public string CopyDirectory(string source, string name)
    {
        var target = Combine(name);
        CopyRecursive(source, target);
        return target;
    }

    /// <summary>
    /// Copia recursiva de un directorio.
    /// </summary>
    public static void CopyRecursive(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var file in Directory.GetFiles(source))
            File.Copy
            (
                file,
                Path.Combine(target, Path.GetFileName(file)),
                true
            );

        foreach (var dir in Directory.GetDirectories(source))
            CopyRecursive
            (
                dir,
                Path.Combine(target, Path.GetFileName(dir))
            );
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            foreach
            (
                var f in Directory.EnumerateFiles
                (
                    Root,
                    "*",
                    SearchOption.AllDirectories
                )
            )
                File.SetAttributes(f, FileAttributes.Normal);
            Directory.Delete(Root, true);
        }
        catch
        {
            // Limpieza de mejor esfuerzo: nunca debe tumbar una prueba.
        }
    }
}
