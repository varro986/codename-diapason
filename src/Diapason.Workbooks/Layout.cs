using Diapason.Domain;
using C = Diapason.Domain.DataContract;

namespace Diapason.Workbooks;

/// <summary>The sheets of each workbook and their columns in order: what the reader requires and the writers write.</summary>
internal static class Layout
{
    public sealed record Sheet(string Name, string[] Columns, bool Optional = false);

    public static readonly Sheet People = new(C.People.Sheet, [C.People.PersonId, C.People.DisplayName]);

    public static readonly Sheet LevelHistory = new(C.LevelHistory.Sheet,
        [C.LevelHistory.PersonId, C.LevelHistory.Level, C.LevelHistory.ValidFrom]);

    public static readonly Sheet LevelRates = new(C.LevelRates.Sheet,
    [
        C.LevelRates.Level, C.LevelRates.ValidFrom, C.LevelRates.ChargeableRate, C.LevelRates.BusinessDevelopmentRate,
        C.LevelRates.ExtraTimeShare,
    ]);

    public static readonly Sheet PersonRates = new(C.PersonRates.Sheet,
        [C.PersonRates.PersonId, C.PersonRates.ValidFrom, C.PersonRates.ChargeableRate, C.PersonRates.BusinessDevelopmentRate],
        Optional: true);

    public static readonly Sheet Calendar = new(C.Calendar.Sheet, [C.Calendar.FortnightStart, C.Calendar.WorkingHours]);

    public static readonly Sheet Engagement = new(C.Engagement.Sheet, [C.Engagement.EngagementId, C.Engagement.Name]);

    public static readonly Sheet Areas = new(C.Areas.Sheet, [C.Areas.Area, C.Areas.SoldValue]);

    public static readonly Sheet Assignments = new(C.Assignments.Sheet,
    [
        C.Assignments.AssignmentId, C.Assignments.PersonId, C.Assignments.Level, C.Assignments.Area, C.Assignments.Role,
        C.Assignments.Kind, C.Assignments.WorkType, C.Assignments.Allocation, C.Assignments.FirstFortnight,
        C.Assignments.LastFortnight,
    ]);

    public static readonly Sheet[] Registry = [People, LevelHistory, LevelRates, PersonRates, Calendar];

    public static readonly Sheet[] Plan = [Engagement, Areas, Assignments];

    public static IReadOnlyList<string> Values<T>() where T : struct, Enum => Enum.GetNames<T>();
}
