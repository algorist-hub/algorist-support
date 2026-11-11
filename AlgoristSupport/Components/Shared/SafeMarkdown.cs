using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;

namespace AlgoristSupport.Components.Shared;

/// <summary>
/// Converts Markdown (assistant replies, the policy document) into safe HTML.
/// Raw HTML in the source is escaped, and links/images are kept only for http, https and mailto URLs.
/// </summary>
internal static class SafeMarkdown
{
    private static readonly string[] AllowedSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeMailto];

    private static readonly MarkdownPipeline Pipeline = CreatePipeline(headingIds: false);

    private static readonly MarkdownPipeline PipelineWithHeadingIds = CreatePipeline(headingIds: true);

    /// <param name="markdown">The Markdown source.</param>
    /// <param name="headingIds">
    /// When true, headings get stable <c>id</c> attributes (e.g. <c>12-garantia</c>) usable as link anchors.
    /// </param>
    public static MarkupString ToHtml (string markdown, bool headingIds = false)
    {
        var pipeline = headingIds ? PipelineWithHeadingIds : Pipeline;
        var document = Markdown.Parse(markdown, pipeline);

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
        pipeline.Setup(renderer);
        renderer.Render(document);
        writer.Flush();

        return new MarkupString(writer.ToString().Trim());
    }

    private static MarkdownPipeline CreatePipeline (bool headingIds)
    {
        var builder = new MarkdownPipelineBuilder()
            .DisableHtml()
            .UseSoftlineBreakAsHardlineBreak()
            .UseEmphasisExtras()
            .UsePipeTables()
            .UseAutoLinks();

        if (headingIds)
        {
            builder.UseAutoIdentifiers(AutoIdentifierOptions.GitHub);
        }

        return builder.Build();
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
