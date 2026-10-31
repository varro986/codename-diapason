using Diapason.Domain;
using Microsoft.Extensions.Localization;

namespace Diapason.Web;

/// <summary>How values are written on screen: always in the Italian format (spec 001, criterion 13).</summary>
public static class Show
{
    public static string Money(decimal value) => value.ToString("#,##0 €", DiapasonApp.Numbers);

    public static string Days(decimal value) => value.ToString("#,##0.0", DiapasonApp.Numbers);

    public static string Share(decimal? value) => value is { } v ? v.ToString("0.0 %", DiapasonApp.Numbers) : "–";

    public static string Ratio(decimal? value) => value is { } v ? v.ToString("0.00", DiapasonApp.Numbers) : "–";

    public static string Moment(DateTimeOffset value) => value.ToString("dd/MM/yyyy HH:mm", DiapasonApp.Numbers);

    public static string Message(IStringLocalizer<Texts> texts, Issue issue) =>
        texts[$"Issue_{issue.Code}", [.. issue.Args.Select(a => Argument(texts, a))]];

    private static string Argument(IStringLocalizer<Texts> texts, object value) => value switch
    {
        decimal n => n.ToString("0.##", DiapasonApp.Numbers),
        DateOnly d => Fortnights.Format(d),
        Kind k => texts[k.ToString()],
        _ => value.ToString() ?? "",
    };
}
