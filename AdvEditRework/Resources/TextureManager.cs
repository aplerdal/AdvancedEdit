using System.Diagnostics;
using Raylib_cs;

namespace AdvEditRework.Resources;

public class TextureManager : IDisposable
{
    private Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
    public TextureManager()
    {
        LoadTexture("tools.png");
        LoadTexture("shapes.png");
        LoadTexture("zoneIcons.png");
    }

    private void LoadTexture(string file)
    {
        Debug.Assert(File.Exists(Path.Combine("Resources/", file)), $"Resource \"{file}\" not found.");
        var texture = Raylib.LoadTexture(Path.Combine("Resources/", file));
        if (!Raylib.IsTextureValid(texture)) throw new Exception();
        _textures.Add(file,texture);
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