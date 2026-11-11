using MusicLib.Data;
using Xunit;

namespace MusicLib.Tests.Data;

/// <summary>
/// Pruebas de las reglas de fechas.
/// </summary>
public sealed class DateTextTests
{
    /// <summary>
    /// Vacío (es opcional) o una fecha real AAAA-MM-DD son válidos.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1973-02-11")]
    [InlineData(" 2000-01-01 ")]
    [InlineData("2024-02-29")]   // año bisiesto
    public void IsValid_EmptyOrRealDate_IsTrue(string? text) =>
        Assert.True(DateText.IsValid(text));

    /// <summary>
    /// Otro formato o una fecha que no existe no son válidos.
    /// </summary>
    [Theory]
    [InlineData("2024-02-30")]
    [InlineData("2023-02-29")]   // no bisiesto
    [InlineData("1973-13-01")]
    [InlineData("1973-2-11")]
    [InlineData("11-02-1973")]
    [InlineData("1973/02/11")]
    [InlineData("1973")]
    [InlineData("abc")]
    public void IsValid_WrongFormatOrImpossibleDate_IsFalse(string text) =>
        Assert.False(DateText.IsValid(text));

    /// <summary>
    /// El fin después del inicio, o el mismo día, está en orden.
    /// </summary>
    [Theory]
    [InlineData("1973-02-11", "2000-01-01")]
    [InlineData("1973-02-11", "1973-02-11")]
    public void IsInOrder_EndNotBeforeStart_IsTrue(string start, string end) =>
        Assert.True(DateText.IsInOrder(start, end));

    /// <summary>
    /// El fin antes del inicio no está en orden.
    /// </summary>
    [Fact]
    public void IsInOrder_EndBeforeStart_IsFalse() =>
        Assert.False(DateText.IsInOrder("2000-01-01", "1999-12-31"));

    /// <summary>
    /// Si falta una fecha o no es válida no hay orden que revisar.
    /// </summary>
    [Theory]
    [InlineData(null, "2000-01-01")]
    [InlineData("2000-01-01", "")]
    [InlineData("abc", "1999-01-01")]
    public void IsInOrder_MissingOrInvalidDate_IsTrue(string? start, string? end) =>
        Assert.True(DateText.IsInOrder(start, end));

    /// <summary>
    /// Normalize recorta los espacios y convierte lo vacío en null.
    /// </summary>
    [Theory]
    [InlineData(" 1973-02-11 ", "1973-02-11")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void Normalize_TrimsAndTurnsBlankIntoNull
    (
        string? text,
        string? expected
    ) => Assert.Equal(expected, DateText.Normalize(text));
}
