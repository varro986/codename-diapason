using System.Globalization;
using ClosedXML.Excel;
using Diapason.Domain;

namespace Diapason.Workbooks;

/// <summary>Reads the rows of one sheet by header name; every cell that breaks its type becomes an issue.</summary>
internal sealed class SheetReader
{
    private static readonly string[] DateFormats = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"];

    private readonly string _workbook;
    private readonly List<Issue> _issues;
    private readonly IXLWorksheet? _sheet;
    private readonly Dictionary<string, int> _columns = new(StringComparer.Ordinal);
    private readonly Layout.Sheet _layout;

    public SheetReader(XLWorkbook book, Layout.Sheet layout, string workbook, List<Issue> issues)
    {
        _layout = layout;
        _workbook = workbook;
        _issues = issues;
        if (!book.TryGetWorksheet(layout.Name, out var sheet))
        {
            if (!layout.Optional) issues.Add(Issue.Error(IssueCode.SheetMissing, new(workbook), layout.Name));
            return;
        }

        foreach (var cell in sheet.Row(1).CellsUsed())
            _columns.TryAdd(cell.GetString().Trim(), cell.Address.ColumnNumber);
        var missing = layout.Columns.Where(c => !_columns.ContainsKey(c)).ToList();
        foreach (var column in missing)
            issues.Add(Issue.Error(IssueCode.ColumnMissing, new(workbook, layout.Name), column));
        if (missing.Count == 0) _sheet = sheet;
    }

    /// <summary>The rows with at least one value, read by <paramref name="read"/>; a row with an issue is left out.</summary>
    public List<T> Rows<T>(Func<Row, T> read)
    {
        var result = new List<T>();
        if (_sheet is null) return result;

        var last = _sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var number = 2; number <= last; number++)
        {
            var row = new Row(this, _sheet.Row(number), number);
            if (_layout.Columns.All(c => row.OptionalText(c) is null)) continue;
            var before = _issues.Count;
            var value = read(row);
            if (_issues.Count == before) result.Add(value);
        }
        return result;
    }

    internal sealed class Row(SheetReader reader, IXLRow row, int number)
    {
        public int Line => number;

        public XLCellValue Value(string column)
        {
            var cell = row.Cell(reader._columns[column]);
            if (!cell.HasFormula) return cell.Value;
            // Excel stores the result of each formula; a workbook saved by another tool may not, so it is computed.
            if (!cell.CachedValue.IsBlank) return cell.CachedValue;
            try
            {
                return cell.Value;
            }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                return Blank.Value;
            }
        }

        public string? OptionalText(string column)
        {
            var value = Value(column);
            if (value.IsBlank) return null;
            var text = value.IsText ? value.GetText() : value.ToString(CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        public string Text(string column)
        {
            var text = OptionalText(column);
            if (text is null) Report(IssueCode.ValueRequired, column);
            return text ?? "";
        }

        public decimal Number(string column)
        {
            var value = Value(column);
            if (value.IsNumber && Math.Abs(value.GetNumber()) < 1e15) return (decimal)value.GetNumber();
            Report(IsEmpty(value) ? IssueCode.ValueRequired : IssueCode.NotANumber, column, Shown(value));
            return 0;
        }

        public DateOnly Date(string column)
        {
            var value = Value(column);
            // A plain number is refused: in a date column it is almost always a value typed in the wrong cell.
            if (value.IsDateTime) return DateOnly.FromDateTime(value.GetDateTime());
            if (value.IsText && DateOnly.TryParseExact(value.GetText().Trim(), DateFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
                return date;
            Report(IsEmpty(value) ? IssueCode.ValueRequired : IssueCode.NotADate, column, Shown(value));
            return default;
        }

        public T Choice<T>(string column) where T : struct, Enum
        {
            var text = Text(column);
            if (text.Length == 0) return default;
            if (Enum.TryParse<T>(text, ignoreCase: true, out var choice) && Enum.IsDefined(choice) && !int.TryParse(text, out _))
                return choice;
            Report(IssueCode.NotAllowed, column, text, string.Join(", ", Enum.GetNames<T>()));
            return default;
        }

        private void Report(IssueCode code, string column, params object[] args) =>
            reader._issues.Add(Issue.Error(code, new(reader._workbook, reader._layout.Name, number, column), args));

        private static bool IsEmpty(XLCellValue value) => value.IsBlank || (value.IsText && string.IsNullOrWhiteSpace(value.GetText()));

        private static string Shown(XLCellValue value) => value.IsBlank ? "" : value.ToString(CultureInfo.InvariantCulture);
    }
}
