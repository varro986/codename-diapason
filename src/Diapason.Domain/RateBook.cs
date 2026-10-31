namespace Diapason.Domain;

/// <summary>Levels and rates of a registry indexed by key, to find the row in force on a date.</summary>
public sealed class RateBook
{
    private readonly Dictionary<string, List<LevelPeriod>> _levels;
    private readonly Dictionary<string, List<LevelRate>> _levelRates;
    private readonly Dictionary<string, List<PersonRate>> _personRates;

    public RateBook(Registry registry)
    {
        _levels = Index(registry.LevelHistory, l => l.PersonId, l => l.ValidFrom);
        _levelRates = Index(registry.LevelRates, r => r.Level, r => r.ValidFrom);
        _personRates = Index(registry.PersonRates, r => r.PersonId, r => r.ValidFrom);
    }

    public string? LevelOf(string personId, DateOnly date) => InForce(_levels, personId, date, l => l.ValidFrom)?.Level;

    public LevelRate? LevelRate(string level, DateOnly date) => InForce(_levelRates, level, date, r => r.ValidFrom);

    public PersonRate? PersonRate(string personId, DateOnly date) => InForce(_personRates, personId, date, r => r.ValidFrom);

    private static Dictionary<string, List<T>> Index<T>(IEnumerable<T> rows, Func<T, string> key, Func<T, DateOnly> from) =>
        rows.GroupBy(key).ToDictionary(g => g.Key, g => g.OrderBy(from).ToList());

    private static T? InForce<T>(Dictionary<string, List<T>> index, string key, DateOnly date, Func<T, DateOnly> from)
        where T : class =>
        index.TryGetValue(key, out var rows) ? rows.LastOrDefault(r => from(r) <= date) : null;
}
