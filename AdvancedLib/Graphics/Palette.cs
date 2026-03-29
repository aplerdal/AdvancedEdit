using AdvancedLib.Serialization;
using AuroraLib.Core.IO;

namespace AdvancedLib.Graphics;

public class Palette : IAsyncWritable
{
    private BgrColor[] _colors;

    public int Length => _colors.Length;

    /// <summary>
    /// Creates empty palette of the given length
    /// </summary>
    /// <param name="length">length of palette</param>
    public Palette(int length)
    {
        _colors = new BgrColor[length];
        for (var i = 0; i < length; i++)
            _colors[i] = new BgrColor(0);
    }

    public Palette(Stream stream, int length)
    {
        _colors = new BgrColor[length];
        for (var i = 0; i < length; i++)
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

    public BgrColor[] this[Range range] => _colors[range];

    public void Write(Stream stream)
    {
        foreach (var entry in _colors) stream.Write(entry);
    }

    public byte[] GetData()
    {
        var stream = new MemoryPoolStream(_colors.Length * 2, true);
        Write(stream);
        return stream.ToArray();
    }

    public Task WriteAsync(Stream stream)
    {
        return Task.Run(() => Write(stream));
    }

    public void Trim(int totalLength)
    {
        _colors = _colors[..totalLength];
    }
}