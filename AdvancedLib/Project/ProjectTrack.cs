using System.Text.Json;
using AdvancedLib.Game;
using AdvancedLib.Graphics;
using AdvancedLib.Serialization;
using AdvancedLib.Serialization.AI;
using MessagePack;
using AuroraLib.Core.IO;

namespace AdvancedLib.Project;

public class ProjectTrack
{
    public string Folder { get; set; }
    public string Name { get; set; }
    
    private readonly string _configPath, _tilesetPath, _tilemapPath, _minimapPath, _obstacleGraphicsPath, _objectsPath, _aiPath, _tilesetPalPath, _behaviorsPath;

    public ProjectTrack(string folder, string name)
    {
        Name = name;
        Folder = folder;
        _configPath = Path.Combine(Folder, "track.json");
        _tilesetPath = Path.Combine(Folder, "tileset.chr");
        _tilesetPalPath = Path.Combine(Folder, "tileset.pal");
        _tilemapPath = Path.Combine(Folder, "tilemap.scr");
        _minimapPath = Path.Combine(Folder, "minimap.chr");
        _obstacleGraphicsPath = Path.Combine(Folder, "obstacles.chr");
        _objectsPath = Path.Combine(Folder, "objects.json");
        _behaviorsPath = Path.Combine(Folder, "behaviors.bin");
        _aiPath = Path.Combine(Folder, "ai.json");
        if (!Directory.Exists(Folder))
            Directory.CreateDirectory(Folder);
    }
    
    // public void SaveTrackData(Track track)
    // {
    //     using var configStream = File.Create(_configPath);
    //     using var tilesetStream = File.Create(_tilesetPath);
    //     using var tilemapStream = File.Create(_tilemapPath);
    //     using var minimapStream = File.Create(_minimapPath);
    //     using var obstacleGfxStream = File.Create(_obstacleGraphicsPath);
    //     using var objectsStream = File.Create(_objectsPath);
    //     using var aiStream = File.Create(_aiPath);
    //     
    //     JsonSerializer.Serialize(configStream, track.Config);
    //     track.Tileset.Write(tilesetStream);
    //     track.Tilemap.Write(tilemapStream);
    //     track.Minimap.Write(minimapStream);
    //     track.ObstacleGfx.Write(obstacleGfxStream);
    //     JsonSerializer.Serialize(objectsStream, track.Objects);
    //     JsonSerializer.Serialize(aiStream, track.Ai);
    // }
    public void SaveTrackData(Track track)
    {
        using var configStream = File.Create(_configPath);
        using var tilesetStream = File.Create(_tilesetPath);
        using var tilesetPalStream = File.Create(_tilesetPalPath);
        using var tilemapStream = File.Create(_tilemapPath);
        using var minimapStream = File.Create(_minimapPath);
        using var obstacleGfxStream = File.Create(_obstacleGraphicsPath);
        using var objectsStream = File.Create(_objectsPath);
        using var aiStream = File.Create(_aiPath);
        using var behaviorsStream = File.Create(_behaviorsPath);

        track.ObstacleGfx?.Write(obstacleGfxStream);
        Task.WaitAll(
            JsonSerializer.SerializeAsync(configStream, track.Config),
            track.Tileset.WriteAsync(tilesetStream),
            track.TilesetPalette.WriteAsync(tilesetPalStream),
            track.Tilemap.WriteAsync(tilemapStream), 
            track.Minimap.WriteAsync(minimapStream),
            JsonSerializer.SerializeAsync(objectsStream, track.Objects), 
            JsonSerializer.SerializeAsync(aiStream, track.Ai)
        );
    }

    public Track LoadTrackData()
    {
        using var configStream = File.OpenRead(_configPath);
        using var tilesetStream = File.OpenRead(_tilesetPath);
        using var tilesetPalStream = File.OpenRead(_tilesetPalPath);
        using var tilemapStream = File.OpenRead(_tilemapPath);
        using var minimapStream = File.OpenRead(_minimapPath);
        Stream? obstacleGfxStream = null;
        if (File.Exists(_obstacleGraphicsPath))
            obstacleGfxStream = File.OpenRead(_obstacleGraphicsPath);
        using var objectsStream = File.OpenRead(_objectsPath);
        using var aiStream = File.OpenRead(_aiPath);
        using var behaviorsStream = File.OpenRead(_behaviorsPath);

        byte[] behaviors = new byte[256];
        behaviorsStream.ReadExactly(behaviors);
        
        var trackConfig = JsonSerializer.Deserialize<TrackConfig>(configStream) ?? throw new NullReferenceException("track.json was null");
        return new Track
        {
            Config = trackConfig,
            Tileset = new Tileset(tilesetStream, 256, PixelFormat.Bpp8),
            TilesetPalette = new Palette(tilesetPalStream, 64),
            Tilemap = new AffineTilemap(tilemapStream, trackConfig.Size.X * 128, trackConfig.Size.Y * 128),
            Minimap = new Tileset(minimapStream, 64, PixelFormat.Bpp4),
            ObstacleGfx = (obstacleGfxStream is null) ? null : new Tileset(obstacleGfxStream, 256, PixelFormat.Bpp4),
            Behaviors = behaviors,
            Objects = JsonSerializer.Deserialize<TrackObjects>(_objectsPath) ?? throw new NullReferenceException("objects.json was null"),
            Ai = JsonSerializer.Deserialize<TrackAi>(_objectsPath) ?? throw new NullReferenceException("ai.json was null"),
        };
    }
}