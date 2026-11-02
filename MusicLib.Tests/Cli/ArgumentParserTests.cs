using MusicLib.Cli;

namespace MusicLib.Tests.Cli;

/// <summary>
/// Pruebas del depurador de parámetros.
/// </summary>
public sealed class ArgumentParserTests
{
    /// <summary>
    /// Sin parámetros solo se abre la vista.
    /// </summary>
    [Fact]
    public void NoArgs_OpensViewOnly()
    {
        var o = ArgumentParser.Parse(Array.Empty<string>());

        Assert.Equal(new Options(false, false, null, true), o);
    }

    /// <summary>
    /// Todas las formas de pedir ayuda.
    /// </summary>
    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    public void Help_IsRecognized(string flag)
    {
        Assert.True(ArgumentParser.Parse(new[] { flag }).Help);
    }

    /// <summary>
    /// La ayuda gana aunque haya otros parámetros inválidos.
    /// </summary>
    [Theory]
    [InlineData("--basura", "-h")]
    [InlineData("-h", "--basura")]
    [InlineData("-m", "-h")]
    [InlineData("uno", "dos", "--help")]
    public void Help_WinsOverEverythingElse(params string[] args)
    {
        Assert.True(ArgumentParser.Parse(args).Help);
    }

    /// <summary>
    /// Todas las formas de activar el log.
    /// </summary>
    [Theory]
    [InlineData("-l")]
    [InlineData("--log")]
    public void Log_IsRecognized_AndStillOpensView(string flag)
    {
        var o = ArgumentParser.Parse(new[] { flag });

        Assert.Equal(new Options(false, true, null, true), o);
    }

    /// <summary>
    /// Todas las formas de pedir el minado: mina y NO abre la vista.
    /// </summary>
    [Theory]
    [InlineData("-m")]
    [InlineData("--miner")]
    [InlineData("--minero")]
    [InlineData("--minar")]
    public void Mine_TakesPath_AndDoesNotOpenView(string flag)
    {
        var o = ArgumentParser.Parse(new[] { flag, "musica" });

        Assert.Equal(new Options(false, false, "musica", false), o);
    }

    /// <summary>
    /// Una ruta suelta mina y luego abre la vista.
    /// </summary>
    [Fact]
    public void BarePath_MinesAndOpensView()
    {
        var o = ArgumentParser.Parse(new[] { "musica" });

        Assert.Equal(new Options(false, false, "musica", true), o);
    }

    /// <summary>
    /// Las rutas con espacios llegan intactas (el shell ya las agrupó).
    /// </summary>
    [Fact]
    public void Path_WithSpaces_IsKeptIntact()
    {
        var o = ArgumentParser.Parse(new[] { "-m", "Mi música/rock" });

        Assert.Equal("Mi música/rock", o.MinePath);
    }

    /// <summary>
    /// El log se combina con minar, en cualquier orden.
    /// </summary>
    [Theory]
    [InlineData("-l", "-m", "musica")]
    [InlineData("-m", "musica", "-l")]
    [InlineData("--log", "--minar", "musica")]
    public void Log_CombinesWithMine_InAnyOrder(params string[] args)
    {
        var o = ArgumentParser.Parse(args);

        Assert.Equal(new Options(false, true, "musica", false), o);
    }

    /// <summary>
    /// El log se combina con la ruta suelta, en cualquier orden.
    /// </summary>
    [Theory]
    [InlineData("-l", "musica")]
    [InlineData("musica", "-l")]
    public void Log_CombinesWithBarePath_InAnyOrder(params string[] args)
    {
        var o = ArgumentParser.Parse(args);

        Assert.Equal(new Options(false, true, "musica", true), o);
    }

    /// <summary>
    /// Repetir -l o -h no es un error.
    /// </summary>
    [Fact]
    public void RepeatedLog_IsHarmless()
    {
        Assert.True(ArgumentParser.Parse(new[] { "-l", "--log" }).Log);
    }

    /// <summary>
    /// -m sin ruta (al final, o seguido de otra opción) es inválido.
    /// </summary>
    [Theory]
    [InlineData("-m")]
    [InlineData("--minar")]
    [InlineData("-m", "-l")]
    [InlineData("-l", "-m")]
    public void Mine_WithoutPath_Throws(params string[] args)
    {
        Assert.Throws<UsageException>(() => ArgumentParser.Parse(args));
    }

    /// <summary>
    /// Una opción que no existe es inválida.
    /// </summary>
    [Theory]
    [InlineData("--basura")]
    [InlineData("-x")]
    [InlineData("-M")]
    [InlineData("-")]
    [InlineData("musica", "--basura")]
    public void UnknownOption_Throws(params string[] args)
    {
        Assert.Throws<UsageException>(() => ArgumentParser.Parse(args));
    }

    /// <summary>
    /// Dos rutas sueltas son ambiguas.
    /// </summary>
    [Fact]
    public void TwoBarePaths_Throws()
    {
        Assert.Throws<UsageException>
        (
            () => ArgumentParser.Parse(new[] { "uno", "dos" })
        );
    }

    /// <summary>
    /// Una ruta suelta junto con -m es ambiguo.
    /// </summary>
    [Theory]
    [InlineData("-m", "uno", "dos")]
    [InlineData("dos", "-m", "uno")]
    public void BarePath_PlusMine_Throws(params string[] args)
    {
        Assert.Throws<UsageException>(() => ArgumentParser.Parse(args));
    }

    /// <summary>
    /// -m solo puede aparecer una vez.
    /// </summary>
    [Fact]
    public void Mine_Twice_Throws()
    {
        Assert.Throws<UsageException>
        (
            () => ArgumentParser.Parse(new[] { "-m", "a", "--minar", "b" })
        );
    }

    /// <summary>
    /// Una ruta vacía o solo espacios es inválida.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyPath_Throws(string path)
    {
        Assert.Throws<UsageException>
        (
            () => ArgumentParser.Parse(new[] { "-m", path })
        );
        Assert.Throws<UsageException>
        (
            () => ArgumentParser.Parse(new[] { path })
        );
    }
}