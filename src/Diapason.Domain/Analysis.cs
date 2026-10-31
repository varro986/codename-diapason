using C = Diapason.Domain.DataContract;

namespace Diapason.Domain;

public sealed record AnalysisResult(Overview? Overview, IReadOnlyList<Issue> Issues);

/// <summary>From a registry and the plans read from the data folder to the overview and every issue found.</summary>
public static class Analysis
{
    public static AnalysisResult Analyse(string registryWorkbook, Registry registry, IReadOnlyList<PlanSource> plans,
        decimal hoursPerDay)
    {
        var issues = Rules.CheckRegistry(registry, registryWorkbook).ToList();
        // Every plan depends on the registry: with an error there, no number would be right (spec 001, criterion 8).
        if (issues.Any(i => i.Severity == Severity.Error))
            return new AnalysisResult(null, issues);

        var seen = new Dictionary<string, string>();
        var engagements = new List<EngagementEconomics>();
        var allRows = new List<(CostRow Row, string Workbook)>();

        foreach (var source in plans.OrderBy(p => p.Workbook, StringComparer.OrdinalIgnoreCase))
        {
            var planIssues = Rules.CheckPlan(source.Plan, registry, source.Workbook).ToList();
            if (seen.TryGetValue(source.Plan.EngagementId, out var first))
                planIssues.Add(Issue.Error(IssueCode.DuplicateEngagement,
                    new SourceLocation(source.Workbook, C.Engagement.Sheet, source.Plan.Row, C.Engagement.EngagementId),
                    source.Plan.EngagementId, first));
            issues.AddRange(planIssues);
            if (planIssues.Any(i => i.Severity == Severity.Error)) continue;

            seen[source.Plan.EngagementId] = source.Workbook;
            var rows = Costing.Cost(source.Plan, registry, hoursPerDay);
            engagements.Add(Economics.OfPlan(source, rows));
            allRows.AddRange(rows.Select(r => (r, source.Workbook)));
        }

        issues.AddRange(OverAllocations(allRows));
        return new AnalysisResult(Economics.Of(engagements), issues);
    }

    /// <summary>A person over 1 in one fortnight, over all plans and per kind (a warning: the numbers still hold).</summary>
    private static IEnumerable<Issue> OverAllocations(IEnumerable<(CostRow Row, string Workbook)> rows) =>
        rows.Where(r => r.Row.PersonId is not null)
            .GroupBy(r => (r.Row.PersonId, r.Row.Kind, r.Row.Fortnight))
            .Where(g => g.Sum(r => r.Row.Allocation) > 1)
            .OrderBy(g => g.Key.PersonId).ThenBy(g => g.Key.Fortnight)
            .Select(g => Issue.Warning(IssueCode.OverAllocation, null,
                g.Key.PersonId!, g.Key.Fortnight, g.Key.Kind, g.Sum(r => r.Row.Allocation),
                string.Join(", ", g.Select(r => r.Workbook).Distinct())));
}
