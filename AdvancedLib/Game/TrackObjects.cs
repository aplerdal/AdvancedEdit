using AdvancedLib.Serialization.Objects;
using AdvancedLib.Serialization.Tracks;
using AuroraLib.Core;
using MessagePack;

namespace AdvancedLib.Game;

[MessagePackObject]
public class TrackObjects : ICloneable<TrackObjects>
{
    [Key(0)] public List<ObjectPlacement> ObstaclePlacements { get; set; } = new();

    [Key(1)] public List<ObjectPlacement> ItemBoxes { get; set; } = new();

    [Key(2)] public List<ObjectPlacement> StartPositions { get; set; } = new();

    [Key(3)] public ObstacleTable ObstacleTable { get; set; } = new();

    public TrackObjects Clone()
    {
        List<ObjectPlacement> newObstacles = new List<ObjectPlacement>(ObstaclePlacements.Count);
        ObstaclePlacements.ForEach((item) => { newObstacles.Add((ObjectPlacement)item.Clone()); });
        List<ObjectPlacement> newItemBoxes = new List<ObjectPlacement>(ObstaclePlacements.Count);
        ItemBoxes.ForEach((item) => { newItemBoxes.Add((ObjectPlacement)item.Clone()); });
        List<ObjectPlacement> newStartPos = new List<ObjectPlacement>(ObstaclePlacements.Count);
        StartPositions.ForEach((item) => { newStartPos.Add((ObjectPlacement)item.Clone()); });
        var newTable = ObstacleTable.Clone();
        return new TrackObjects
        {
            ObstaclePlacements = newObstacles,
            ItemBoxes = newItemBoxes,
            StartPositions = newStartPos,
            ObstacleTable = newTable,
        };
    }
}