using AdvancedLib.Serialization.Tracks;
using MessagePack;

namespace AdvancedLib.Game;

[MessagePackObject]
public class TrackConfig
{
    [Key(0)] public required Vec2I Size { get; set; }
    [Key(1)] public required uint BackgroundIndex { get; set; }
    [Key(2)] public required uint BackgroundBehavior { get; set; }
    [Key(3)] public required uint PaletteBehavior { get; set; }
    [Key(4)] public required uint Theme { get; set; }
    [Key(5)] public required uint SongID { get; set; }
    [Key(6)] public required uint Laps { get; set; }
    [Key(7)] public required uint TurnsPointer { get; set; }
    [Key(8)] public required uint TargetOptionsPointer { get; set; }
    [Key(9)] public required uint CoverGfxPointer { get; set; }
    [Key(10)] public required uint CoverPalPointer { get; set; }
    [Key(11)] public required uint LockedTrackPalPointer { get; set; }
    [Key(12)] public required uint TrackNameGfxPointer { get; set; }
    [Key(13)] public required uint TargetTimesPtr { get; set; }

    public static readonly TrackConfig Default = new()
    {
        Size = new Vec2I(2, 2),
        BackgroundIndex = 0,
        BackgroundBehavior = 1,
        PaletteBehavior = 1,
        Theme = 2,
        SongID = 25,
        Laps = 3,
        TurnsPointer = 0,
        TargetOptionsPointer = 0,
        CoverGfxPointer = 0,
        CoverPalPointer = 0,
        LockedTrackPalPointer = 0,
        TrackNameGfxPointer = 0,
        TargetTimesPtr = 0
    };
}