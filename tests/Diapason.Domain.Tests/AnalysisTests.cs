using Diapason.Domain;
using static Diapason.Domain.SampleData;

namespace Diapason.Domain.Tests;

// Spec 001, criteria 7 to 9.
public class AnalysisTests
{
    private static AnalysisResult Analyse(Registry registry, params PlanSource[] plans) =>
        Analysis.Analyse("registry.xlsx", registry, plans, 8m);

    [Fact]
    public void A_plan_with_errors_is_left_out_and_the_others_are_shown()
    {
        var result = Analyse(Team, new PlanSource("e1.xlsx", Example), new PlanSource("e9.xlsx", Broken));

        Assert.Equal(["E1"], result.Overview!.Engagements.Select(e => e.EngagementId));
        Assert.All(result.Issues, i => Assert.Equal("e9.xlsx", i.Location?.Workbook));
        Assert.Equal([IssueCode.UnknownPerson, IssueCode.UnknownArea], result.Issues.Select(i => i.Code));
    }

    [Fact]
    public void A_registry_with_errors_shows_no_plan()
    {
        var registry = Team with { People = [.. Team.People, new Person("P1", "Again", 9)] };

        var result = Analyse(registry, new PlanSource("e1.xlsx", Example));

        Assert.Null(result.Overview);
        Assert.Equal("registry.xlsx", Assert.Single(result.Issues).Location?.Workbook);
    }

    [Fact]
    public void The_same_engagement_in_two_workbooks_keeps_the_first()
    {
        var result = Analyse(Team, new PlanSource("a.xlsx", Example), new PlanSource("b.xlsx", Example));

        Assert.Equal("a.xlsx", Assert.Single(result.Overview!.Engagements).Workbook);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(IssueCode.DuplicateEngagement, issue.Code);
        Assert.Equal(["E1", "a.xlsx"], issue.Args);
    }

    [Fact]
    public void Over_allocation_across_plans_is_a_warning_and_numbers_are_still_shown()
    {
        var other = Example with
        {
            EngagementId = "E3",
            Assignments = [Example.Assignments[0] with { Allocation = 0.75m, LastFortnight = D(2026, 12, 1) }],
        };

        var result = Analyse(Team, new PlanSource("e1.xlsx", Example), new PlanSource("e3.xlsx", other));

        Assert.Equal(2, result.Overview!.Engagements.Count);
        var warning = Assert.Single(result.Issues);
        Assert.Equal(Severity.Warning, warning.Severity);
        Assert.Equal(IssueCode.OverAllocation, warning.Code);
        Assert.Equal(["P2", D(2026, 12, 1), Kind.Forecast, 1.25m, "e1.xlsx, e3.xlsx"], warning.Args);
    }

    [Fact]
    public void Forecast_and_actual_do_not_add_up_for_over_allocation()
    {
        var plan = Example with
        {
            Assignments =
            [
                Example.Assignments[0] with { Allocation = 1m },
                Example.Assignments[0] with { AssignmentId = "A2", Kind = Kind.Actual, Allocation = 1m },
            ],
        };

        Assert.Empty(Analyse(Team, new PlanSource("e1.xlsx", plan)).Issues);
    }
}
