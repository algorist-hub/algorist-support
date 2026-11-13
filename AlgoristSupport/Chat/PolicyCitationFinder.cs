using System.Text.RegularExpressions;

namespace AlgoristSupport.Chat;

/// <summary>A policy section citation found in text: the span to link and the section it refers to.</summary>
public readonly record struct PolicyCitation(int Start, int Length, int SectionNumber);

/// <summary>
/// Finds policy section citations in reply text: <c>Seção 12</c>, <c>Seção 12.1</c> (section 12) and
/// <c>Seções 12 e 13</c> / <c>Seções 8, 12 e 13</c> (one citation per number). Case-insensitive; accents optional.
/// Pure: it does not check whether a section exists (that is <see cref="PolicySectionIndex"/>'s job).
/// </summary>
public static partial class PolicyCitationFinder
{
    /// <summary>Returns the citations in <paramref name="text"/>, ordered by position and non-overlapping.</summary>
    public static IReadOnlyList<PolicyCitation> Find (string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var citations = new List<PolicyCitation>();

        // Singular: the whole "Seção 12.1" is the link text (larger click target).
        foreach (Match match in Singular().Matches(text))
        {
            citations.Add(new PolicyCitation(match.Index, match.Length, int.Parse(match.Groups["section"].ValueSpan)));
        }

        // Plural: each number in the list is its own link.
        foreach (Match match in Plural().Matches(text))
        {
            var list = match.Groups["list"];
            foreach (Match item in ListItem().Matches(list.Value))
            {
                citations.Add(new PolicyCitation(list.Index + item.Index, item.Length, int.Parse(item.Groups["section"].ValueSpan)));
            }
        }

        citations.Sort((a, b) => a.Start.CompareTo(b.Start));
        return citations;
    }

    private const string Number = @"(?<section>\d{1,3})(?:\.\d{1,3})?(?![\d\p{L}])";

    [GeneratedRegex(@"\bse[çc][ãa]o\s+" + Number, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Singular ();

    [GeneratedRegex(@"\bse[çc][õo]es\s+(?<list>\d{1,3}(?:\.\d{1,3})?(?:\s*(?:,|\be\b)\s*\d{1,3}(?:\.\d{1,3})?)*)(?![\d\p{L}])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Plural ();

    [GeneratedRegex(Number, RegexOptions.CultureInvariant)]
    private static partial Regex ListItem ();
}
