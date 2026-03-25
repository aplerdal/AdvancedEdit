using MessagePack;

namespace AdvancedLib.Game;

[MessagePackObject]
public class ObstaclePlacement(int index, Vec2I position)
{
    [Key(0)]
    public int Index { get; set; } = index;

    [Key(1)]
    public Vec2I Position { get; set; } = position;
}