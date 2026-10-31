using ClosedXML.Excel;
using Diapason.Domain;
using C = Diapason.Domain.DataContract;

namespace Diapason.Workbooks;

/// <summary>From a workbook to the domain types; only the types of the cells are checked here, the rules in the domain.</summary>
public static class WorkbookReader
{
    public static (Registry Registry, IReadOnlyList<Issue> Issues) ReadRegistry(Stream stream, string workbook)
    {
        var issues = new List<Issue>();
        using var book = new XLWorkbook(stream);
        SheetReader Sheet(Layout.Sheet layout) => new(book, layout, workbook, issues);

        var people = Sheet(Layout.People).Rows(r =>
            new Person(r.Text(C.People.PersonId), r.Text(C.People.DisplayName), r.Line));
        var levels = Sheet(Layout.LevelHistory).Rows(r => new LevelPeriod(
            r.Text(C.LevelHistory.PersonId), r.Text(C.LevelHistory.Level), r.Date(C.LevelHistory.ValidFrom), r.Line));
        var levelRates = Sheet(Layout.LevelRates).Rows(r => new LevelRate(
            r.Text(C.LevelRates.Level), r.Date(C.LevelRates.ValidFrom), r.Number(C.LevelRates.ChargeableRate),
            r.Number(C.LevelRates.BusinessDevelopmentRate), r.Number(C.LevelRates.ExtraTimeShare), r.Line));
        var personRates = Sheet(Layout.PersonRates).Rows(r => new PersonRate(
            r.Text(C.PersonRates.PersonId), r.Date(C.PersonRates.ValidFrom), r.Number(C.PersonRates.ChargeableRate),
            r.Number(C.PersonRates.BusinessDevelopmentRate), r.Line));
        var calendar = Sheet(Layout.Calendar).Rows(r =>
            new Fortnight(r.Date(C.Calendar.FortnightStart), r.Number(C.Calendar.WorkingHours), r.Line));

        return (new Registry(people, levels, levelRates, personRates, calendar), issues);
    }

    public static (Plan? Plan, IReadOnlyList<Issue> Issues) ReadPlan(Stream stream, string workbook)
    {
        var issues = new List<Issue>();
        using var book = new XLWorkbook(stream);
        SheetReader Sheet(Layout.Sheet layout) => new(book, layout, workbook, issues);

        var engagement = Sheet(Layout.Engagement).Rows(r =>
            (Id: r.Text(C.Engagement.EngagementId), Name: r.Text(C.Engagement.Name), r.Line));
        var areas = Sheet(Layout.Areas).Rows(r => new Area(r.Text(C.Areas.Area), r.Number(C.Areas.SoldValue), r.Line));
        var assignments = Sheet(Layout.Assignments).Rows(r => new Assignment(
            r.Text(C.Assignments.AssignmentId), r.OptionalText(C.Assignments.PersonId), r.OptionalText(C.Assignments.Level),
            r.Text(C.Assignments.Area), r.Text(C.Assignments.Role), r.Choice<Kind>(C.Assignments.Kind),
            r.Choice<WorkType>(C.Assignments.WorkType), r.Number(C.Assignments.Allocation),
            r.Date(C.Assignments.FirstFortnight), r.Date(C.Assignments.LastFortnight), r.Line));

        if (book.TryGetWorksheet(C.Engagement.Sheet, out _) && engagement.Count != 1 && issues.Count == 0)
            issues.Add(Issue.Error(IssueCode.OneRowExpected, new(workbook, C.Engagement.Sheet),
                engagement.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        if (issues.Any(i => i.Severity == Severity.Error)) return (null, issues);
        var (id, name, _) = engagement[0];
        return (new Plan(id, name, areas, assignments), issues);
    }
}
