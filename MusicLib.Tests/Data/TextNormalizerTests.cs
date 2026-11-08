using Microsoft.Data.Sqlite;
using MusicLib.Data;
using Xunit;

namespace MusicLib.Tests.Data;

/// <summary>
/// Pruebas de TextNormalizer: quitar acentos y pasar a minúsculas.
/// </summary>
public sealed class TextNormalizerTests
{
    // =====================================================================
    //  Normalize
    // =====================================================================

    /// <summary>
    /// Quita acentos, tildes y diéresis.
    /// </summary>
    [Theory]
    [InlineData("Canción", "cancion")]
    [InlineData("Ñandú", "nandu")]
    [InlineData("Pingüino", "pinguino")]
    [InlineData("Último año", "ultimo ano")]
    public void Normalize_RemovesDiacritics(string input, string expected) =>
        Assert.Equal(expected, TextNormalizer.Normalize(input));

    /// <summary>
    /// Pasa a minúsculas.
    /// </summary>
    [Fact]
    public void Normalize_LowersCase() =>
        Assert.Equal("rock", TextNormalizer.Normalize("ROCK"));

    /// <summary>
    /// El texto sin acentos (incluido el no latino) queda igual.
    /// </summary>
    [Theory]
    [InlineData("burzum")]
    [InlineData("日本語")]
    [InlineData("¿si? ¡no! 100%")]
    public void Normalize_TextWithoutDiacritics_IsUnchanged(string input) =>
        Assert.Equal(input, TextNormalizer.Normalize(input));

    /// <summary>
    /// Texto vacío da texto vacío.
    /// </summary>
    [Fact]
    public void Normalize_Empty_ReturnsEmpty() =>
        Assert.Equal("", TextNormalizer.Normalize(""));

    // =====================================================================
    //  Register: norm() dentro de SQLite
    // =====================================================================

    private static object? SelectWithNorm(string expr)
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        TextNormalizer.Register(conn);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT {expr}";
        return cmd.ExecuteScalar();
    }

    /// <summary>
    /// Una vez registrada, SQLite puede llamar norm() y normaliza igual
    /// que Normalize.
    /// </summary>
    [Fact]
    public void Register_NormIsAvailableInSql() =>
        Assert.Equal("cancion", SelectWithNorm("norm('Canción')"));

    /// <summary>
    /// norm(NULL) devuelve NULL (así NULL nunca coincide en un LIKE).
    /// </summary>
    [Fact]
    public void Register_NormOfNull_IsNull() =>
        Assert.Equal(DBNull.Value, SelectWithNorm("norm(NULL)"));
}
