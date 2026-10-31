using Diapason.Workbooks;

namespace Diapason.Web;

/// <summary>Section "Diapason" of appsettings.json; the data folder can be overridden by DIAPASON_DATA_FOLDER.</summary>
public sealed class DiapasonOptions
{
    public const string Section = "Diapason";
    public const string DataFolderVariable = "DIAPASON_DATA_FOLDER";

    public string DataFolder { get; set; } = "data";
    public string RegistryFileName { get; set; } = "registry.xlsx";
    public decimal HoursPerDay { get; set; } = 8m;
    public int Port { get; set; } = 5188;

    public DataFolderOptions ToLoader() => new(DataFolder, RegistryFileName, HoursPerDay);
}
