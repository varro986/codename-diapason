namespace Diapason.Domain;

/// <summary>
/// Sheet and column names of specs/data-contract.md: the only place they are written, used by the reader, the writers
/// and the checks that name where to fix a value.
/// </summary>
public static class DataContract
{
    public static class People
    {
        public const string Sheet = "People";
        public const string PersonId = "PersonId";
        public const string DisplayName = "DisplayName";
    }

    public static class LevelHistory
    {
        public const string Sheet = "LevelHistory";
        public const string PersonId = "PersonId";
        public const string Level = "Level";
        public const string ValidFrom = "ValidFrom";
    }

    public static class LevelRates
    {
        public const string Sheet = "LevelRates";
        public const string Level = "Level";
        public const string ValidFrom = "ValidFrom";
        public const string ChargeableRate = "ChargeableRate";
        public const string BusinessDevelopmentRate = "BusinessDevelopmentRate";
        public const string ExtraTimeShare = "ExtraTimeShare";
    }

    public static class PersonRates
    {
        public const string Sheet = "PersonRates";
        public const string PersonId = "PersonId";
        public const string ValidFrom = "ValidFrom";
        public const string ChargeableRate = "ChargeableRate";
        public const string BusinessDevelopmentRate = "BusinessDevelopmentRate";
    }

    public static class Calendar
    {
        public const string Sheet = "Calendar";
        public const string FortnightStart = "FortnightStart";
        public const string WorkingHours = "WorkingHours";
    }

    public static class Engagement
    {
        public const string Sheet = "Engagement";
        public const string EngagementId = "EngagementId";
        public const string Name = "Name";
    }

    public static class Areas
    {
        public const string Sheet = "Areas";
        public const string Area = "Area";
        public const string SoldValue = "SoldValue";
    }

    public static class Assignments
    {
        public const string Sheet = "Assignments";
        public const string AssignmentId = "AssignmentId";
        public const string PersonId = "PersonId";
        public const string Level = "Level";
        public const string Area = "Area";
        public const string Role = "Role";
        public const string Kind = "Kind";
        public const string WorkType = "WorkType";
        public const string Allocation = "Allocation";
        public const string FirstFortnight = "FirstFortnight";
        public const string LastFortnight = "LastFortnight";
    }
}
