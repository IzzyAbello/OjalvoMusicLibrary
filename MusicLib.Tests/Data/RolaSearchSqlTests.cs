using MusicLib.Data;
using Xunit;

namespace MusicLib.Tests.Data;

/// <summary>
/// Pruebas del armado de SQL de la búsqueda.
/// </summary>
public sealed class RolaSearchSqlTests
{
    private const int Count = 50;

    // Arma la consulta y devuelve el SQL y los parámetros como diccionario.
    private static (string Sql, Dictionary<string, object> Params) Build
    (
        RolaSearch criteria
    )
    {
        var (sql, parameters) = RolaSearchSql.Build(criteria, Count);
        return (sql, parameters.ToDictionary(p => p.Name, p => p.Value));
    }

    // Cuántas veces aparece 'part' dentro de 'text'.
    private static int Occurrences(string text, string part) =>
        text.Split(part).Length - 1;

    /// <summary>
    /// Sin criterios: no hay WHERE y el único parámetro es $count.
    /// (FindFirst)
    /// </summary>
    [Fact]
    public void Build_EmptyCriteria_HasNoWhereAndOnlyCount()
    {
        var (sql, ps) = Build(new RolaSearch());

        Assert.DoesNotContain("WHERE", sql);
        Assert.Contains("LIMIT $count", sql);
        Assert.Equal(Count, Assert.Single(ps).Value);
    }

    /// <summary>
    /// Un campo de texto agrega su condición y su parámetro normalizado.
    /// </summary>
    [Fact]
    public void Build_TextField_AddsConditionAndNormalizedParameter()
    {
        var (sql, ps) = Build(new RolaSearch(Title: "  Canción "));

        Assert.Contains("WHERE (norm(r.title) LIKE $title ESCAPE '\\')", sql);
        Assert.Equal("%cancion%", ps["$title"]);
    }

    /// <summary>
    /// Texto vacío o solo espacios no filtra.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_BlankText_DoesNotFilter(string blank)
    {
        var (sql, ps) = Build(new RolaSearch(Any: blank, Genre: blank));

        Assert.DoesNotContain("WHERE", sql);
        Assert.Single(ps);
    }

    /// <summary>
    /// Los comodines de LIKE se buscan.
    /// </summary>
    [Theory]
    [InlineData("100%", "%100\\%%")]
    [InlineData("a_b", "%a\\_b%")]
    [InlineData("c\\d", "%c\\\\d%")]
    public void Build_LikeWildcards_AreEscaped(string text, string expected)
    {
        var (_, ps) = Build(new RolaSearch(Title: text));
        Assert.Equal(expected, ps["$title"]);
    }

    /// <summary>
    /// Any busca en 4 columnas unidas con OR, con un solo parámetro.
    /// </summary>
    [Fact]
    public void Build_Any_SearchesFourColumnsWithOneParameter()
    {
        var (sql, ps) = Build(new RolaSearch(Any: "rock"));

        Assert.Equal(4, Occurrences(sql, "LIKE $any"));
        Assert.Equal(3, Occurrences(sql, " OR "));
        foreach (var col in new[] { "r.title", "p.name", "a.name", "r.genre" })
            Assert.Contains($"norm({col}) LIKE $any", sql);

        Assert.Equal(2, ps.Count);   // $any y $count
        Assert.Equal("%rock%", ps["$any"]);
    }

    /// <summary>
    /// Los años generan >= y <= con sus valores.
    /// </summary>
    [Fact]
    public void Build_Years_GenerateRangeConditions()
    {
        var (sql, ps) = Build(new RolaSearch(YearFrom: 1990, YearTo: 2000));

        Assert.Contains("r.year >= $yearFrom", sql);
        Assert.Contains("r.year <= $yearTo", sql);
        Assert.Equal(1990, ps["$yearFrom"]);
        Assert.Equal(2000, ps["$yearTo"]);
    }

    /// <summary>
    /// Varios criterios se combinan con AND, uno por criterio.
    /// </summary>
    [Fact]
    public void Build_SeveralCriteria_AreJoinedWithAnd()
    {
        var (sql, ps) = Build
        (
            new RolaSearch
            (
                Title: "a",
                Performer: "b",
                Album: "c",
                Genre: "d",
                YearFrom: 1
            )
        );

        Assert.Equal(4, Occurrences(sql, " AND "));   // 5 condiciones
        Assert.Equal(6, ps.Count);                   // 5 + $count
    }

    /// <summary>
    /// El texto del usuario nunca entra al SQL.
    /// </summary>
    [Fact]
    public void Build_UserText_NeverAppearsInSql()
    {
        const string evil = "x'); DROP TABLE rolas; --";

        var (sql, ps) = Build(new RolaSearch(Any: evil, Title: evil));

        Assert.DoesNotContain("DROP", sql);
        Assert.DoesNotContain("x'", sql);
        Assert.Contains("drop table rolas", (string)ps["$title"]);
    }
}
