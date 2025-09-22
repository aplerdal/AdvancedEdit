using AdvancedLib.Serialization.Objects;
using MessagePack;

namespace AdvancedLib.Game;

[MessagePackObject(keyAsPropertyName: true)]
public class ObstaclePlacement(Obstacle obstacle, Vec2I position)
{
    public Obstacle Obstacle { get; set; } = obstacle;
    public Vec2I Position { get; set; } = position;
}