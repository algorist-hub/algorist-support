using AlgoristSupport.Chat;

namespace AlgoristSupport.Tests.Chat;

public sealed class PolicyDocumentLoaderTests : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("policy-tests-").FullName;

    public void Dispose () => Directory.Delete(directory, recursive: true);

    private string WriteFile (string content)
    {
        var path = Path.Combine(directory, "policy.md");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Valid_file_is_loaded ()
    {
        var path = WriteFile("# Política\n\nTexto.\n");

        var result = PolicyDocumentLoader.Load(path);

        Assert.True(result.IsLoaded);
        Assert.Null(result.Problem);
        Assert.Equal("# Política\n\nTexto.", result.Document!.Markdown);
        Assert.Equal(path, result.Path);
    }

    [Fact]
    public void Missing_file_is_reported ()
    {
        var path = Path.Combine(directory, "nao-existe.md");

        var result = PolicyDocumentLoader.Load(path);

        Assert.False(result.IsLoaded);
        Assert.Equal(PolicyLoadProblem.Missing, result.Problem);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  \n\t \r\n")]
    public void Empty_or_whitespace_file_is_reported (string content)
    {
        var result = PolicyDocumentLoader.Load(WriteFile(content));

        Assert.False(result.IsLoaded);
        Assert.Equal(PolicyLoadProblem.Empty, result.Problem);
    }

    [Fact]
    public void Locked_file_is_reported_as_unreadable ()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // Exclusive file locks are only enforced on Windows.
        }

        var path = WriteFile("# Política");
        using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var result = PolicyDocumentLoader.Load(path);

        Assert.False(result.IsLoaded);
        Assert.Equal(PolicyLoadProblem.Unreadable, result.Problem);
    }

    [Fact]
    public void Shipped_policy_is_present_in_output_and_loads ()
    {
        Assert.True(File.Exists(PolicyDocumentLoader.DefaultPath), $"Policy not found at {PolicyDocumentLoader.DefaultPath}");

        var result = PolicyDocumentLoader.Load(PolicyDocumentLoader.DefaultPath);

        Assert.True(result.IsLoaded);
        Assert.StartsWith("# Política de Prestação de Serviços", result.Document!.Markdown);
    }
}
