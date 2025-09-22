using MessagePack;

namespace AdvancedLib.Game;

public enum StartingPlace
{
    First = 1,
    Second = 2,
    Third = 3,
    Fourth = 4,
    Fifth = 5,
    Sixth = 6,
    Seventh = 7,
    Eighth = 8,
    SinglePakFirst = 9,
    SinglePakSecond = 10,
}

[MessagePackObject(keyAsPropertyName: true)]
public class StartPosition(Vec2I position, StartingPlace startingPlace)
{
    public StartPosition() : this(new Vec2I(0, 0), StartingPlace.First) {}
    public StartingPlace Place { get; set; } = startingPlace;
    public Vec2I Position { get; set; } = position;
}