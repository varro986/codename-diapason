namespace Diapason.Domain;

/// <summary>The cost of one assignment in one fortnight.</summary>
public sealed record CostRow(
    string EngagementId,
    string AssignmentId,
    string? PersonId,
    string Level,
    string Area,
    Kind Kind,
    DateOnly Fortnight,
    decimal Allocation,
    decimal Hours,
    decimal Days,
    decimal Cost);

/// <summary>The calculation of specs/data-contract.md, on a plan that has passed <see cref="Rules.CheckPlan"/>.</summary>
public static class Costing
{
    public static IReadOnlyList<CostRow> Cost(Plan plan, Registry registry, decimal hoursPerDay)
    {
        var book = new RateBook(registry);
        var calendar = registry.Calendar.OrderBy(f => f.Start).ToList();
        var rows = new List<CostRow>();

        foreach (var a in plan.Assignments)
            foreach (var f in calendar.Where(f => f.Start >= a.FirstFortnight && f.Start <= a.LastFortnight))
            {
                var level = a.Level ?? book.LevelOf(a.PersonId!, f.Start)!;
                var levelRate = book.LevelRate(level, f.Start)!;
                var personRate = a.PersonId is null ? null : book.PersonRate(a.PersonId, f.Start);
                var baseRate = a.WorkType == WorkType.Chargeable
                    ? personRate?.ChargeableRate ?? levelRate.ChargeableRate
                    : personRate?.BusinessDevelopmentRate ?? levelRate.BusinessDevelopmentRate;

                var hours = f.WorkingHours * a.Allocation;
                rows.Add(new CostRow(plan.EngagementId, a.AssignmentId, a.PersonId, level, a.Area, a.Kind, f.Start,
                    a.Allocation, hours, hours / hoursPerDay, hours * baseRate * (1 + levelRate.ExtraTimeShare)));
            }
        return rows;
    }
}
