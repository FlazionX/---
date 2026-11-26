using System.Text.Json;

namespace Sector;

public sealed class SectorPreferences
{
    public int AutosaveSeconds { get; set; } = 2;
    public string DefaultResolution { get; set; } = "1280 × 720";
    public bool ShowGridByDefault { get; set; } = true;
    public bool ConfirmBeforeClearingHistory { get; set; } = true;

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Sector", "settings.json");

    public static SectorPreferences Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new SectorPreferences();
            var value = JsonSerializer.Deserialize<SectorPreferences>(File.ReadAllText(FilePath));
            return value ?? new SectorPreferences();
        }
        catch { return new SectorPreferences(); }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this,
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
