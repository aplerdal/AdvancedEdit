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

    public BgrColor(ushort value)
    {
        _raw = value;
    }
}