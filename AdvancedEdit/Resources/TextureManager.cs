using System.Diagnostics;
using Raylib_cs;

namespace AdvEditRework.Resources;

public class TextureManager : IDisposable
{
    private Dictionary<string, Texture2D> _textures = new();

    public TextureManager()
    {
        LoadTexture("tools.png");
        LoadTexture("shapes.png");
        LoadTexture("zoneIcons.png");
        LoadTexture("font.png");
    }

    private void LoadTexture(string file)
    {
        Debug.Assert(File.Exists(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "Resources/", file)), $"Resource \"{file}\" not found.");
        var texture = Raylib.LoadTexture(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "Resources/", file));
        if (!Raylib.IsTextureValid(texture)) throw new Exception($"Error loading resource {Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "Resources/", file)}");
        _textures.Add(file, texture);
    }

    public Texture2D GetTexture(string filename)
    {
        return _textures[filename];
    }

    public void Dispose()
    {
        foreach (var texture in _textures)
            Raylib.UnloadTexture(texture.Value);
    }
}