using AdvancedLib.Serialization;
using AuroraLib.Core.IO;

namespace AdvancedLib.Graphics;

public class BgrColor : ISerializable
{
    private ushort _raw;

    public byte R
    {
        get => (byte)((_raw >> 0) & 0x1F);
        set => _raw = (ushort)((_raw & ~(0x1F << 0)) | ((value & 0x1F) << 0));
    }

    public byte G
    {
        get => (byte)((_raw >> 5) & 0x1F);
        set => _raw = (ushort)((_raw & ~(0x1F << 5)) | ((value & 0x1F) << 5));
    }

    public byte B
    {
        get => (byte)((_raw >> 10) & 0x1F);
        set => _raw = (ushort)((_raw & ~(0x1F << 10)) | ((value & 0x1F) << 10));
    }

    public void Serialize(Stream stream)
    {
        stream.Write(_raw);
    }

    public void Deserialize(Stream stream)
    {
        _raw = stream.ReadUInt16();
    }

    public BgrColor()
    {
    }

    public BgrColor(byte r, byte g, byte b)
    {
        R = (byte)(r * 8);
        G = (byte)(g * 8);
        B = (byte)(b * 8);
    }

    public BgrColor(float r, float g, float b)
    {
        R = (byte)((byte)(r * 255f) / 8);
        G = (byte)((byte)(g * 255f) / 8);
        B = (byte)((byte)(b * 255f) / 8);
    }

    public BgrColor(ushort value)
    {
        _raw = value;
    }
}