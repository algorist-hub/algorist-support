using AlgoristSupport.Components.Layout;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace AlgoristSupport.Tests.Components;

public class BrandBarTests : BunitContext
{
    [Fact]
    public void Logo_links_to_the_company_site_in_a_new_window ()
    {
        var cut = Render<BrandBar>();

        var link = cut.Find("header.brand-bar a.brand-link");
        Assert.Equal("https://algorist.com.br", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
    }

    [Fact]
    public void Logo_image_has_alternative_text ()
    {
        var cut = Render<BrandBar>();

        var img = cut.Find("a.brand-link img");
        Assert.Equal("Algorist", img.GetAttribute("alt"));
        Assert.Equal("brand/logo.png", img.GetAttribute("src"));
    }

    [Fact]
    public void Logo_link_accessible_name_says_it_opens_in_a_new_window ()
    {
        var cut = Render<BrandBar>();

        var link = cut.Find("a.brand-link");
        // Accessible name = image alt text + visually hidden hint.
        var name = link.QuerySelector("img")!.GetAttribute("alt") + link.QuerySelector(".visually-hidden")!.TextContent;
        Assert.Equal("Algorist (abre em nova janela)", name);
    }

    [Fact]
    public void Main_layout_renders_the_brand_bar_above_the_body ()
    {
        RenderFragment body = builder => builder.AddMarkupContent(0, "<p class=\"page-body\">conteúdo</p>");

        var cut = Render<MainLayout>(parameters => parameters.Add(p => p.Body, body));

        var bar = cut.Find(".page > header.brand-bar");
        Assert.Equal("https://algorist.com.br", bar.QuerySelector("a")!.GetAttribute("href"));
        Assert.NotNull(cut.Find(".page main .page-body"));
        Assert.Null(bar.Closest("main"));
    }
}
