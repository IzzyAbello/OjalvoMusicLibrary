using MusicLib.Logging;

namespace MusicLib.Tests.Logging;

/// <summary>
/// Pruebas de ConsoleLog y NullLog.
/// </summary>
public sealed class ConsoleLogTests
{
    /// <summary>
    /// Cada mensaje sale en su propia línea con el prefijo "[Log] ".
    /// </summary>
    [Fact]
    public void ConsoleLog_WritesOneLinePerMessage_WithPrefix()
    {
        var output = new StringWriter();
        var log = new ConsoleLog(output);

        log.Write("uno");
        log.Write("dos");

        Assert.Equal
        (
            "[Log] uno" + Environment.NewLine + "[Log] dos" + Environment.NewLine,
            output.ToString()
        );
    }

    /// <summary>
    /// Si la consola falla (pipe roto), el log no lanza excepción.
    /// </summary>
    [Fact]
    public void ConsoleLog_WhenWriterFails_DoesNotThrow()
    {
        var log = new ConsoleLog(new BrokenWriter());

        var ex = Record.Exception(() => log.Write("hola"));

        Assert.Null(ex);
    }

    /// <summary>
    /// NullLog descarta todo sin fallar.
    /// </summary>
    [Fact]
    public void NullLog_DiscardsEverything()
    {
        var ex = Record.Exception(() => NullLog.Instance.Write("fidel casto"));

        Assert.Null(ex);
    }

    private sealed class BrokenWriter : StringWriter
    {
        public override void WriteLine(string? value) =>
            throw new IOException("pipe roto");
    }
}