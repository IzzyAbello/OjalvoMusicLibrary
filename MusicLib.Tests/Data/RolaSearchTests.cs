using MusicLib.Data;
using Xunit;

namespace MusicLib.Tests.Data;

/// <summary>
/// Pruebas de RolaSearch.FromFields (campos de la búsqueda avanzada).
/// </summary>
public sealed class RolaSearchTests
{
    /// <summary>
    /// Todo vacío: búsqueda sin criterios.
    /// </summary>
    [Fact]
    public void FromFields_AllEmpty_IsEmptySearch() =>
        Assert.Equal
        (
            new RolaSearch(),
            RolaSearch.FromFields(null, "", "   ", null, null, null)
        );

    /// <summary>
    /// Los textos se recortan y van a su campo; Any queda vacío.
    /// </summary>
    [Fact]
    public void FromFields_Texts_AreTrimmedIntoTheirFields() =>
        Assert.Equal
        (
            new RolaSearch
            (
                Title: "t", Performer: "p", Album: "a", Genre: "g"
            ),
            RolaSearch.FromFields(" t ", "p ", " a", "g", null, null)
        );

    /// <summary>
    /// Los años pasan tal cual; un rango de un solo año es válido.
    /// </summary>
    [Theory]
    [InlineData(1990, 2000)]
    [InlineData(1990, 1990)]
    [InlineData(1990, null)]
    [InlineData(null, 2000)]
    public void FromFields_ValidYears_ArePassedThrough(int? from, int? to)
    {
        var search = RolaSearch.FromFields(null, null, null, null, from, to);

        Assert.Equal(from, search.YearFrom);
        Assert.Equal(to, search.YearTo);
    }

    /// <summary>
    /// "Desde" mayor que "hasta" se rechaza con un mensaje.
    /// </summary>
    [Fact]
    public void FromFields_FromGreaterThanTo_Throws()
    {
        var ex = Assert.Throws<ArgumentException>
        (
            () => RolaSearch.FromFields(null, null, null, null, 2000, 1990)
        );
        Assert.Contains("desde", ex.Message);
    }
}
