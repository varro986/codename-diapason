using Diapason.Domain;

namespace Diapason.Workbooks;

public sealed record DataFolderOptions(string Folder, string RegistryFileName, decimal HoursPerDay);

public sealed record LoadResult(
    DateTimeOffset LoadedAt, string Folder, Overview? Overview, IReadOnlyList<Issue> Issues, IReadOnlyList<string> Workbooks)
{
    public int Errors => Issues.Count(i => i.Severity == Severity.Error);

    public int Warnings => Issues.Count(i => i.Severity == Severity.Warning);
}

/// <summary>Reads the data folder (specs/data-contract.md, "Data folder"), only reading: nothing is ever written there.</summary>
public static class DataFolderLoader
{
    public static LoadResult Load(DataFolderOptions options, TimeProvider clock)
    {
        var now = clock.GetLocalNow();
        var folder = options.Folder;
        if (!Directory.Exists(folder))
            return new(now, folder, null, [Issue.Error(IssueCode.FolderMissing, null, Path.GetFullPath(folder))], []);

        var registryPath = Path.Combine(folder, options.RegistryFileName);
        if (!File.Exists(registryPath))
            return new(now, folder, null, [Issue.Error(IssueCode.RegistryMissing, null, Path.GetFullPath(registryPath))], []);

        var issues = new List<Issue>();
        var registryName = options.RegistryFileName;
        var registry = Read(registryPath, registryName, WorkbookReader.ReadRegistry, issues);
        if (registry is null || issues.Any(i => i.Severity == Severity.Error))
            return new(now, folder, null, issues, [registryName]);

        var plans = new List<PlanSource>();
        var planFiles = Directory.EnumerateFiles(folder, "*.xlsx", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).OfType<string>()
            .Where(name => !name.StartsWith("~$", StringComparison.Ordinal))
            .Where(name => !name.Equals(registryName, StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var name in planFiles)
        {
            var plan = Read(Path.Combine(folder, name), name, WorkbookReader.ReadPlan, issues);
            if (plan is not null) plans.Add(new PlanSource(name, plan));
        }

        var analysis = Analysis.Analyse(registryName, registry, plans, options.HoursPerDay);
        issues.AddRange(analysis.Issues);
        return new(now, folder, analysis.Overview, issues, [registryName, .. planFiles]);
    }

    private static T? Read<T>(string path, string name, Func<Stream, string, (T?, IReadOnlyList<Issue>)> read,
        List<Issue> issues) where T : class
    {
        try
        {
            // Shared access: a workbook open in Excel can still be read.
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var (value, found) = read(stream, name);
            issues.AddRange(found);
            return value;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException
                                      or FormatException or ArgumentException or InvalidOperationException)
        {
            issues.Add(Issue.Error(IssueCode.WorkbookUnreadable, new SourceLocation(name), e.Message));
            return null;
        }
    }
}
