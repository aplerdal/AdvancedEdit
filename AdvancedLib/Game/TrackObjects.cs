using AdvancedLib.Serialization.Objects;
using AdvancedLib.Serialization.Tracks;
using MessagePack;

namespace AdvancedLib.Game;

[MessagePackObject]
public class TrackObjects
{
    [Key(0)] public List<ObjectPlacement> ObstaclePlacements { get; set; } = new();

    [Key(1)] public List<Vec2I> ItemBoxes { get; set; } = new();

    [Key(2)] public List<StartPosition> StartPositions { get; set; } = new();

    [Key(3)] public ObstacleTable ObstacleTable { get; set; } = new();
}