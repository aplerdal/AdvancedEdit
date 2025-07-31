namespace AdvancedLib.Graphics;

public interface ITilemap
{
    public byte this[int x, int y] { get; set; }
}