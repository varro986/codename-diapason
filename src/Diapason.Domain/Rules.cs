using C = Diapason.Domain.DataContract;

namespace Diapason.Domain;

/// <summary>The rules of specs/data-contract.md that go beyond the type of a single cell.</summary>
public static class Rules
{
    public static IReadOnlyList<Issue> CheckRegistry(Registry registry, string workbook)
    {
        var issues = new List<Issue>();
        SourceLocation At(string sheet, int row, string column) => new(workbook, sheet, row, column);
        void Error(IssueCode code, SourceLocation at, params object[] args) => issues.Add(Issue.Error(code, at, args));
        void Positive(decimal value, SourceLocation at)
        {
            if (value <= 0) Error(IssueCode.OutOfRange, at, value, "> 0");
        }

        var people = registry.People.Select(p => p.PersonId).ToHashSet();
        var levels = registry.LevelRates.Select(r => r.Level).ToHashSet();

        foreach (var p in Duplicates(registry.People, p => p.PersonId))
            Error(IssueCode.Duplicate, At(C.People.Sheet, p.Row, C.People.PersonId), p.PersonId);

        foreach (var l in registry.LevelHistory)
        {
            if (!people.Contains(l.PersonId))
                Error(IssueCode.UnknownPerson, At(C.LevelHistory.Sheet, l.Row, C.LevelHistory.PersonId), l.PersonId);
            if (!levels.Contains(l.Level))
                Error(IssueCode.UnknownLevel, At(C.LevelHistory.Sheet, l.Row, C.LevelHistory.Level), l.Level);
        }
        foreach (var l in Duplicates(registry.LevelHistory, l => (l.PersonId, l.ValidFrom)))
            Error(IssueCode.Duplicate, At(C.LevelHistory.Sheet, l.Row, C.LevelHistory.ValidFrom),
                $"{l.PersonId} {Fortnights.Format(l.ValidFrom)}");

        foreach (var r in registry.LevelRates)
        {
            Positive(r.ChargeableRate, At(C.LevelRates.Sheet, r.Row, C.LevelRates.ChargeableRate));
            Positive(r.BusinessDevelopmentRate, At(C.LevelRates.Sheet, r.Row, C.LevelRates.BusinessDevelopmentRate));
            if (r.ExtraTimeShare < 0)
                Error(IssueCode.OutOfRange, At(C.LevelRates.Sheet, r.Row, C.LevelRates.ExtraTimeShare),
                    r.ExtraTimeShare, ">= 0");
        }
        foreach (var r in Duplicates(registry.LevelRates, r => (r.Level, r.ValidFrom)))
            Error(IssueCode.Duplicate, At(C.LevelRates.Sheet, r.Row, C.LevelRates.ValidFrom),
                $"{r.Level} {Fortnights.Format(r.ValidFrom)}");

        foreach (var r in registry.PersonRates)
        {
            if (!people.Contains(r.PersonId))
                Error(IssueCode.UnknownPerson, At(C.PersonRates.Sheet, r.Row, C.PersonRates.PersonId), r.PersonId);
            Positive(r.ChargeableRate, At(C.PersonRates.Sheet, r.Row, C.PersonRates.ChargeableRate));
            Positive(r.BusinessDevelopmentRate, At(C.PersonRates.Sheet, r.Row, C.PersonRates.BusinessDevelopmentRate));
        }
        foreach (var r in Duplicates(registry.PersonRates, r => (r.PersonId, r.ValidFrom)))
            Error(IssueCode.Duplicate, At(C.PersonRates.Sheet, r.Row, C.PersonRates.ValidFrom),
                $"{r.PersonId} {Fortnights.Format(r.ValidFrom)}");

        foreach (var f in registry.Calendar)
        {
            if (!Fortnights.IsStart(f.Start))
                Error(IssueCode.NotAFortnightStart, At(C.Calendar.Sheet, f.Row, C.Calendar.FortnightStart),
                    f.Start);
            if (f.WorkingHours < 0)
                Error(IssueCode.OutOfRange, At(C.Calendar.Sheet, f.Row, C.Calendar.WorkingHours),
                    f.WorkingHours, ">= 0");
        }
        foreach (var f in Duplicates(registry.Calendar, f => f.Start))
            Error(IssueCode.Duplicate, At(C.Calendar.Sheet, f.Row, C.Calendar.FortnightStart), f.Start);

        // A gap would silently drop the hours of the missing fortnight from every assignment that spans it.
        var starts = registry.Calendar.Where(f => Fortnights.IsStart(f.Start)).DistinctBy(f => f.Start)
            .OrderBy(f => f.Start).ToList();
        for (var i = 1; i < starts.Count; i++)
        {
            var expected = Fortnights.Next(starts[i - 1].Start);
            if (starts[i].Start != expected)
                Error(IssueCode.CalendarGap, At(C.Calendar.Sheet, starts[i].Row, C.Calendar.FortnightStart),
                    expected, starts[i].Start);
        }

        return issues;
    }

