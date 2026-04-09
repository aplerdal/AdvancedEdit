using AuroraLib.Core.Collections;
using MessagePack;
using Raylib_cs;

namespace AdvEditRework;

[MessagePackObject]
public class Settings
{
    public static Settings Shared { get; private set; } = new();

    [Key(0)] public KeyboardKey EyedropperBind = KeyboardKey.V;

    [Key(1)] public KeyboardKey SelectBind = KeyboardKey.S;

    [Key(2)] public KeyboardKey DrawBind = KeyboardKey.P;

    [Key(3)] public KeyboardKey RectangleBind = KeyboardKey.R;

    [Key(4)] public KeyboardKey BucketBind = KeyboardKey.B;

    [Key(5)] public List<string> RecentProjectFiles = new();
    [Key(6)] public string? BaseRomPath;

    private static string SettingsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AdvancedEdit");
    private static string SettingsFile => Path.Combine(SettingsDirectory, "config.msp");

    public static void Load()
    {
        var path = SettingsFile;
        if (!File.Exists(path)) Save();
        try
        {
            using var settingsStream = File.OpenRead(path);
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

    public void UpdateProjectList(string path)
    {
        if (RecentProjectFiles.Contains(path))
        {
            var index = RecentProjectFiles.IndexOf(path);
            RecentProjectFiles.Move(index, 0);
        }
        else
        {
            RecentProjectFiles.Insert(0, path);
        }

        if (RecentProjectFiles.Count > 16) RecentProjectFiles.RemoveAt(16);
        Save();
    }
}