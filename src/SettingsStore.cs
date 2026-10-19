using System.Text.Json;

namespace KeyboardSwitcher;

internal sealed record AppSettings(bool FunctionMode = true, bool Compatibility = true);
internal static class SettingsStore {
    internal static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ThinkPadKeyboardSwitcher");
    internal static string FileName => Path.Combine(Folder, "settings.json");
    internal static AppSettings Load() {
        if (!File.Exists(FileName)) return new();
        return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FileName)) ?? new();
    }
    internal static void Save(AppSettings settings) {
        Directory.CreateDirectory(Folder);
        string temporary = FileName + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, FileName, true);
    }
}
