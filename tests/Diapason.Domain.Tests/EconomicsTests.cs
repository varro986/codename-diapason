using Diapason.Domain;
using static Diapason.Domain.SampleData;

namespace Diapason.Domain.Tests;

// Spec 001, criteria 5 and 6.
public class EconomicsTests
{
    [Fact]
    public void Margin_share_and_coverage()
    {
        var f = new Figures(Sold: 50_000m, Cost: 40_000m, Hours: 0, Days: 0);

        Assert.Equal(10_000m, f.Margin);
        Assert.Equal(0.2m, f.MarginShare);
        Assert.Equal(1.25m, f.Coverage);
    }

    [Fact]
    public void Margin_share_is_empty_without_sold_and_coverage_without_cost()
    {
        Assert.Null(new Figures(0, 100m, 0, 0).MarginShare);
        Assert.Null(new Figures(100m, 0, 0, 0).Coverage);
    }

    [Fact]
    public void Totals_add_up_from_areas_to_engagements_to_overall()
    {
        var result = Analysis.Analyse("registry.xlsx", Team,
            [new PlanSource("e1.xlsx", Example), new PlanSource("e2.xlsx", Second)], 8m);

        var overview = Assert.IsType<Overview>(result.Overview);
        foreach (var e in overview.Engagements)
        {
            Assert.Equal(Figures.Sum(e.Areas.Select(a => a.Forecast)), e.Forecast);
            Assert.Equal(Figures.Sum(e.Areas.Select(a => a.Actual)), e.Actual);
        }
        Assert.Equal(Figures.Sum(overview.Engagements.Select(e => e.Forecast)), overview.Forecast);
        Assert.Equal(Figures.Sum(overview.Engagements.Select(e => e.Actual)), overview.Actual);
        Assert.Equal(100_000m, overview.Forecast.Sold);
        Assert.Equal(overview.Forecast.Sold, overview.Actual.Sold);
    }

    [Fact]
    public void Forecast_and_actual_are_kept_apart()
    {
        var result = Analysis.Analyse("registry.xlsx", Team, [new PlanSource("e2.xlsx", Second)], 8m);

        var governance = result.Overview!.Engagements.Single().Areas.Single(a => a.Area == "Governance");
        // P1 at L6, 90 per hour, 25%: forecast over four fortnights, actual over the first two.
        Assert.Equal((88m + 72m + 64m + 88m) * 0.25m * 90m, governance.Forecast.Cost);
        Assert.Equal((88m + 72m) * 0.25m * 90m, governance.Actual.Cost);
    }
}
