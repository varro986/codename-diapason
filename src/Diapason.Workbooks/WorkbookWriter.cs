using ClosedXML.Excel;
using Diapason.Domain;

namespace Diapason.Workbooks;

/// <summary>
/// Writes a registry or a plan with the layout the reader expects: empty ones are the templates, the sample data the
/// synthetic workbooks. Values only, no formulas, so a workbook never opened in Excel reads the same. Never overwrites.
/// </summary>
public static class WorkbookWriter
{
    private const string DateFormat = "dd/mm/yyyy";

    public static void WriteRegistry(string path, Registry registry)
    {
        using var book = new XLWorkbook();
        Write(book, Layout.People, registry.People, p => [p.PersonId, p.DisplayName]);
        Write(book, Layout.LevelHistory, registry.LevelHistory, l => [l.PersonId, l.Level, l.ValidFrom]);
        Write(book, Layout.LevelRates, registry.LevelRates,
            r => [r.Level, r.ValidFrom, r.ChargeableRate, r.BusinessDevelopmentRate, r.ExtraTimeShare]);
        Write(book, Layout.PersonRates, registry.PersonRates,
            r => [r.PersonId, r.ValidFrom, r.ChargeableRate, r.BusinessDevelopmentRate]);
        Write(book, Layout.Calendar, registry.Calendar, f => [f.Start, f.WorkingHours]);
        Save(book, path);
    }

    public static void WritePlan(string path, Plan plan)
    {
        using var book = new XLWorkbook();
        Write<Plan>(book, Layout.Engagement, plan.EngagementId.Length == 0 ? [] : [plan], p => [p.EngagementId, p.Name]);
        Write(book, Layout.Areas, plan.Areas, a => [a.Name, a.SoldValue]);
        Write(book, Layout.Assignments, plan.Assignments, a =>
        [
            a.AssignmentId, a.PersonId, a.Level, a.Area, a.Role, a.Kind.ToString(), a.WorkType.ToString(), a.Allocation,
            a.FirstFortnight, a.LastFortnight,
        ]);
        Save(book, path);
    }

    private static void Write<T>(XLWorkbook book, Layout.Sheet layout, IEnumerable<T> rows, Func<T, object?[]> values)
    {
        var sheet = book.AddWorksheet(layout.Name);
        for (var c = 0; c < layout.Columns.Length; c++)
            sheet.Cell(1, c + 1).Value = layout.Columns[c];
        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);

        var r = 2;
        foreach (var row in rows)
        {
            var cells = values(row);
            for (var c = 0; c < cells.Length; c++)
            {
                var cell = sheet.Cell(r, c + 1);
                switch (cells[c])
                {
                    case null: break;
                    case DateOnly d:
                        cell.Value = d.ToDateTime(TimeOnly.MinValue);
                        cell.Style.DateFormat.Format = DateFormat;
                        break;
                    case decimal n: cell.Value = n; break;
                    case var v: cell.Value = v.ToString(); break;
                }
            }
            r++;
        }
        sheet.Columns(1, layout.Columns.Length).AdjustToContents();
    }

    private static void Save(XLWorkbook book, string path)
    {
        // CreateNew: an existing file is never replaced (spec 001, criterion 15).
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        book.SaveAs(stream);
    }
}
