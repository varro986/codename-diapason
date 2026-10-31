namespace Diapason.Workbooks.Tests;

/// <summary>A folder of its own under the system temp folder; removed with the files the test wrote there.</summary>
internal sealed class TempFolder : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "diapason-tests", Guid.NewGuid().ToString("N"));

    public TempFolder() => Directory.CreateDirectory(Path);

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        // Only the files of this test's own folder, then the folder itself: never a recursive delete.
        if (!Directory.Exists(Path)) return;
        foreach (var file in Directory.EnumerateFiles(Path)) System.IO.File.Delete(file);
        Directory.Delete(Path);
    }
}
