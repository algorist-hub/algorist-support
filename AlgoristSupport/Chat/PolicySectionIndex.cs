using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace AlgoristSupport.Chat;

/// <summary>A numbered top-level section of the policy (<c>## 12. Garantia</c>).</summary>
public sealed record PolicySection(int Number, string Title, string Anchor);

/// <summary>
/// The numbered sections of the policy, keyed by number. Single source of truth for which sections exist and for
/// their anchor on the policy page, shared by the chat citation links and the policy page itself.
/// </summary>
public sealed partial class PolicySectionIndex
{
    private readonly IReadOnlyDictionary<int, PolicySection> sections;

    private PolicySectionIndex (IReadOnlyDictionary<int, PolicySection> sections) => this.sections = sections;

    /// <summary>An index with no sections (used when the policy could not be loaded).</summary>
    public static PolicySectionIndex Empty { get; } = new(new Dictionary<int, PolicySection>());

    public int Count => sections.Count;

    public IEnumerable<PolicySection> Sections => sections.Values.OrderBy(s => s.Number);

    /// <summary>Anchor (HTML id) of section <paramref name="number"/> on the policy page, e.g. <c>secao-12</c>.</summary>
    public static string AnchorFor (int number) => $"secao-{number}";

    /// <summary>Builds the index from the level-2 headings written as <c>## N. Title</c>. Pure: no I/O.</summary>
    public static PolicySectionIndex Build (PolicyDocument policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var sections = new Dictionary<int, PolicySection>();
        foreach (Match match in SectionHeading().Matches(policy.Markdown))
        {
            if (int.TryParse(match.Groups["number"].ValueSpan, out var number))
            {
                sections.TryAdd(number, new PolicySection(number, match.Groups["title"].Value.Trim(), AnchorFor(number)));
            }
        }

        return new PolicySectionIndex(sections);
    }

    /// <summary>Builds the index from a load result; <see cref="Empty"/> when the policy was not loaded.</summary>
    public static PolicySectionIndex From (PolicyLoadResult result) =>
        result.Document is { } document ? Build(document) : Empty;

    public bool TryGet (int number, [NotNullWhen(true)] out PolicySection? section) =>
        sections.TryGetValue(number, out section);

    [GeneratedRegex(@"^##[ \t]+(?<number>\d{1,3})\.[ \t]+(?<title>\S.*?)[ \t]*\r?$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex SectionHeading ();
}
