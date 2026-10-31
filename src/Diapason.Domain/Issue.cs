namespace Diapason.Domain;

public enum Severity { Error, Warning }

/// <summary>What went wrong; the UI turns the code and its arguments into a message in the user's language.</summary>
public enum IssueCode
{
    FolderMissing,
    RegistryMissing,
    WorkbookUnreadable,
    SheetMissing,
    ColumnMissing,
    ValueRequired,
    NotANumber,
    NotADate,
    NotAllowed,
    Duplicate,
    UnknownPerson,
    UnknownLevel,
    UnknownArea,
    OutOfRange,
    NotAFortnightStart,
    CalendarGap,
    NotInCalendar,
    LastBeforeFirst,
    PersonOrLevel,
    OneRowExpected,
    DuplicateEngagement,
    NoLevelInForce,
    NoRateInForce,
    OverAllocation,
}

public sealed record SourceLocation(string Workbook, string? Sheet = null, int? Row = null, string? Column = null);

public sealed record Issue(Severity Severity, IssueCode Code, SourceLocation? Location, IReadOnlyList<string> Args)
{
    public static Issue Error(IssueCode code, SourceLocation? location, params string[] args) =>
        new(Severity.Error, code, location, args);

    public static Issue Warning(IssueCode code, SourceLocation? location, params string[] args) =>
        new(Severity.Warning, code, location, args);
}
