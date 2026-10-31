namespace Diapason.Domain;

/// <summary>
/// Invented people and engagements: the example of specs/data-contract.md, used by the tests and by the sample-data
/// command. Never real data.
/// </summary>
public static class SampleData
{
    public static DateOnly D(int year, int month, int day) => new(year, month, day);

    public static Registry Team { get; } = new(
        People:
        [
            new("P1", "Alice Example", 2),
            new("P2", "Bruno Sample", 3),
        ],
        LevelHistory:
        [
            new("P1", "L6", D(2026, 1, 1), 2),
            new("P2", "L9", D(2026, 1, 1), 3),
            new("P2", "L8", D(2027, 1, 1), 4),
        ],
        LevelRates:
        [
            new("L6", D(2026, 1, 1), 90m, 70m, 0m, 2),
            new("L8", D(2026, 1, 1), 60m, 48m, 0m, 3),
            new("L9", D(2026, 1, 1), 40m, 32m, 0.2m, 4),
        ],
        PersonRates: [],
        Calendar:
        [
            new(D(2026, 12, 1), 88m, 2),
            new(D(2026, 12, 16), 72m, 3),
            new(D(2027, 1, 1), 64m, 4),
            new(D(2027, 1, 16), 88m, 5),
        ]);

    /// <summary>The example plan: its forecast of area Core is 80 hours, 10 days, 3,840 (spec 001, criterion 1).</summary>
    public static Plan Example { get; } = new("E1", "Example engagement",
        [new Area("Core", 50_000m, 2)],
        [
            new("A1", "P2", null, "Core", "Developer", Kind.Forecast, WorkType.Chargeable, 0.5m,
                D(2026, 12, 1), D(2026, 12, 16), 2),
        ]);

    /// <summary>A second plan, with an actual, a business development row and an open position.</summary>
    public static Plan Second { get; } = new("E2", "Second engagement",
        [new Area("Governance", 20_000m, 2), new Area("Delivery", 30_000m, 3)],
        [
            new("A1", "P1", null, "Governance", "Lead", Kind.Forecast, WorkType.Chargeable, 0.25m,
                D(2026, 12, 1), D(2027, 1, 16), 2),
            new("A2", "P1", null, "Governance", "Lead", Kind.Actual, WorkType.Chargeable, 0.25m,
                D(2026, 12, 1), D(2026, 12, 16), 3),
            new("A3", "P1", null, "Delivery", "Lead", Kind.Forecast, WorkType.BusinessDevelopment, 0.1m,
                D(2026, 12, 1), D(2026, 12, 16), 4),
            new("A4", null, "L9", "Delivery", "Developer", Kind.Forecast, WorkType.Chargeable, 1m,
                D(2027, 1, 1), D(2027, 1, 16), 5),
        ]);

    /// <summary>A plan with errors: an unknown person and an area that is not in its Areas (spec 001, criterion 7).</summary>
    public static Plan Broken { get; } = new("E9", "Broken engagement",
        [new Area("Core", 10_000m, 2)],
        [
            new("A1", "P404", null, "Core", "Developer", Kind.Forecast, WorkType.Chargeable, 0.5m,
                D(2026, 12, 1), D(2026, 12, 16), 2),
            new("A2", "P1", null, "Nowhere", "Developer", Kind.Forecast, WorkType.Chargeable, 0.5m,
                D(2026, 12, 1), D(2026, 12, 16), 3),
        ]);
}
