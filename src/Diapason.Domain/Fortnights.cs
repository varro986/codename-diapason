using System.Globalization;

namespace Diapason.Domain;

public static class Fortnights
{
    public static bool IsStart(DateOnly date) => date.Day is 1 or 16;

    public static DateOnly Next(DateOnly start) =>
        start.Day == 1 ? new DateOnly(start.Year, start.Month, 16) : new DateOnly(start.Year, start.Month, 1).AddMonths(1);

    /// <summary>Dates as written in issues and on screen, in both languages (spec 001, criterion 13).</summary>
    public static string Format(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
