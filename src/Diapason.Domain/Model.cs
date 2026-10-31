namespace Diapason.Domain;

public enum Kind { Forecast, Actual }

public enum WorkType { Chargeable, BusinessDevelopment }

// Row is the source row in the workbook (0 when the value does not come from a workbook), so that every rule
// broken later can still name where to fix it.

public sealed record Person(string PersonId, string DisplayName, int Row = 0);

public sealed record LevelPeriod(string PersonId, string Level, DateOnly ValidFrom, int Row = 0);

public sealed record LevelRate(
    string Level, DateOnly ValidFrom, decimal ChargeableRate, decimal BusinessDevelopmentRate, decimal ExtraTimeShare,
    int Row = 0);

public sealed record PersonRate(
    string PersonId, DateOnly ValidFrom, decimal ChargeableRate, decimal BusinessDevelopmentRate, int Row = 0);

public sealed record Fortnight(DateOnly Start, decimal WorkingHours, int Row = 0);

public sealed record Registry(
    IReadOnlyList<Person> People,
    IReadOnlyList<LevelPeriod> LevelHistory,
    IReadOnlyList<LevelRate> LevelRates,
    IReadOnlyList<PersonRate> PersonRates,
    IReadOnlyList<Fortnight> Calendar)
{
    public static Registry Empty { get; } = new([], [], [], [], []);
}

public sealed record Area(string Name, decimal SoldValue, int Row = 0);

/// <summary>An assignment of a person, or of an open position when <see cref="PersonId"/> is null.</summary>
public sealed record Assignment(
    string AssignmentId,
    string? PersonId,
    string? Level,
    string Area,
    string Role,
    Kind Kind,
    WorkType WorkType,
    decimal Allocation,
    DateOnly FirstFortnight,
    DateOnly LastFortnight,
    int Row = 0);

public sealed record Plan(string EngagementId, string Name, IReadOnlyList<Area> Areas, IReadOnlyList<Assignment> Assignments)
{
    public static Plan Empty { get; } = new("", "", [], []);
}

/// <summary>A plan together with the workbook it was read from.</summary>
public sealed record PlanSource(string Workbook, Plan Plan);