    /// <summary>Checks a plan against a registry that has already passed <see cref="CheckRegistry"/>.</summary>
    public static IReadOnlyList<Issue> CheckPlan(Plan plan, Registry registry, string workbook)
    {
        var issues = new List<Issue>();
        SourceLocation At(string sheet, int row, string column) => new(workbook, sheet, row, column);
        void Error(IssueCode code, SourceLocation at, params object[] args) => issues.Add(Issue.Error(code, at, args));

        var people = registry.People.Select(p => p.PersonId).ToHashSet();
        var levels = registry.LevelRates.Select(r => r.Level).ToHashSet();
        var calendar = registry.Calendar.Select(f => f.Start).Order().ToList();
        var inCalendar = calendar.ToHashSet();
        var areas = plan.Areas.Select(a => a.Name).ToHashSet();
        var book = new RateBook(registry);

        foreach (var a in plan.Areas.Where(a => a.SoldValue < 0))
            Error(IssueCode.OutOfRange, At(C.Areas.Sheet, a.Row, C.Areas.SoldValue), a.SoldValue, ">= 0");
        foreach (var a in Duplicates(plan.Areas, a => a.Name))
            Error(IssueCode.Duplicate, At(C.Areas.Sheet, a.Row, C.Areas.Area), a.Name);
        foreach (var a in Duplicates(plan.Assignments, a => a.AssignmentId))
            Error(IssueCode.Duplicate, At(C.Assignments.Sheet, a.Row, C.Assignments.AssignmentId), a.AssignmentId);

        foreach (var a in plan.Assignments)
        {
            SourceLocation Cell(string column) => At(C.Assignments.Sheet, a.Row, column);
            var before = issues.Count;

            if ((a.PersonId is null) == (a.Level is null))
                Error(IssueCode.PersonOrLevel, Cell(C.Assignments.PersonId));
            else if (a.PersonId is not null && !people.Contains(a.PersonId))
                Error(IssueCode.UnknownPerson, Cell(C.Assignments.PersonId), a.PersonId);
            else if (a.Level is not null && !levels.Contains(a.Level))
                Error(IssueCode.UnknownLevel, Cell(C.Assignments.Level), a.Level);

            if (!areas.Contains(a.Area))
                Error(IssueCode.UnknownArea, Cell(C.Assignments.Area), a.Area);
            if (a.Allocation <= 0 || a.Allocation > 1)
                Error(IssueCode.OutOfRange, Cell(C.Assignments.Allocation), a.Allocation, "> 0, <= 1");
            if (!inCalendar.Contains(a.FirstFortnight))
                Error(IssueCode.NotInCalendar, Cell(C.Assignments.FirstFortnight), a.FirstFortnight);
            if (!inCalendar.Contains(a.LastFortnight))
                Error(IssueCode.NotInCalendar, Cell(C.Assignments.LastFortnight), a.LastFortnight);
            if (a.LastFortnight < a.FirstFortnight)
                Error(IssueCode.LastBeforeFirst, Cell(C.Assignments.LastFortnight),
                    a.FirstFortnight, a.LastFortnight);

            // Levels and rates in force are checked only on an otherwise valid row: one cause, one message.
            if (issues.Count > before) continue;
            foreach (var start in calendar.Where(s => s >= a.FirstFortnight && s <= a.LastFortnight))
            {
                var level = a.Level ?? book.LevelOf(a.PersonId!, start);
                if (level is null)
                {
                    Error(IssueCode.NoLevelInForce, Cell(C.Assignments.PersonId), a.PersonId!, start);
                    break;
                }
                if (book.LevelRate(level, start) is null)
                {
                    Error(IssueCode.NoRateInForce, Cell(C.Assignments.FirstFortnight), level, start);
                    break;
                }
            }
        }
        return issues;
    }

    private static IEnumerable<T> Duplicates<T, TKey>(IEnumerable<T> rows, Func<T, TKey> key) =>
        rows.GroupBy(key).SelectMany(g => g.Skip(1));
}
