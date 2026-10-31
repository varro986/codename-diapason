using Diapason.Domain;
using static Diapason.Domain.SampleData;

namespace Diapason.Domain.Tests;

// Spec 001, criteria 1 to 4.
public class CostingTests
{
    private const decimal HoursPerDay = 8m;

    private static Assignment Bruno(DateOnly first, DateOnly last, WorkType workType = WorkType.Chargeable) =>
        new("A1", "P2", null, "Core", "Developer", Kind.Forecast, workType, 0.5m, first, last);

    private static Plan PlanOf(params Assignment[] assignments) => Example with { Assignments = assignments };

    [Fact]
    public void Example_of_the_data_contract_gives_80_hours_10_days_3840()
    {
        var rows = Costing.Cost(Example, Team, HoursPerDay);

        Assert.Equal([44m, 36m], rows.Select(r => r.Hours));
        Assert.Equal([2_112m, 1_728m], rows.Select(r => r.Cost));
        Assert.Equal(80m, rows.Sum(r => r.Hours));
        Assert.Equal(10m, rows.Sum(r => r.Days));
        Assert.Equal(3_840m, rows.Sum(r => r.Cost));
    }

    [Fact]
    public void Promotion_applies_from_the_fortnight_that_starts_on_its_date()
    {
        var rows = Costing.Cost(PlanOf(Bruno(D(2026, 12, 16), D(2027, 1, 1))), Team, HoursPerDay);

        Assert.Equal(["L9", "L8"], rows.Select(r => r.Level));
        Assert.Equal(36m * 40m * 1.2m, rows[0].Cost);
        Assert.Equal(32m * 60m, rows[1].Cost);
    }

    [Fact]
    public void Business_development_uses_the_second_rate()
    {
        var rows = Costing.Cost(PlanOf(Bruno(D(2026, 12, 1), D(2026, 12, 1), WorkType.BusinessDevelopment)),
            Team, HoursPerDay);

        Assert.Equal(44m * 32m * 1.2m, Assert.Single(rows).Cost);
    }

    [Fact]
    public void Person_rates_win_and_keep_the_extra_time_of_the_level()
    {
        var registry = Team with { PersonRates = [new PersonRate("P2", D(2026, 12, 16), 50m, 30m)] };

        var rows = Costing.Cost(Example, registry, HoursPerDay);

        Assert.Equal(44m * 40m * 1.2m, rows[0].Cost);
        Assert.Equal(36m * 50m * 1.2m, rows[1].Cost);
    }

    [Fact]
    public void Rates_of_a_level_change_on_their_date()
    {
        var registry = Team with { LevelRates = [.. Team.LevelRates, new LevelRate("L9", D(2026, 12, 16), 50m, 40m, 0.1m)] };

        var rows = Costing.Cost(Example, registry, HoursPerDay);

        Assert.Equal(44m * 40m * 1.2m, rows[0].Cost);
        Assert.Equal(36m * 50m * 1.1m, rows[1].Cost);
    }

    [Fact]
    public void Open_position_uses_the_rates_of_its_level()
    {
        var position = new Assignment("A1", null, "L6", "Core", "Lead", Kind.Forecast, WorkType.Chargeable, 1m,
            D(2026, 12, 1), D(2026, 12, 1));

        var row = Assert.Single(Costing.Cost(PlanOf(position), Team, HoursPerDay));

        Assert.Equal("L6", row.Level);
        Assert.Equal(88m * 90m, row.Cost);
    }

    [Fact]
    public void Hours_per_day_comes_from_the_caller()
    {
        var rows = Costing.Cost(Example, Team, hoursPerDay: 7.5m);

        Assert.Equal(80m / 7.5m, rows.Sum(r => r.Days));
    }
}
