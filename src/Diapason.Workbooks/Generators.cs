using Diapason.Domain;

namespace Diapason.Workbooks;

/// <summary>Workbooks generated for people (empty templates) and for tests and development (synthetic data).</summary>
public static class Generators
{
    public const string RegistryFile = "registry.xlsx";
    public const string PlanTemplateFile = "plan.xlsx";

    /// <summary>An empty registry and an empty plan (spec 001, criterion 15). Returns the files written.</summary>
    public static IReadOnlyList<string> WriteTemplates(string folder) =>
        WriteAll(folder,
        [
            (RegistryFile, path => WorkbookWriter.WriteRegistry(path, Registry.Empty)),
            (PlanTemplateFile, path => WorkbookWriter.WritePlan(path, Plan.Empty)),
        ]);

    /// <summary>The synthetic registry and plans of <see cref="SampleData"/> (spec 001, criterion 16).</summary>
    public static IReadOnlyList<string> WriteSampleData(string folder) =>
        WriteAll(folder,
        [
            (RegistryFile, path => WorkbookWriter.WriteRegistry(path, SampleData.Team)),
            ("e1-example.xlsx", path => WorkbookWriter.WritePlan(path, SampleData.Example)),
            ("e2-second.xlsx", path => WorkbookWriter.WritePlan(path, SampleData.Second)),
            ("e9-broken.xlsx", path => WorkbookWriter.WritePlan(path, SampleData.Broken)),
        ]);

    private static IReadOnlyList<string> WriteAll(string folder, (string Name, Action<string> Write)[] files)
    {
        // Checked before writing anything, so a refused run leaves the folder as it was.
        var existing = files.Select(f => Path.GetFullPath(Path.Combine(folder, f.Name))).Where(File.Exists).ToList();
        if (existing.Count > 0)
            throw new IOException($"Not written, these files already exist: {string.Join(", ", existing)}");

        Directory.CreateDirectory(folder);
        return files.Select(f =>
        {
            var path = Path.GetFullPath(Path.Combine(folder, f.Name));
            f.Write(path);
            return path;
        }).ToList();
    }
}
