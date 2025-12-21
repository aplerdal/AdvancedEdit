using MessagePack;

namespace AdvancedLib.Game;

[MessagePackObject(keyAsPropertyName: true)]
public class TrackConfig
{
    public required Vec2I Size { get; set; }
    public required uint BackgroundIndex { get; set; }
    public required uint BackgroundBehavior { get; set; }
    public required uint PaletteBehavior { get; set; }
    public required uint Theme { get; set; }
    public required uint SongID { get; set; }
    public required uint Laps { get; set; }
    public required uint TurnsPointer { get; set; }
    public required uint TargetOptionsPointer { get; set; }
    public required uint CoverGfxPointer { get; set; }
    public required uint CoverPalPointer { get; set; }
    public required uint LockedTrackPalPointer { get; set; }
    public required uint TrackNameGfxPointer { get; set; }

    public static readonly TrackConfig Default = new TrackConfig
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
    };
}