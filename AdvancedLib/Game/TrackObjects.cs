using AdvancedLib.Serialization.Objects;
using MessagePack;

namespace AdvancedLib.Game;

[MessagePackObject]
public class TrackObjects
{
    [Key(0)]
    public List<ObstaclePlacement> ObstaclePlacements { get; set; } = new();

    [Key(1)]
    public List<Vec2I> ItemBoxes { get; set; } = new();

    [Key(2)]
    public List<StartPosition> StartPositions { get; set; } = new();

    public List<Obstacle> GetObstacles()
    {
        List<Obstacle> obstacles = new();
        foreach (var obstacle in ObstaclePlacements)
            if (!obstacles.Contains(obstacle.Obstacle))
                obstacles.Add(obstacle.Obstacle);
        return obstacles;
    }
}