using ClosedXML.Excel;
using Diapason.Domain;
using static Diapason.Domain.SampleData;

namespace Diapason.Workbooks.Tests;

public class WorkbookTests
{
    private static (Registry, IReadOnlyList<Issue>) ReadRegistry(string path)
    {
        using var stream = File.OpenRead(path);
        return WorkbookReader.ReadRegistry(stream, Path.GetFileName(path));
    }

    private static (Plan?, IReadOnlyList<Issue>) ReadPlan(string path)
    {
        using var stream = File.OpenRead(path);
        return WorkbookReader.ReadPlan(stream, Path.GetFileName(path));
    }

    [Fact]
    public void Writer_and_reader_agree_on_the_registry()
    {
        using var temp = new TempFolder();
        var registry = Team with { PersonRates = [new PersonRate("P2", D(2026, 12, 16), 50m, 30m, 2)] };
        WorkbookWriter.WriteRegistry(temp.File("r.xlsx"), registry);

        var (read, issues) = ReadRegistry(temp.File("r.xlsx"));

        Assert.Empty(issues);
        Assert.Equal(registry.People, read.People);
        Assert.Equal(registry.LevelHistory, read.LevelHistory);
        Assert.Equal(registry.LevelRates, read.LevelRates);
        Assert.Equal(registry.PersonRates, read.PersonRates);
        Assert.Equal(registry.Calendar, read.Calendar);
    }

    [Fact]
    public void Writer_and_reader_agree_on_a_plan()
    {
        using var temp = new TempFolder();
        WorkbookWriter.WritePlan(temp.File("p.xlsx"), Second);

        var (read, issues) = ReadPlan(temp.File("p.xlsx"));

        Assert.Empty(issues);
        Assert.NotNull(read);
        Assert.Equal((Second.EngagementId, Second.Name), (read.EngagementId, read.Name));
        Assert.Equal(Second.Areas, read.Areas);
        Assert.Equal(Second.Assignments, read.Assignments);
    }

    [Fact]
    public void A_wrong_cell_names_sheet_row_and_column()
    {
        using var temp = new TempFolder();
        var path = temp.File("p.xlsx");
        WorkbookWriter.WritePlan(path, Example);
        using (var book = new XLWorkbook(path))
        {
            var sheet = book.Worksheet("Assignments");
            sheet.Cell(2, 8).Value = "half";
            sheet.Cell(2, 6).Value = "Maybe";
            book.Save();
        }

        var (plan, issues) = ReadPlan(path);

        Assert.Null(plan);
        Assert.Equal(
            [
                Issue.Error(IssueCode.NotAllowed, new("p.xlsx", "Assignments", 2, "Kind"), "Maybe", "Forecast, Actual"),
                Issue.Error(IssueCode.NotANumber, new("p.xlsx", "Assignments", 2, "Allocation"), "half"),
            ],
            issues, IssueComparer.Instance);
    }

    [Fact]
    public void A_missing_sheet_or_column_is_an_error_but_PersonRates_is_optional()
    {
        using var temp = new TempFolder();
        var path = temp.File("r.xlsx");
        WorkbookWriter.WriteRegistry(path, Team);
        using (var book = new XLWorkbook(path))
        {
            book.Worksheet("PersonRates").Delete();
            book.Worksheet("Calendar").Delete();
            book.Worksheet("People").Cell(1, 2).Value = "Name";
            book.Save();
        }

        var (_, issues) = ReadRegistry(path);

        Assert.Equal(
            [
                Issue.Error(IssueCode.ColumnMissing, new("r.xlsx", "People"), "DisplayName"),
                Issue.Error(IssueCode.SheetMissing, new("r.xlsx"), "Calendar"),
            ],
            issues, IssueComparer.Instance);
    }

    [Fact]
    public void Dates_written_as_text_are_read_and_empty_rows_skipped()
    {
        using var temp = new TempFolder();
        var path = temp.File("p.xlsx");
        WorkbookWriter.WritePlan(path, Example);
        using (var book = new XLWorkbook(path))
        {
            var sheet = book.Worksheet("Assignments");
            sheet.Cell(2, 9).Value = "01/12/2026";
            sheet.Cell(5, 1).Value = "";
            book.Save();
        }

        var (plan, issues) = ReadPlan(path);

        Assert.Empty(issues);
        Assert.Equal(D(2026, 12, 1), Assert.Single(plan!.Assignments).FirstFortnight);
    }

    [Fact]
    public void Engagement_sheet_needs_exactly_one_row()
    {
        using var temp = new TempFolder();
        var path = temp.File("p.xlsx");
        WorkbookWriter.WritePlan(path, Plan.Empty);

        var (plan, issues) = ReadPlan(path);

        Assert.Null(plan);
        Assert.Equal(IssueCode.OneRowExpected, Assert.Single(issues).Code);
    }

    [Fact]
    public void Templates_have_the_sheets_and_headers_of_the_contract_and_are_never_overwritten()
    {
        using var temp = new TempFolder();

        var written = Generators.WriteTemplates(temp.Path);

        Assert.Equal(2, written.Count);
        using (var registry = new XLWorkbook(temp.File(Generators.RegistryFile)))
            Assert.Equal(["People", "LevelHistory", "LevelRates", "PersonRates", "Calendar"],
                registry.Worksheets.Select(s => s.Name));
        using (var plan = new XLWorkbook(temp.File(Generators.PlanTemplateFile)))
            Assert.Equal(
                ["AssignmentId", "PersonId", "Level", "Area", "Role", "Kind", "WorkType", "Allocation", "FirstFortnight", "LastFortnight"],
                plan.Worksheet("Assignments").Row(1).CellsUsed().Select(c => c.GetString()));

        var before = File.GetLastWriteTimeUtc(temp.File(Generators.RegistryFile));
        Assert.Throws<IOException>(() => Generators.WriteTemplates(temp.Path));
        Assert.Equal(before, File.GetLastWriteTimeUtc(temp.File(Generators.RegistryFile)));
    }

    /// <summary>Issues compared by value, arguments included.</summary>
    private sealed class IssueComparer : IEqualityComparer<Issue>
    {
        public static readonly IssueComparer Instance = new();

        public bool Equals(Issue? x, Issue? y) =>
            x is not null && y is not null && x.Severity == y.Severity && x.Code == y.Code && x.Location == y.Location &&
            x.Args.SequenceEqual(y.Args);

        public int GetHashCode(Issue obj) => HashCode.Combine(obj.Code, obj.Location);
    }
}
