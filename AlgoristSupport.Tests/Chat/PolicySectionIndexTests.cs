using AlgoristSupport.Chat;

namespace AlgoristSupport.Tests.Chat;

public class PolicySectionIndexTests
{
    private static PolicySectionIndex Build (string markdown) => PolicySectionIndex.Build(new PolicyDocument(markdown));

    [Fact]
    public void Parses_numbered_level_two_headings ()
    {
        var index = Build("# Título\n\n## 12. Garantia\n\nTexto.\n\n## 13. Suporte e níveis de serviço (SLA)\n");

        Assert.Equal(2, index.Count);
        Assert.True(index.TryGet(12, out var section));
        Assert.Equal(new PolicySection(12, "Garantia", "secao-12"), section);
        Assert.True(index.TryGet(13, out var sla));
        Assert.Equal("Suporte e níveis de serviço (SLA)", sla.Title);
    }

    [Fact]
    public void Handles_windows_line_endings ()
    {
        var index = Build("## 1. Objetivo e escopo\r\n\r\nTexto.\r\n");

        Assert.True(index.TryGet(1, out var section));
        Assert.Equal("Objetivo e escopo", section.Title);
    }

    [Theory]
    [InlineData("# 1. Título de nível 1")]
    [InlineData("### 2. Subtítulo")]
    [InlineData("## Sem número")]
    [InlineData("## 3 Sem ponto")]
    [InlineData("Texto com ## 4. no meio")]
    [InlineData("4.4. Cláusula")]
    public void Ignores_non_section_lines (string line) =>
        Assert.Equal(0, Build(line).Count);

    [Fact]
    public void Anchor_format_is_numeric ()
    {
        Assert.Equal("secao-12", PolicySectionIndex.AnchorFor(12));
    }

    [Fact]
    public void Empty_index_resolves_nothing ()
    {
        Assert.Equal(0, PolicySectionIndex.Empty.Count);
        Assert.False(PolicySectionIndex.Empty.TryGet(1, out _));
    }

    [Fact]
    public void From_a_failed_load_is_empty ()
    {
        Assert.Same(PolicySectionIndex.Empty, PolicySectionIndex.From(TestChat.MissingPolicy()));
    }

    [Fact]
    public void Shipped_policy_has_21_sections ()
    {
        var index = PolicySectionIndex.From(PolicyDocumentLoader.Load(PolicyDocumentLoader.DefaultPath));

        Assert.Equal(21, index.Count);
        Assert.Equal(Enumerable.Range(1, 21), index.Sections.Select(s => s.Number));
        Assert.True(index.TryGet(12, out var warranty));
        Assert.Equal("Garantia", warranty.Title);
        Assert.True(index.TryGet(13, out var support));
        Assert.StartsWith("Suporte", support.Title);
    }
}
