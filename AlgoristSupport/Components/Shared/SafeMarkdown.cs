using System.Text.RegularExpressions;
using AlgoristSupport.Chat;
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
internal static partial class SafeMarkdown
{
    private static readonly string[] AllowedSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeMailto];

    private static readonly MarkdownPipeline Pipeline = CreatePipeline(headingIds: false);

    private static readonly MarkdownPipeline PipelineWithHeadingIds = CreatePipeline(headingIds: true);

    /// <summary>Relative URL of the policy page (respects <c>&lt;base href&gt;</c>).</summary>
    private const string PolicyPageUrl = "politica";

    /// <param name="markdown">The Markdown source.</param>
    /// <param name="headingIds">
    /// When true, headings get <c>id</c> attributes usable as link anchors: numbered sections (<c>## 12. Garantia</c>)
    /// get the title-independent <see cref="PolicySectionIndex.AnchorFor"/> anchor (<c>secao-12</c>), other headings a
    /// GitHub-style auto ID.
    /// </param>
    /// <param name="citations">
    /// When set, policy section citations (<c>Seção 12</c>) whose section exists in this index become links to that
    /// section on the policy page. These links are built by the app, never from model-written URLs.
    /// </param>
    public static MarkupString ToHtml (string markdown, bool headingIds = false, PolicySectionIndex? citations = null)
    {
        var pipeline = headingIds ? PipelineWithHeadingIds : Pipeline;
        var document = Markdown.Parse(markdown, pipeline);

        if (headingIds)
        {
            ApplySectionAnchors(document);
        }

        foreach (var link in document.Descendants<LinkInline>().ToList())
        {
            if (!IsSafeUrl(link.Url))
            {
                if (link.IsImage)
                {
                    link.Url = null;
                }
                else
                {
                    // Keep only the link text: an empty href would still be a clickable link to the current page.
                    Unwrap(link);
                }

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

        // Runs after the URL filter: citation links are relative, but built only from the index.
        if (citations is { Count: > 0 })
        {
            LinkCitations(document, citations);
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

    /// <summary>Overrides the auto ID of numbered level-2 headings with the numeric section anchor.</summary>
    private static void ApplySectionAnchors (MarkdownDocument document)
    {
        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            if (heading.Level != 2 || heading.Inline?.FirstChild is not LiteralInline first)
            {
                continue;
            }

            var match = SectionNumber().Match(first.Content.ToString());
            if (match.Success && int.TryParse(match.Groups[1].ValueSpan, out var number))
            {
                heading.GetAttributes().Id = PolicySectionIndex.AnchorFor(number);
            }
        }
    }

    /// <summary>
    /// Splits text literals outside links into text and links for citations of existing sections. Code spans are
    /// <see cref="CodeInline"/> nodes, so they are never visited.
    /// </summary>
    private static void LinkCitations (MarkdownDocument document, PolicySectionIndex index)
    {
        foreach (var literal in document.Descendants<LiteralInline>().ToList())
        {
            if (IsInsideLink(literal))
            {
                continue;
            }

            var text = literal.Content.ToString();
            var found = PolicyCitationFinder.Find(text);
            if (found.Count == 0)
            {
                continue;
            }

            var position = 0;
            var replaced = false;
            foreach (var citation in found)
            {
                if (!index.TryGet(citation.SectionNumber, out var section))
                {
                    continue;
                }

                if (citation.Start > position)
                {
                    literal.InsertBefore(new LiteralInline(text[position..citation.Start]));
                }

                literal.InsertBefore(CreateCitationLink(text.Substring(citation.Start, citation.Length), section));
                position = citation.Start + citation.Length;
                replaced = true;
            }

            if (!replaced)
            {
                continue;
            }

            if (position < text.Length)
            {
                literal.InsertBefore(new LiteralInline(text[position..]));
            }

            literal.Remove();
        }
    }

    private static LinkInline CreateCitationLink (string text, PolicySection section)
    {
        var link = new LinkInline($"{PolicyPageUrl}#{section.Anchor}", string.Empty);
        link.AppendChild(new LiteralInline(text));

        var attributes = link.GetAttributes();
        attributes.AddClass("policy-citation");
        attributes.AddPropertyIfNotExist("title", $"{section.Number}. {section.Title}");
        attributes.AddPropertyIfNotExist("aria-label", $"Seção {section.Number} — {section.Title} (abre em nova janela)");
        AddExternalLinkAttributes(link);
        return link;
    }

    /// <summary>Replaces <paramref name="container"/> with its children.</summary>
    private static void Unwrap (ContainerInline container)
    {
        var child = container.FirstChild;
        while (child is not null)
        {
            var next = child.NextSibling;
            child.Remove();
            container.InsertBefore(child);
            child = next;
        }

        container.Remove();
    }

    private static bool IsInsideLink (Inline inline)
    {
        for (var parent = inline.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is LinkInline)
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"^\s*(\d{1,3})\.\s", RegexOptions.CultureInvariant)]
    private static partial Regex SectionNumber ();

    private static void AddExternalLinkAttributes (Inline link)
    {
        var attributes = link.GetAttributes();
        attributes.AddPropertyIfNotExist("target", "_blank");
        attributes.AddPropertyIfNotExist("rel", "noopener noreferrer");
    }
}
