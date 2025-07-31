using AdvancedLib.Serialization;

namespace AdvancedLib.Graphics;

public class Palette
{
    private BgrColor[] _colors;
    public int Length
    {
        get => _colors.Length;
    }

    /// <summary>
    /// Creates empty palette of the given length
    /// </summary>
    /// <param name="length">length of palette</param>
    public Palette(int length)
    {
        _colors = new BgrColor[length];
        for (int i = 0; i < length; i++)
            _colors[i] = new BgrColor(0);
    }
    public Palette(Stream stream, int length)
    {
        _colors = new BgrColor[length];
        for (int i = 0; i < length; i++)
            _colors[i] = stream.Read<BgrColor>();
    }

    public Palette(BgrColor[] colors)
    {
        _colors = colors;
    }
    public BgrColor this[int index]
    {
        get => _colors[index];
        set => _colors[index] = value;
    }

    public void Write(Stream stream)
    {
        foreach (var entry in _colors)
        {
            stream.Write(entry);   
        }
    }
    public Task WriteAsync(Stream stream) => Task.Run(() => Write(stream));
}