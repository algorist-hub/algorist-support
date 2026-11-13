using AlgoristSupport.Components.Shared;
using AlgoristSupport.Tests.Chat;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace AlgoristSupport.Tests.Components;

public class SafeMarkdownCitationTests
{
    private static IElement Render (string markdown)
    {
        var html = SafeMarkdown.ToHtml(markdown, citations: TestChat.Sections()).Value;
        return new HtmlParser().ParseDocument($"<div id=\"root\">{html}</div>").GetElementById("root")!;
    }

    private static IElement[] Citations (IElement root) => root.QuerySelectorAll("a.policy-citation").ToArray();

    [Fact]
    public void Known_section_becomes_a_link_to_its_anchor ()
    {
        var root = Render("A garantia é de 90 dias (Seção 12).");

        var link = Assert.Single(Citations(root));
        Assert.Equal("Seção 12", link.TextContent);
        Assert.Equal("politica#secao-12", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
        Assert.Equal("Seção 12 — Garantia (abre em nova janela)", link.GetAttribute("aria-label"));
        Assert.Equal("A garantia é de 90 dias (Seção 12).", root.TextContent);
    }

    [Fact]
    public void Clause_reference_keeps_its_text_and_links_to_the_section ()
    {
        var link = Assert.Single(Citations(Render("Custos de nuvem (Seção 8.6).")));

        Assert.Equal("Seção 8.6", link.TextContent);
        Assert.Equal("politica#secao-8", link.GetAttribute("href"));
    }

    [Fact]
    public void Plural_citation_links_each_number ()
    {
        var links = Citations(Render("Veja as Seções 12 e 13."));

        Assert.Equal(["12", "13"], links.Select(a => a.TextContent));
        Assert.Equal(["politica#secao-12", "politica#secao-13"], links.Select(a => a.GetAttribute("href")));
    }

    [Fact]
    public void Unknown_section_stays_plain_while_known_ones_are_linked ()
    {
        var root = Render("Seção 99 não existe, mas a Seção 12 sim.");

        var link = Assert.Single(Citations(root));
        Assert.Equal("Seção 12", link.TextContent);
        Assert.Equal("Seção 99 não existe, mas a Seção 12 sim.", root.TextContent);
    }

    [Fact]
    public void Code_span_is_not_linked ()
    {
        var root = Render("Use `Seção 12` literalmente.");

        Assert.Empty(Citations(root));
        Assert.Equal("Seção 12", root.QuerySelector("code")!.TextContent);
    }

    [Fact]
    public void Text_inside_an_existing_link_is_not_linked ()
    {
        var root = Render("[Seção 12 no site](https://example.com)");

        Assert.Empty(Citations(root));
        Assert.Equal("https://example.com", Assert.Single(root.QuerySelectorAll("a")).GetAttribute("href"));
    }

    [Fact]
    public void Emphasized_citation_is_linked ()
    {
        var link = Assert.Single(Citations(Render("Prazo: **Seção 12**")));

        Assert.Equal("STRONG", link.ParentElement!.TagName);
    }

    [Fact]
    public void Model_written_relative_link_is_still_removed ()
    {
        var root = Render("[veja](politica#secao-12)");

        Assert.DoesNotContain(root.QuerySelectorAll("a"), a => a.GetAttribute("href") is not null);
        Assert.Contains("veja", root.TextContent);
    }

    [Fact]
    public void Without_an_index_nothing_is_linked ()
    {
        var html = SafeMarkdown.ToHtml("(Seção 12)").Value;

        Assert.DoesNotContain("<a", html);
    }
}
