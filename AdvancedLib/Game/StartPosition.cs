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

public class StartPosition(Vec2I position, StartingPlace startingPlace)
{
    public StartingPlace Place { get; set; } = startingPlace;
    public Vec2I Position { get; set; } = position;
}