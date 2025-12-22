using AdvancedLib.Game;
using AdvancedLib.Graphics;
using AdvancedLib.Serialization.AI;
using MessagePack;

namespace AdvancedLib.Project;

[MessagePackObject(keyAsPropertyName: true)]
public class ProjectTrack
{
    public string Folder { get; set; }
    public string Name { get; set; }

    private readonly string _configPath, _tilesetPath, _tilemapPath, _minimapPath, _obstacleGraphicsPath, _objectsPath, _obstaclePalettePath, _aiPath, _tilesetPalPath, _behaviorsPath, _coinsPath;

    public ProjectTrack(string folder, string name)
    {
        Name = name;
        Folder = folder;
        _configPath = Path.Combine(Folder, "track.msp");
        _tilesetPath = Path.Combine(Folder, "tileset.chr");
        _tilesetPalPath = Path.Combine(Folder, "tileset.pal");
        _tilemapPath = Path.Combine(Folder, "tilemap.scr");
        _minimapPath = Path.Combine(Folder, "minimap.chr");
        _obstacleGraphicsPath = Path.Combine(Folder, "obstacles.chr");
        _obstaclePalettePath = Path.Combine(Folder, "obstacles.pal");
        _objectsPath = Path.Combine(Folder, "objects.msp");
        _coinsPath = Path.Combine(Folder, "coins.msp");
        _behaviorsPath = Path.Combine(Folder, "behaviors.bin");
        _aiPath = Path.Combine(Folder, "ai.msp");
        if (!Directory.Exists(Folder))
            Directory.CreateDirectory(Folder);
    }

    public void SaveTrackData(Track track)
    {
        using var configStream = File.Create(_configPath);
        using var tilesetStream = File.Create(_tilesetPath);
        using var tilesetPalStream = File.Create(_tilesetPalPath);
        using var tilemapStream = File.Create(_tilemapPath);
        using var minimapStream = File.Create(_minimapPath);
        using var objectsStream = File.Create(_objectsPath);
        using var aiStream = File.Create(_aiPath);
        using var coinsStream = File.Create(_coinsPath);
        using var behaviorsStream = File.Create(_behaviorsPath);

        if (track.ObstacleGfx is not null && track.ObstaclePalette is not null)
        {
            using var obstacleGfxStream = File.Create(_obstacleGraphicsPath);
            track.ObstacleGfx.Write(obstacleGfxStream);
            using var obstaclePaletteStream = File.Create(_obstaclePalettePath);
            track.ObstaclePalette.Write(obstaclePaletteStream);
        }

        Task.WaitAll(
            MessagePackSerializer.SerializeAsync(configStream, track.Config),
            track.Tileset.WriteAsync(tilesetStream),
            track.TilesetPalette.WriteAsync(tilesetPalStream),
            track.Tilemap.WriteAsync(tilemapStream),
            track.Minimap.WriteAsync(minimapStream),
            MessagePackSerializer.SerializeAsync(coinsStream, track.Coins),
            behaviorsStream.WriteAsync(track.Behaviors).AsTask(),
            MessagePackSerializer.SerializeAsync(objectsStream, track.Objects),
            MessagePackSerializer.SerializeAsync(aiStream, track.Ai)
        );
    }

    public Track LoadTrackData()
    {
        using var configStream = File.OpenRead(_configPath);
        using var tilesetStream = File.OpenRead(_tilesetPath);
        using var tilesetPalStream = File.OpenRead(_tilesetPalPath);
        using var tilemapStream = File.OpenRead(_tilemapPath);
        using var minimapStream = File.OpenRead(_minimapPath);
        using var coinsStream = File.OpenRead(_coinsPath);
        Stream? obstacleGfxStream = null;
        Stream? obstaclePaletteStream = null;
        if (File.Exists(_obstacleGraphicsPath))
        {
            obstacleGfxStream = File.OpenRead(_obstacleGraphicsPath);
            obstaclePaletteStream = File.OpenRead(_obstaclePalettePath);
        }

        using var objectsStream = File.OpenRead(_objectsPath);
        using var aiStream = File.OpenRead(_aiPath);
        using var behaviorsStream = File.OpenRead(_behaviorsPath);

        byte[] behaviors = new byte[256];
        behaviorsStream.ReadExactly(behaviors);

        var trackConfig = MessagePackSerializer.Deserialize<TrackConfig>(configStream);
        var obstacleGfxTileset = (obstacleGfxStream is null) ? null : new Tileset(obstacleGfxStream, 256, PixelFormat.Bpp4);
        var obstaclePalette = (obstaclePaletteStream is null) ? null : new Palette(obstaclePaletteStream, 48);
        obstacleGfxStream?.Dispose();
        obstaclePaletteStream?.Dispose();
        return new Track
        {
            Config = trackConfig,
            Tileset = new Tileset(tilesetStream, 256, PixelFormat.Bpp8),
            TilesetPalette = new Palette(tilesetPalStream, 64),
            Tilemap = new AffineTilemap(tilemapStream, trackConfig.Size.X * 128, trackConfig.Size.Y * 128),
            Minimap = new Tileset(minimapStream, 64, PixelFormat.Bpp4),
            ObstacleGfx = obstacleGfxTileset,
            ObstaclePalette = obstaclePalette,
            Behaviors = behaviors,
            Coins = MessagePackSerializer.Deserialize<List<Vec2I>>(coinsStream),
            Objects = MessagePackSerializer.Deserialize<TrackObjects>(objectsStream),
            Ai = MessagePackSerializer.Deserialize<TrackAi>(aiStream),
        };
    }
}