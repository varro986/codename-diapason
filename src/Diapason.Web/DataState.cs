using Diapason.Workbooks;
using Microsoft.Extensions.Options;

namespace Diapason.Web;

/// <summary>The last reading of the data folder, shared by every page; replaced only by <see cref="Reload"/>.</summary>
public sealed class DataState(IOptions<DiapasonOptions> options, TimeProvider clock, ILogger<DataState> logger)
{
    private readonly Lock _gate = new();
    private LoadResult? _current;

    public event Action? Changed;

    public LoadResult Current
    {
        get
        {
            lock (_gate) return _current ??= Load();
        }
    }

    public LoadResult Reload()
    {
        LoadResult result;
        lock (_gate) result = _current = Load();
        Changed?.Invoke();
        return result;
    }

    private LoadResult Load()
    {
        var result = DataFolderLoader.Load(options.Value.ToLoader(), clock);
        logger.LogInformation(
            "Data folder {Folder} read: {Workbooks} workbooks, {Engagements} engagements shown, {Errors} errors, {Warnings} warnings",
            Path.GetFullPath(result.Folder), result.Workbooks.Count, result.Overview?.Engagements.Count ?? 0,
            result.Errors, result.Warnings);
        foreach (var issue in result.Issues)
            logger.LogWarning("{Severity} {Code} at {Location}: {Args}", issue.Severity, issue.Code, issue.Location,
                string.Join(" | ", issue.Args));
        return result;
    }
}
