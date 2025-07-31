using AdvancedLib.Serialization.Objects;

namespace AdvancedLib.Game;

public class ObstaclePlacement(Obstacle obstacle, Vec2I position)
{
    public Obstacle Obstacle { get; set; } = obstacle;
    public Vec2I Position { get; set; } = position;
}