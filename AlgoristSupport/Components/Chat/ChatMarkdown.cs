using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;

namespace AlgoristSupport.Components.Chat;

/// <summary>
/// Converts assistant replies (Markdown produced by the LLM) into safe HTML for the message list.
/// Raw HTML in the source is escaped, and links/images are kept only for http, https and mailto URLs.
/// </summary>
internal static class ChatMarkdown
{
    private static readonly string[] AllowedSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeMailto];

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .UseSoftlineBreakAsHardlineBreak()
        .UseEmphasisExtras()
        .UsePipeTables()
        .UseAutoLinks()
        .Build();

    public static MarkupString ToHtml (string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);

        foreach (var link in document.Descendants<LinkInline>())
        {
            if (!IsSafeUrl(link.Url))
            {
                link.Url = null;
                continue;
            }

            if (!link.IsImage)
            {
                AddExternalLinkAttributes(link);
            }
        }

        foreach (var autolink in document.Descendants<AutolinkInline>())
        {
            AddExternalLinkAttributes(autolink);
        }

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        Pipeline.Setup(renderer);
        renderer.Render(document);
        writer.Flush();

        return new MarkupString(writer.ToString().Trim());
    }

    private static bool IsSafeUrl (string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        AllowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase);

    private static void AddExternalLinkAttributes (Inline link)
    {
        var attributes = link.GetAttributes();
        attributes.AddPropertyIfNotExist("target", "_blank");
        attributes.AddPropertyIfNotExist("rel", "noopener noreferrer");
    }
}
