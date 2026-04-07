using AdvancedLib.Game;
using AdvancedLib.Graphics;
using AdvancedLib.Serialization.AI;
using AdvancedLib.Serialization.Tracks;
using MessagePack;

namespace AdvancedLib.Project;

[MessagePackObject]
public class ProjectTrack
{
    [Key(0)] public string Name { get; set; }

    // Folder is derived at runtime, never serialized
    [IgnoreMember] public string Folder { get; private set; }

    public static string Config => "track.msp";
    public static string Tileset => "tileset.chr";
    public static string TilesetPal => "tileset.pal";
    public static string Tilemap => "tilemap.scr";
    public static string Minimap => "minimap.chr";
    public static string ObstacleGfx => "obstacles.chr";
    public static string ObstaclePal => "obstacles.pal";
    public static string Objects => "objects.msp";
    public static string Behaviors => "behaviors.msp";
    public static string Ai => "ai.msp";
    public static string CoverArt => "cover.chr";
    public static string TrackNameGfx => "name.chr";
    public static string CoverPal => "cover.pal";
    public static string LockedCoverPal => "cover_locked.pal";
    public static string TargetTimes => "times.msp";
    public static string TurnSigns => "turns.msp";
    public static string RivalTargets => "rivals.msp";

    private string PathFor(string file)
    {
        return Path.Combine(Folder, file);
    }

    public ProjectTrack(string name)
    {
        Name = name;
    }

    public void ResolveFolder(string baseDirectory)
    {
        Folder = Path.Combine(baseDirectory, Name);
        if (!Directory.Exists(Folder))
            Directory.CreateDirectory(Folder);
    }

    private async Task SerializeMspAsync<T>(string file, T value)
    {
        await using var stream = File.Create(PathFor(file));
        await MessagePackSerializer.SerializeAsync(stream, value);
    }

    private async Task SerializeAsync<T>(string file, T value) where T : IAsyncWritable
    {
        await using var stream = File.Create(PathFor(file));
        await value.WriteAsync(stream);
    }


    public async Task SaveTrackDataAsync(Track track)
    {
        var tasks = new List<Task>();

        if (track.ObstacleGfx is not null && track.ObstaclePalette is not null)
        {
            tasks.Add(SerializeAsync(ObstacleGfx, track.ObstacleGfx));
            tasks.Add(SerializeAsync(ObstaclePal, track.ObstaclePalette));
        }

        if (track.CoverArt is not null && track.CoverPalette is not null)
        {
            tasks.Add(SerializeAsync(CoverArt, track.CoverArt));
            tasks.Add(SerializeAsync(CoverPal, track.CoverPalette));
        }

        if (track.LockedCoverPalette is not null)
        {
            tasks.Add(SerializeAsync(LockedCoverPal, track.LockedCoverPalette));
        }

        if (track.TurnSigns is not null)
        {
            tasks.Add(SerializeMspAsync(TurnSigns, track.TurnSigns));
        }

        tasks.AddRange([
            SerializeMspAsync(Config, track.Config),
            SerializeAsync(Tileset, track.Tileset),
            SerializeAsync(TilesetPal, track.TilesetPalette),
            SerializeAsync(Tilemap, track.Tilemap),
            SerializeAsync(Minimap, track.Minimap),
            SerializeAsync(TrackNameGfx, track.TrackNameGfx),
            SerializeMspAsync(Objects, track.Objects),
            SerializeMspAsync(Ai, track.Ai),
            SerializeMspAsync(TargetTimes, track.TargetTimes),
            SerializeMspAsync(Behaviors, track.Behaviors),
            SerializeMspAsync(RivalTargets, track.RivalTargets)
        ]);

        await Task.WhenAll(tasks);
    }

    private T DeserializeMsp<T>(string file)
    {
        using var stream = File.OpenRead(PathFor(file));
        return MessagePackSerializer.Deserialize<T>(stream);
    }

    private T? DeserializeMspIfExists<T>(string file) where T : class
    {
        if (!File.Exists(PathFor(file))) return null;
        using var stream = File.OpenRead(PathFor(file));
        return MessagePackSerializer.Deserialize<T>(stream);
    }

    private T Deserialize<T>(string file, Func<Stream, T> init)
    {
        using var stream = File.OpenRead(PathFor(file));
        return init(stream);
    }

    private T? DeserializeIfExists<T>(string file, Func<Stream, T> init) where T : class
    {
        if (!File.Exists(PathFor(file))) return null;
        using var stream = File.OpenRead(PathFor(file));
        return init(stream);
    }


    public Track LoadTrackData()
    {
        var trackConfig = DeserializeMsp<TrackConfig>(Config);
        return new Track
        {
            Config = trackConfig,
            Tileset = Deserialize(Tileset, s => new Tileset(s, 256, PixelFormat.Bpp8)),
            TilesetPalette = Deserialize(TilesetPal, s => new Palette(s, 64)),
            Tilemap = Deserialize(Tilemap, s => new AffineTilemap(s, trackConfig.Size.X * 128, trackConfig.Size.Y * 128)),
            Minimap = Deserialize(Minimap, s => new Tileset(s, 64, PixelFormat.Bpp4)),
            ObstacleGfx = DeserializeIfExists(ObstacleGfx, s => new Tileset(s, 256, PixelFormat.Bpp4)),
            ObstaclePalette = DeserializeIfExists(ObstaclePal, s => new Palette(s, 48)),
            CoverArt = DeserializeIfExists(CoverArt, s => new Tileset(s, 81, PixelFormat.Bpp8)),
            CoverPalette = DeserializeIfExists(CoverPal, s => new Palette(s, 80)),
            LockedCoverPalette = DeserializeIfExists(CoverPal, s => new Palette(s, 80)),
            TrackNameGfx = Deserialize(TrackNameGfx, s => new Tileset(s, 24, PixelFormat.Bpp4)),
            Behaviors = DeserializeMsp<byte[]>(Behaviors),
            Objects = DeserializeMsp<TrackObjects>(Objects),
            Ai = DeserializeMsp<TrackAi>(Ai),
            TargetTimes = DeserializeMsp<TargetTime[]>(TargetTimes),
            TurnSigns = DeserializeMspIfExists<TurnSign[]>(TurnSigns),
            RivalTargets = DeserializeMsp<RivalTargets>(RivalTargets),
        };
    }
}