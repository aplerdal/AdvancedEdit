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
    SinglePakSecond = 10
}

[MessagePackObject]
public class StartPosition(Vec2I position, StartingPlace startingPlace)
{
    public StartPosition() : this(new Vec2I(0, 0), StartingPlace.First)
    {
    }

    [Key(0)]
    public StartingPlace Place { get; set; } = startingPlace;
    [Key(1)]
    public Vec2I Position { get; set; } = position;
}