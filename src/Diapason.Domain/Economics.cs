namespace Diapason.Domain;

/// <summary>Sold against cost for one scope and one <see cref="Kind"/> (spec 001, "Definitions").</summary>
public sealed record Figures(decimal Sold, decimal Cost, decimal Hours, decimal Days)
{
    public static Figures Zero { get; } = new(0, 0, 0, 0);

    public decimal Margin => Sold - Cost;

    public decimal? MarginShare => Sold == 0 ? null : Margin / Sold;

    public decimal? Coverage => Cost == 0 ? null : Sold / Cost;

    public static Figures operator +(Figures a, Figures b) =>
        new(a.Sold + b.Sold, a.Cost + b.Cost, a.Hours + b.Hours, a.Days + b.Days);

    public static Figures Sum(IEnumerable<Figures> figures) => figures.Aggregate(Zero, (a, b) => a + b);
}

public sealed record AreaEconomics(string Area, Figures Forecast, Figures Actual);

public sealed record EngagementEconomics(
    string EngagementId, string Name, string Workbook, IReadOnlyList<AreaEconomics> Areas, Figures Forecast, Figures Actual);

public sealed record Overview(IReadOnlyList<EngagementEconomics> Engagements, Figures Forecast, Figures Actual);

public static class Economics
{
    public static EngagementEconomics OfPlan(PlanSource source, IReadOnlyList<CostRow> rows)
    {
        var plan = source.Plan;
        var areas = plan.Areas.Select(area =>
        {
            Figures Of(Kind kind)
            {
                var r = rows.Where(c => c.Area == area.Name && c.Kind == kind).ToList();
                return new Figures(area.SoldValue, r.Sum(c => c.Cost), r.Sum(c => c.Hours), r.Sum(c => c.Days));
            }
            return new AreaEconomics(area.Name, Of(Kind.Forecast), Of(Kind.Actual));
        }).ToList();

        return new EngagementEconomics(plan.EngagementId, plan.Name, source.Workbook, areas,
            Figures.Sum(areas.Select(a => a.Forecast)), Figures.Sum(areas.Select(a => a.Actual)));
    }

    public static Overview Of(IReadOnlyList<EngagementEconomics> engagements) =>
        new(engagements, Figures.Sum(engagements.Select(e => e.Forecast)), Figures.Sum(engagements.Select(e => e.Actual)));
}
