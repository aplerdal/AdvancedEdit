using AdvancedLib.Serialization.Objects;

namespace AdvancedLib.Game;

public class TrackObjects
{
    public List<ObstaclePlacement> ObstaclePlacements { get; set; } = new();
    public List<Vec2I> ItemBoxes { get; set; } = new();
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