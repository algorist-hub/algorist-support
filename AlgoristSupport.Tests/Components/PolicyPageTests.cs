using AlgoristSupport.Chat;
using AlgoristSupport.Components.Pages;
using AlgoristSupport.Tests.Chat;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace AlgoristSupport.Tests.Components;

public class PolicyPageTests : BunitContext
{
    private IRenderedComponent<Policy> RenderWith (PolicyLoadResult result)
    {
        Services.AddSingleton(result);
        return Render<Policy>();
    }

    [Fact]
    public void Shipped_policy_is_rendered_as_formatted_html ()
    {
        var cut = RenderWith(PolicyDocumentLoader.Load(PolicyDocumentLoader.DefaultPath));

        Assert.Equal(
            "Política de Prestação de Serviços de Desenvolvimento de Software — Algorist",
            cut.Find("article.policy h1").TextContent);
        Assert.NotEmpty(cut.FindAll("article.policy h2"));
        Assert.Contains("Crítica", cut.Find("article.policy table").TextContent);
        Assert.DoesNotContain("**", cut.Find("article.policy").TextContent);
        Assert.DoesNotContain("## ", cut.Find("article.policy").TextContent);
    }

    [Fact]
    public void Fictional_notice_is_visible_near_the_top ()
    {
        var cut = RenderWith(PolicyDocumentLoader.Load(PolicyDocumentLoader.DefaultPath));

        var children = cut.Find("article.policy").Children.ToList();
        var noticeIndex = children.FindIndex(e => e.TagName == "BLOCKQUOTE");

        // Right after the title.
        Assert.Equal(1, noticeIndex);
        Assert.Contains("exemplo fictício", children[noticeIndex].TextContent);
    }

    [Fact]
    public void Headings_get_anchor_ids ()
    {
        var cut = RenderWith(PolicyLoadResult.Loaded("p.md", new PolicyDocument("## 12. Garantia\n\nTexto.")));

        Assert.Equal("secao-12", cut.Find("h2").GetAttribute("id"));
    }

    [Fact]
    public void Section_anchor_does_not_depend_on_the_title ()
    {
        var cut = RenderWith(PolicyLoadResult.Loaded("p.md", new PolicyDocument("## 12. Garantia estendida (novo título)\n\nTexto.")));

        Assert.Equal("secao-12", cut.Find("h2").GetAttribute("id"));
    }

    [Fact]
    public void Unnumbered_headings_keep_an_auto_id ()
    {
        var cut = RenderWith(PolicyLoadResult.Loaded("p.md", new PolicyDocument("# Política\n\n## Anexo\n\n### 3. Subitem")));

        Assert.False(string.IsNullOrEmpty(cut.Find("h1").GetAttribute("id")));
        Assert.False(string.IsNullOrEmpty(cut.Find("h2").GetAttribute("id")));
        Assert.NotEqual("secao-3", cut.Find("h3").GetAttribute("id"));
    }

    [Fact]
    public void Shipped_policy_sections_have_numeric_anchors ()
    {
        var cut = RenderWith(PolicyDocumentLoader.Load(PolicyDocumentLoader.DefaultPath));

        var ids = cut.FindAll("h2").Select(h => h.GetAttribute("id")).ToList();
        Assert.Equal(Enumerable.Range(1, 21).Select(n => $"secao-{n}"), ids);
    }

    [Fact]
    public void Html_in_the_policy_is_shown_as_text ()
    {
        var cut = RenderWith(PolicyLoadResult.Loaded("p.md", new PolicyDocument("Texto <script>alert(1)</script> fim")));

        Assert.Empty(cut.FindAll("script"));
        Assert.Contains("<script>alert(1)</script>", cut.Find("article.policy").TextContent);
    }

    [Fact]
    public void Missing_policy_shows_unavailable_message_without_details ()
    {
        var cut = RenderWith(TestChat.MissingPolicy());

        Assert.Empty(cut.FindAll("article.policy"));
        var alert = cut.Find(".policy-unavailable");
        Assert.Equal("alert", alert.GetAttribute("role"));
        Assert.Contains("temporariamente indisponível", alert.TextContent);
        Assert.DoesNotContain("policy.md", cut.Markup);
        Assert.DoesNotContain("Missing", cut.Markup);
    }
}
