using AdvancedLib.Serialization;
using AuroraLib.Core.IO;

namespace AdvancedLib.Graphics;

public class BgrColor : ISerializable
{
    private ushort _raw;

    public byte R5
    {
        get => (byte)((_raw >> 0) & 0x1F);
        set => _raw = (ushort)((_raw & ~(0x1F << 0)) | ((value & 0x1F) << 0));
    }

    public byte G5
    {
        get => (byte)((_raw >> 5) & 0x1F);
        set => _raw = (ushort)((_raw & ~(0x1F << 5)) | ((value & 0x1F) << 5));
    }

    public byte B5
    {
        get => (byte)((_raw >> 10) & 0x1F);
        set => _raw = (ushort)((_raw & ~(0x1F << 10)) | ((value & 0x1F) << 10));
    }

    public byte R8 => (byte)((R5 << 3) | (R5 >> 2));
    public byte G8 => (byte)((G5 << 3) | (G5 >> 2));
    public byte B8 => (byte)((B5 << 3) | (B5 >> 2));

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
        R5 = (byte)(r >> 3);
        G5 = (byte)(g >> 3);
        B5 = (byte)(b >> 3);
    }

    public BgrColor(float r, float g, float b)
    {
        R5 = (byte)((byte)(r * 255f) / 8);
        G5 = (byte)((byte)(g * 255f) / 8);
        B5 = (byte)((byte)(b * 255f) / 8);
    }

    public BgrColor(ushort value)
    {
        _raw = value;
    }
}