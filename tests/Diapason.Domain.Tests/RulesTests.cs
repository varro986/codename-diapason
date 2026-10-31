using Diapason.Domain;
using static Diapason.Domain.SampleData;

namespace Diapason.Domain.Tests;

public class RulesTests
{
    private static IssueCode[] Codes(IEnumerable<Issue> issues) => issues.Select(i => i.Code).ToArray();

    private static IReadOnlyList<Issue> CheckAssignment(Assignment assignment, Registry? registry = null) =>
        Rules.CheckPlan(Example with { Assignments = [assignment with { Row = 7 }] }, registry ?? Team, "e1.xlsx");

    private static Assignment Valid => Example.Assignments[0];

    [Fact]
    public void Sample_registry_and_plans_are_valid()
    {
        Assert.Empty(Rules.CheckRegistry(Team, "registry.xlsx"));
        Assert.Empty(Rules.CheckPlan(Example, Team, "e1.xlsx"));
        Assert.Empty(Rules.CheckPlan(Second, Team, "e2.xlsx"));
    }

    [Fact]
    public void An_issue_names_workbook_sheet_row_and_column()
    {
        var issue = Assert.Single(CheckAssignment(Valid with { PersonId = "P404" }));

        Assert.Equal(IssueCode.UnknownPerson, issue.Code);
        Assert.Equal(new SourceLocation("e1.xlsx", "Assignments", 7, "PersonId"), issue.Location);
        Assert.Equal(["P404"], issue.Args);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.01)]
    public void Allocation_must_be_above_0_and_at_most_1(double allocation) =>
        Assert.Equal([IssueCode.OutOfRange], Codes(CheckAssignment(Valid with { Allocation = (decimal)allocation })));

    [Fact]
    public void Person_and_level_exclude_each_other()
    {
        Assert.Equal([IssueCode.PersonOrLevel], Codes(CheckAssignment(Valid with { Level = "L6" })));
        Assert.Equal([IssueCode.PersonOrLevel], Codes(CheckAssignment(Valid with { PersonId = null })));
    }

    [Fact]
    public void Fortnights_must_be_in_the_calendar_and_in_order()
    {
        Assert.Equal([IssueCode.NotInCalendar],
            Codes(CheckAssignment(Valid with { LastFortnight = D(2030, 1, 1) })));
        Assert.Equal([IssueCode.LastBeforeFirst],
            Codes(CheckAssignment(Valid with { FirstFortnight = D(2026, 12, 16), LastFortnight = D(2026, 12, 1) })));
    }

    [Fact]
    public void Area_must_be_in_the_plan() =>
        Assert.Equal([IssueCode.UnknownArea], Codes(CheckAssignment(Valid with { Area = "Nowhere" })));

    [Fact]
    public void A_person_needs_a_level_in_force()
    {
        var registry = Team with { LevelHistory = [.. Team.LevelHistory.Where(l => l.PersonId != "P2")] };

        Assert.Equal([IssueCode.NoLevelInForce], Codes(CheckAssignment(Valid, registry)));
    }

    [Fact]
    public void A_level_needs_rates_in_force()
    {
        var registry = Team with
        {
            LevelRates = [.. Team.LevelRates.Select(r => r.Level == "L9" ? r with { ValidFrom = D(2026, 12, 16) } : r)],
        };

        Assert.Equal([IssueCode.NoRateInForce], Codes(CheckAssignment(Valid, registry)));
    }

    [Fact]
    public void Registry_rules()
    {
        var registry = Team with
        {
            People = [.. Team.People, new Person("P1", "Again", 9)],
            LevelHistory = [.. Team.LevelHistory, new LevelPeriod("P3", "L99", D(2026, 1, 1), 9)],
            LevelRates = [.. Team.LevelRates, new LevelRate("L7", D(2026, 1, 1), 0m, 10m, -0.1m, 9)],
            Calendar = [.. Team.Calendar, new Fortnight(D(2027, 2, 16), 80m, 9), new Fortnight(D(2027, 3, 2), 80m, 10)],
        };

        Assert.Equal(
            new[]
            {
                IssueCode.Duplicate, IssueCode.UnknownPerson, IssueCode.UnknownLevel, IssueCode.OutOfRange,
                IssueCode.OutOfRange, IssueCode.NotAFortnightStart, IssueCode.CalendarGap,
            }.Order(),
            Codes(Rules.CheckRegistry(registry, "registry.xlsx")).Order());
    }

    [Fact]
    public void Calendar_gap_names_the_missing_fortnight()
    {
        var registry = Team with { Calendar = [.. Team.Calendar.Where(f => f.Start != D(2026, 12, 16))] };

        var issue = Assert.Single(Rules.CheckRegistry(registry, "registry.xlsx"));

        Assert.Equal(IssueCode.CalendarGap, issue.Code);
        Assert.Equal([D(2026, 12, 16), D(2027, 1, 1)], issue.Args);
    }
}
