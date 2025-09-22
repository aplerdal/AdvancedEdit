using System.Text.Json;
using MessagePack;

namespace AdvEditRework;

[MessagePackObject(keyAsPropertyName: true)]
public class Settings
{
    public static Settings Shared = new();
    public int UIScale = 1;

    public static Settings Load(string path)
    {
        using var settingsStream = File.OpenRead(path);
        return MessagePackSerializer.Deserialize<Settings>(settingsStream);
    }

    public void Save(string path)
    {
        using var settingsStream = File.Create(path);
        MessagePackSerializer.Serialize(settingsStream, this);
    }
}