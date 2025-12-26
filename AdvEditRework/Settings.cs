using MessagePack;
using Raylib_cs;

namespace AdvEditRework;

[MessagePackObject(keyAsPropertyName: true)]
public class Settings
{
    public static Settings Shared { get; private set; } = new();
    public int UIScale = 1;
    
    public KeyboardKey EyedropperBind = KeyboardKey.V;
    public KeyboardKey SelectBind = KeyboardKey.S;
    public KeyboardKey DrawBind = KeyboardKey.P;
    public KeyboardKey RectangleBind = KeyboardKey.R;
    public KeyboardKey BucketBind = KeyboardKey.B;

    private static string SettingsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AdvancedEdit");
    private static string SettingsFile => Path.Combine(SettingsDirectory, "config.msp");

    public static void Load()
    {
        var path = SettingsFile;
        if (!File.Exists(path)) Save();
        using var settingsStream = File.OpenRead(path);
        try
        {
            Shared = MessagePackSerializer.Deserialize<Settings>(settingsStream);
        }
        catch
        {
            // If settings are not read correctly (ex. wrong version) just use default ones.
            Shared = new Settings();
        }
    }

    public static void Save()
    {
        if (!Directory.Exists(SettingsDirectory))
            Directory.CreateDirectory(SettingsDirectory);
        using var settingsStream = File.Create(SettingsFile);
        MessagePackSerializer.Serialize(settingsStream, Shared);
    }
}