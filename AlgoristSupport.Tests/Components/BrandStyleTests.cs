namespace AlgoristSupport.Tests.Components;

/// <summary>
/// Guards the brand palette: the app's own stylesheets must not bring back the default
/// Blazor/Bootstrap template blues (Bootstrap itself is themed through its CSS variables).
/// </summary>
public class BrandStyleTests
{
    private static readonly string[] TemplateBlues =
    [
        "#1b6ec2", "#1861ac", "#006bb7", "#0b5394", "#258cfb", "#0087ff", "#6b9ed2", "#3b6ea2",
    ];

    [Fact]
    public void App_stylesheets_do_not_use_template_blues ()
    {
        var appDir = FindAppProjectDirectory();
        var files = Directory.EnumerateFiles(Path.Combine(appDir, "Components"), "*.razor.css", SearchOption.AllDirectories)
            .Append(Path.Combine(appDir, "wwwroot", "app.css"))
            .ToList();

        var offenders = files
            .SelectMany(file => TemplateBlues
                .Where(blue => File.ReadAllText(file).Contains(blue, StringComparison.OrdinalIgnoreCase))
                .Select(blue => $"{Path.GetRelativePath(appDir, file)}: {blue}"))
            .ToList();

        Assert.True(files.Count > 1, "No stylesheets found to check.");
        Assert.True(offenders.Count == 0, "Template blue found:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    private static string FindAppProjectDirectory ()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "AlgoristSupport");
            if (File.Exists(Path.Combine(candidate, "AlgoristSupport.csproj")))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("AlgoristSupport project directory not found above the test output.");
    }
}
