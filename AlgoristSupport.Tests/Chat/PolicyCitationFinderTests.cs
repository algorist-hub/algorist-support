using AlgoristSupport.Chat;

namespace AlgoristSupport.Tests.Chat;

public class PolicyCitationFinderTests
{
    private static (string Text, int Section)[] Find (string text) =>
        PolicyCitationFinder.Find(text).Select(c => (text.Substring(c.Start, c.Length), c.SectionNumber)).ToArray();

    [Theory]
    [InlineData("A garantia é de 90 dias (Seção 12).", "Seção 12", 12)]
    [InlineData("veja a seção 12", "seção 12", 12)]
    [InlineData("SEÇÃO 12", "SEÇÃO 12", 12)]
    [InlineData("conforme a Secao 12", "Secao 12", 12)]
    [InlineData("(Seção 9).", "Seção 9", 9)]
    [InlineData("Seção 12.1 trata disso", "Seção 12.1", 12)]
    [InlineData("custos de nuvem (Seção 8.6)", "Seção 8.6", 8)]
    public void Finds_a_single_citation (string text, string expectedText, int expectedSection) =>
        Assert.Equal([(expectedText, expectedSection)], Find(text));

    [Fact]
    public void Plural_yields_one_citation_per_number ()
    {
        Assert.Equal([("12", 12), ("13", 13)], Find("Veja as Seções 12 e 13."));
        Assert.Equal([("8", 8), ("12", 12), ("13", 13)], Find("Seções 8, 12 e 13"));
        Assert.Equal([("12.1", 12), ("13.2", 13)], Find("secoes 12.1 e 13.2"));
    }

    [Fact]
    public void Several_citations_are_returned_in_order ()
    {
        Assert.Equal([("Seção 12", 12), ("Seção 13", 13)], Find("Garantia (Seção 12) e SLA (Seção 13)."));
    }

    [Theory]
    [InlineData("")]
    [InlineData("São 12 dias")]
    [InlineData("Seção sem número")]
    [InlineData("Seçãoxyz 12")]
    [InlineData("Interseção 12")]
    [InlineData("Seção 1234")]
    [InlineData("Seção 12abc")]
    public void Ignores_non_citations (string text) =>
        Assert.Empty(Find(text));
}
