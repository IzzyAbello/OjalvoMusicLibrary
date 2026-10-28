namespace MusicLib.Cli;

/// <summary>
/// Depura los parámetros de la línea de comandos.
///
/// Reglas:
/// - -h / --help gana sobre todo lo demás, aunque haya otros errores.
/// - -m / --miner / --minero / --minar exige una ruta justo después; solo
///   mina (OpenView = false).
/// - Una ruta suelta (sin -m) mina y luego abre la vista.
/// - -l / --log activa el log y se combina con cualquier otra cosa.
/// - Sin parámetros: solo abre la vista.
/// </summary>
public static class ArgumentParser
{
    /// <summary>
    /// Interpreta los argumentos.
    /// </summary>
    /// <param name="args">
    /// Argumentos tal como llegan a Main.
    /// </param>
    /// <returns>
    /// Opciones ya depuradas.
    /// </returns>
    /// <exception cref="UsageException">
    /// Si los argumentos son inválidos.
    /// </exception>
    public static Options Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Any(a => a is "-h" or "--help"))
            return new Options(true, false, null, false);

        bool log = false, mine = false;
        string? minePath = null, barePath = null;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg is "-l" or "--log") log = true;
            else if (arg is "-m" or "--miner" or "--minero" or "--minar")
            {
                if (mine)
                    throw new UsageException
                    (
                        "La opción de minado solo puede indicarse una vez."
                    );

                if (i + 1 >= args.Length || IsOption(args[i + 1]))
                    throw new UsageException
                    (
                        $"'{arg}' requiere la ruta donde minar."
                    );

                minePath = RequirePath(args[++i]);
                mine = true;
            }
            else if (IsOption(arg))
                throw new UsageException($"Opción desconocida: '{arg}'.");
            else
            {
                if (barePath is not null)
                    throw new UsageException
                    (
                        "Solo se admite una ruta. " +
                        $"Se recibió también '{arg}'."
                    );

                barePath = RequirePath(arg);
            }
        }

        if (mine && barePath is not null)
            throw new UsageException
            (
                "No se pueden tener dos rutas de minado."
            );

        return mine
            ? new Options(false, log, minePath, false)
            : new Options(false, log, barePath, true);
    }

    private static bool IsOption(string s) => s.StartsWith('-');

    private static string RequirePath(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? throw new UsageException("La ruta no puede estar vacía.")
            : s;
}