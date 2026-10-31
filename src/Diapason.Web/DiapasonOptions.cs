using Diapason.Workbooks;

namespace Diapason.Web;

/// <summary>Section "Diapason" of appsettings.json, where the values are; the data folder can be overridden by
/// DIAPASON_DATA_FOLDER.</summary>
public sealed class DiapasonOptions
{
    public const string Section = "Diapason";
    public const string DataFolderVariable = "DIAPASON_DATA_FOLDER";

    public string DataFolder { get; set; } = "";
    public string RegistryFileName { get; set; } = "";
    public decimal HoursPerDay { get; set; }
    public int Port { get; set; }

    public DataFolderOptions ToLoader() => new(DataFolder, RegistryFileName, HoursPerDay);
}
