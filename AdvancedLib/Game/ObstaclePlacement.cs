using AdvancedLib.Serialization.Objects;
using MessagePack;

namespace AdvancedLib.Game;

[MessagePackObject]
public class ObstaclePlacement(Obstacle obstacle, Vec2I position)
{
    [Key(0)]
    public Obstacle Obstacle { get; set; } = obstacle;

    [Key(1)]
    public Vec2I Position { get; set; } = position;
}