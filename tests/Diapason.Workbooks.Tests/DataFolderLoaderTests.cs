using System.Security.Cryptography;
using Diapason.Domain;

namespace Diapason.Workbooks.Tests;

// Spec 001, criteria 7, 8, 10, 11 and 16.
public class DataFolderLoaderTests
{
    private static LoadResult Load(string folder) =>
        DataFolderLoader.Load(new DataFolderOptions(folder, Generators.RegistryFile, 8m), TimeProvider.System);

    [Fact]
    public void Sample_data_loads_with_the_numbers_of_the_contract_and_the_broken_plan_left_out()
    {
        using var temp = new TempFolder();
        Generators.WriteSampleData(temp.Path);

        var result = Load(temp.Path);

        var overview = Assert.IsType<Overview>(result.Overview);
        Assert.Equal(["E1", "E2"], overview.Engagements.Select(e => e.EngagementId));
        var core = overview.Engagements[0].Areas.Single();
        Assert.Equal(new Figures(50_000m, 3_840m, 80m, 10m), core.Forecast);
        Assert.All(result.Issues, i => Assert.Equal("e9-broken.xlsx", i.Location?.Workbook));
        Assert.Equal(2, result.Errors);
    }

    [Fact]
    public void Lock_files_subfolders_and_other_files_are_skipped()
    {
        using var temp = new TempFolder();
        Generators.WriteSampleData(temp.Path);
        File.WriteAllText(temp.File("~$e1-example.xlsx"), "lock");
        File.WriteAllText(temp.File("notes.txt"), "not a workbook");
        var sub = Directory.CreateDirectory(temp.File("archive"));

        try
        {
            File.Copy(temp.File("e1-example.xlsx"), Path.Combine(sub.FullName, "old.xlsx"));
            var result = Load(temp.Path);

            Assert.Equal(["registry.xlsx", "e1-example.xlsx", "e2-second.xlsx", "e9-broken.xlsx"], result.Workbooks);
        }
        finally
        {
            File.Delete(Path.Combine(sub.FullName, "old.xlsx"));
            sub.Delete();
        }
    }

    [Fact]
    public void A_missing_folder_or_registry_says_which_path_was_looked_for()
    {
        using var temp = new TempFolder();
        var missing = temp.File("nowhere");

        var noFolder = Assert.Single(Load(missing).Issues);
        var noRegistry = Assert.Single(Load(temp.Path).Issues);

        Assert.Equal((IssueCode.FolderMissing, missing), (noFolder.Code, noFolder.Args[0]));
        Assert.Equal((IssueCode.RegistryMissing, temp.File("registry.xlsx")), (noRegistry.Code, noRegistry.Args[0]));
    }

    [Fact]
    public void A_registry_with_errors_shows_no_plan()
    {
        using var temp = new TempFolder();
        WorkbookWriter.WriteRegistry(temp.File("registry.xlsx"),
            SampleData.Team with { People = [.. SampleData.Team.People, new Person("P1", "Again", 4)] });
        WorkbookWriter.WritePlan(temp.File("e1.xlsx"), SampleData.Example);

        var result = Load(temp.Path);

        Assert.Null(result.Overview);
        Assert.Equal(IssueCode.Duplicate, Assert.Single(result.Issues).Code);
    }

    [Fact]
    public void A_file_that_is_not_a_workbook_is_an_issue_not_a_crash()
    {
        using var temp = new TempFolder();
        Generators.WriteSampleData(temp.Path);
        File.WriteAllText(temp.File("fake.xlsx"), "not a zip");

        var result = Load(temp.Path);

        Assert.Contains(result.Issues, i => i.Code == IssueCode.WorkbookUnreadable && i.Location?.Workbook == "fake.xlsx");
        Assert.Equal(2, result.Overview!.Engagements.Count);
    }

    [Fact]
    public void A_zip_with_broken_content_is_an_issue_not_a_crash()
    {
        using var temp = new TempFolder();
        Generators.WriteSampleData(temp.Path);
        using (var zip = System.IO.Compression.ZipFile.Open(temp.File("broken.xlsx"), System.IO.Compression.ZipArchiveMode.Create))
        using (var writer = new StreamWriter(zip.CreateEntry("[Content_Types].xml").Open()))
            writer.Write("<Types><not closed");

        var result = Load(temp.Path);

        Assert.Contains(result.Issues, i => i.Code == IssueCode.WorkbookUnreadable && i.Location?.Workbook == "broken.xlsx");
        Assert.Equal(2, result.Overview!.Engagements.Count);
    }

    [Fact]
    public void Loading_never_changes_the_data_folder()
    {
        using var temp = new TempFolder();
        Generators.WriteSampleData(temp.Path);
        var before = Snapshot(temp.Path);

        Load(temp.Path);
        Load(temp.Path);

        Assert.Equal(before, Snapshot(temp.Path));
    }

    private static Dictionary<string, (DateTime, string)> Snapshot(string folder) =>
        Directory.EnumerateFiles(folder).ToDictionary(p => p, p =>
            (File.GetLastWriteTimeUtc(p), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)))));
}
